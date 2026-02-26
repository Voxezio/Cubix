using System;
using System.IO;
using StbImageSharp;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Cubix;

public class Texture : IDisposable
{
    public ID3D11ShaderResourceView ResourceView { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }

    public Texture(ID3D11Device device, string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Texture not found: {filePath}");

        // Загрузка байтов через StbImageSharp
        using var stream = File.OpenRead(filePath);
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

        Width = image.Width;
        Height = image.Height;

        // Описание текстуры
        var texDesc = new Texture2DDescription
        {
            Width = image.Width,
            Height = image.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = Format.R8G8B8A8_UNorm,
            SampleDescription = new SampleDescription(1, 0),
            Usage = Usage.Immutable,
            BindFlags = BindFlags.ShaderResource,
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };

        // Заливка данных
        var data = new SubresourceData(image.Data, image.Width * 4);

        using var texture = device.CreateTexture2D(texDesc, new[] { data });
        ResourceView = device.CreateShaderResourceView(texture);
    }

    public void Dispose()
    {
        ResourceView?.Dispose();
    }
}