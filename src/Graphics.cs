using System;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Cubix;

public class Graphics : IDisposable
{
    private ID3D11Device _device = null!;
    private ID3D11DeviceContext _context = null!;
    private IDXGISwapChain _swapChain = null!;
    private ID3D11RenderTargetView _renderView = null!;

    // Цвет фона по умолчанию
    private Color4 _clearColor = new Color4(0.39f, 0.58f, 0.93f, 1.0f);

    public Graphics(IntPtr windowHandle, int width, int height)
    {
        Initialize(windowHandle, width, height);
    }

    private void Initialize(IntPtr windowHandle, int width, int height)
    {
        var swapChainDesc = new SwapChainDescription()
        {
            BufferCount = 1,
            BufferDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            BufferUsage = Usage.RenderTargetOutput,
            OutputWindow = windowHandle,
            SampleDescription = new SampleDescription(1, 0),
            Windowed = true,
            SwapEffect = SwapEffect.Discard,
            Flags = SwapChainFlags.None
        };

        var result = D3D11.D3D11CreateDeviceAndSwapChain(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport, 
            [FeatureLevel.Level_11_0],
            swapChainDesc,
            out _swapChain!,
            out _device!,
            out var featureLevel,
            out var context
        );

        if (result.Failure)
        {
            throw new Exception($"Failed to create Direct3D11 Device: {result}");
        }

        _context = context!;

        using var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderView = _device.CreateRenderTargetView(backBuffer);
    }

    // Метод для изменения цвета фона
    public void SetClearColor(Color4 color)
    {
        _clearColor = color;
    }

    public void Render()
    {
        if (_context == null || _renderView == null) return;

        // Очищаем экран текущим цветом _clearColor
        _context.ClearRenderTargetView(_renderView, _clearColor);

        // Здесь будет рендеринг 3D...

        _swapChain.Present(1, PresentFlags.None);
    }

    public void Dispose()
    {
        _renderView?.Dispose();
        _context?.Dispose();
        _device?.Dispose();
        _swapChain?.Dispose();
    }
}