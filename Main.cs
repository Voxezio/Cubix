using System;
using System.Drawing;
using System.Windows.Forms;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.XAudio2;
using Vortice.XInput;

namespace Cubix;

public static class Program
{
    // Graphics Resources
    private static ID3D11Device _device = null!;
    private static ID3D11DeviceContext _context = null!;
    private static IDXGISwapChain _swapChain = null!;
    private static ID3D11RenderTargetView _renderView = null!;

    // Audio Resources
    private static IXAudio2 _audioEngine = null!;
    private static IXAudio2MasteringVoice _masterVoice = null!;

    // Windowing
    private static Form _window = null!;
    private static bool _isRunning = true;

    [STAThread]
    static void Main()
    {
        InitializeWindow();
        InitializeGraphics();
        InitializeAudio();

        Console.WriteLine("Cubix Engine Initialized.");

        // --- The Game Loop ---
        while (_isRunning && !_window.IsDisposed)
        {
            // 1. Handle Window Events
            Application.DoEvents();

            // 2. Handle Input
            HandleInput();

            // 3. Render Frame
            Render();
        }

        Cleanup();
    }

    private static void InitializeWindow()
    {
        _window = new Form
        {
            Text = "Cubix - DirectX Engine",
            Width = 1280,
            Height = 720,
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false
        };

        _window.FormClosed += (s, e) => _isRunning = false;
        _window.Show();
    }

    private static void InitializeGraphics()
    {
        var swapChainDesc = new SwapChainDescription()
        {
            BufferCount = 1,
            ModeDescription = new ModeDescription(_window.Width, _window.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
            Usage = Usage.RenderTargetOutput,
            OutputHandle = _window.Handle,
            SampleDescription = new SampleDescription(1, 0),
            IsWindowed = true,
            SwapEffect = SwapEffect.Discard
        };

        // Create Device and SwapChain
        D3D11.D3D11CreateDeviceAndSwapChain(
            null,
            DriverType.Hardware,
            SwapChainFlags.None,
            [FeatureLevel.Level_11_0],
            swapChainDesc,
            out _swapChain,
            out _device,
            out var featureLevel,
            out var context
        ).CheckError();

        _context = context!;

        // Create Render Target View (Backbuffer)
        using var backBuffer = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderView = _device.CreateRenderTargetView(backBuffer);
    }

    private static void InitializeAudio()
    {
        // Initialize XAudio2
        XAudio2.XAudio2Create(out _audioEngine).CheckError();
        
        // Create the mastering voice (the speakers)
        _masterVoice = _audioEngine.CreateMasteringVoice();
    }

    private static void HandleInput()
    {
        // Example: Check Controller 1 via XInput
        if (XInput.XInputGetState(0, out var state))
        {
            // Controller is connected
            if (state.Gamepad.Buttons.HasFlag(GamepadButtons.A))
            {
                // 'A' button logic here
                _window.Text = "Cubix - Button A Pressed";
            }
        }
        
        // Note: Keyboard/Mouse is usually handled via WinForms KeyDown events 
        // or RawInput for high-precision engines.
    }

    private static void Render()
    {
        // 1. Clear the screen (CornflowerBlue is the traditional DirectX "Hello World" color)
        _context.ClearRenderTargetView(_renderView, new Color4(0.39f, 0.58f, 0.93f, 1.0f));

        // 2. 3D Rendering logic would go here...

        // 3. Swap buffers (SyncInterval 1 = VSync On)
        _swapChain.Present(1, PresentFlags.None);
    }

    private static void Cleanup()
    {
        // Dispose resources in reverse order of creation
        _masterVoice?.Dispose();
        _audioEngine?.Dispose();
        _renderView?.Dispose();
        _context?.Dispose();
        _device?.Dispose();
        _swapChain?.Dispose();
    }
}