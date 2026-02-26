using System;
using System.Numerics;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Cubix;

public class SpriteRenderer : IDisposable
{
    private ID3D11Device _device;
    private ID3D11DeviceContext _context;

    private ID3D11Buffer _vertexBuffer;
    private ID3D11InputLayout _inputLayout;
    private ID3D11VertexShader _vertexShader;
    private ID3D11PixelShader _pixelShader;
    private ID3D11SamplerState _samplerState;
    private ID3D11BlendState _blendState;

    // Структура вершины
    private struct Vertex
    {
        public Vector3 Position;
        public Vector2 TexCoord;
        public Vertex(float x, float y, float u, float v) 
        { 
            Position = new Vector3(x, y, 0); 
            TexCoord = new Vector2(u, v); 
        }
    }

    public SpriteRenderer(ID3D11Device device, ID3D11DeviceContext context)
    {
        _device = device;
        _context = context;
        InitializePipeline();
    }

    private void InitializePipeline()
    {
        // 1. Шейдеры (HLSL)
        string source = @"
            struct VS_IN { float3 pos : POSITION; float2 uv : TEXCOORD; };
            struct PS_IN { float4 pos : SV_POSITION; float2 uv : TEXCOORD; };
            
            PS_IN VS(VS_IN input) {
                PS_IN output;
                // Преобразование из пикселей экрана (-1..1 NDC) делается в C# или матрицей. 
                // Для упрощения передаем координаты сразу в NDC (-1..1)
                output.pos = float4(input.pos, 1.0);
                output.uv = input.uv;
                return output;
            }

            Texture2D tex : register(t0);
            SamplerState samp : register(s0);

            float4 PS(PS_IN input) : SV_TARGET {
                return tex.Sample(samp, input.uv);
            }
        ";

        // Компиляция
        using var vsBlob = Compiler.Compile(source, "VS", "vs_5_0");
        using var psBlob = Compiler.Compile(source, "PS", "ps_5_0");

        _vertexShader = _device.CreateVertexShader(vsBlob);
        _pixelShader = _device.CreatePixelShader(psBlob);

        // 2. Input Layout
        _inputLayout = _device.CreateInputLayout(
            [
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 12, 0)
            ], 
            vsBlob);

        // 3. Динамический вертексный буфер (на 1 квад = 6 вершин)
        _vertexBuffer = _device.CreateBuffer(new BufferDescription
        {
            Usage = Usage.Dynamic,
            ByteWidth = (int)(sizeof(float) * 5 * 6), // 5 floats * 6 vertices
            BindFlags = BindFlags.VertexBuffer,
            CpuAccessFlags = CpuAccessFlags.Write
        });

        // 4. Сэмплер (Point для пиксель-арта)
        _samplerState = _device.CreateSamplerState(new SamplerDescription
        {
            Filter = Filter.MinMagMipPoint, // Пиксельный стиль
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            ComparisonFunction = ComparisonFunction.Never
        });

        // 5. Blend State (прозрачность)
        var blendDesc = new BlendDescription();
        blendDesc.RenderTarget[0] = new RenderTargetBlendDescription
        {
            IsBlendEnabled = true,
            SourceBlend = Blend.SourceAlpha,
            DestinationBlend = Blend.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.One,
            DestinationBlendAlpha = Blend.Zero,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };
        _blendState = _device.CreateBlendState(blendDesc);
    }

    public void Begin()
    {
        _context.IASetInputLayout(_inputLayout);
        _context.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        _context.VSSetShader(_vertexShader);
        _context.PSSetShader(_pixelShader);
        _context.PSSetSamplers(0, _samplerState);
        _context.OMSetBlendState(_blendState);
    }

    // Рисует спрайт. Координаты в пикселях.
    public void Draw(Texture texture, Rectangle destRect, Rectangle? srcRect, int screenWidth, int screenHeight)
    {
        _context.PSSetShaderResources(0, texture.ResourceView);

        float left = destRect.X;
        float top = destRect.Y;
        float right = destRect.X + destRect.Width;
        float bottom = destRect.Y + destRect.Height;

        // Преобразование координат (0,0 - верхний левый) в NDC (-1..1)
        float ndcLeft = (left / screenWidth) * 2 - 1;
        float ndcRight = (right / screenWidth) * 2 - 1;
        float ndcTop = 1 - (top / screenHeight) * 2;
        float ndcBottom = 1 - (bottom / screenHeight) * 2;

        // UV координаты
        float u0 = 0, v0 = 0, u1 = 1, v1 = 1;
        if (srcRect.HasValue)
        {
            u0 = (float)srcRect.Value.X / texture.Width;
            v0 = (float)srcRect.Value.Y / texture.Height;
            u1 = (float)(srcRect.Value.X + srcRect.Value.Width) / texture.Width;
            v1 = (float)(srcRect.Value.Y + srcRect.Value.Height) / texture.Height;
        }

        // Заполняем буфер (2 треугольника)
        Vertex[] vertices = {
            new Vertex(ndcLeft, ndcTop, u0, v0),    // Top Left
            new Vertex(ndcRight, ndcTop, u1, v0),   // Top Right
            new Vertex(ndcLeft, ndcBottom, u0, v1), // Bottom Left
            
            new Vertex(ndcLeft, ndcBottom, u0, v1), // Bottom Left
            new Vertex(ndcRight, ndcTop, u1, v0),   // Top Right
            new Vertex(ndcRight, ndcBottom, u1, v1) // Bottom Right
        };

        // Загрузка в GPU
        DataStream stream;
        _context.Map(_vertexBuffer, 0, MapMode.WriteDiscard, MapFlags.None, out stream);
        stream.Write(vertices);
        _context.Unmap(_vertexBuffer, 0);
        stream.Dispose();

        _context.IASetVertexBuffers(0, new VertexBufferView(_vertexBuffer, sizeof(float) * 5));
        _context.Draw(6, 0);
    }

    public void Dispose()
    {
        _blendState.Dispose();
        _samplerState.Dispose();
        _vertexBuffer.Dispose();
        _inputLayout.Dispose();
        _vertexShader.Dispose();
        _pixelShader.Dispose();
    }
}