using System;
using System.Drawing; // Для System.Drawing.Size
using System.Windows.Forms;
using Vortice.XAudio2;
using Vortice.XInput;
using Vortice.Mathematics; // Для Color4
using Cubix.UI;           // Подключаем наше меню

namespace Cubix;

public static class Program
{
    private static Form _window = null!;
    private static bool _isRunning = true;

    private static Graphics _graphics = null!;
    private static IXAudio2 _audioEngine = null!;
    private static IXAudio2MasteringVoice _masterVoice = null!;
    
    // Система Меню
    private static Menu _menu = new Menu();
    
    // Переменная для предотвращения слишком быстрого пролистывания меню
    private static GamepadButtons _prevButtons = GamepadButtons.None;

    [STAThread]
    static void Main()
    {
        InitializeWindow();
        
        _graphics = new Graphics(_window.Handle, _window.ClientSize.Width, _window.ClientSize.Height);
        
        InitializeAudio();

        Console.WriteLine("Cubix Engine Initialized.");
        Console.WriteLine("Use Gamepad D-Pad Up/Down to change colors (Menu simulation). Press A to select.");

        while (_isRunning && !_window.IsDisposed)
        {
            Application.DoEvents();
            
            HandleInput();
            
            UpdateGameLogic(); // Логика игры (обновление цвета)
            
            _graphics.Render();
        }

        Cleanup();
    }

    private static void InitializeWindow()
    {
        _window = new Form
        {
            Text = "Cubix - DirectX Engine",
            ClientSize = new System.Drawing.Size(1280, 720),
            FormBorderStyle = FormBorderStyle.FixedSingle,
            MaximizeBox = false
        };
        _window.FormClosed += (s, e) => _isRunning = false;
        _window.Show();
    }

    private static void InitializeAudio()
    {
        if (XAudio2.XAudio2Create(out _audioEngine!).Failure) return;
        _masterVoice = _audioEngine.CreateMasteringVoice();
    }

    private static void HandleInput()
    {
        // Используем XInput для геймпада №0
        if (XInput.GetState(0, out var state))
        {
            var buttons = state.Gamepad.Buttons;

            // Проверяем нажатие (было отпущено, стало нажато)
            if (buttons.HasFlag(GamepadButtons.DPadDown) && !_prevButtons.HasFlag(GamepadButtons.DPadDown))
            {
                _menu.MoveDown();
            }
            else if (buttons.HasFlag(GamepadButtons.DPadUp) && !_prevButtons.HasFlag(GamepadButtons.DPadUp))
            {
                _menu.MoveUp();
            }
            else if (buttons.HasFlag(GamepadButtons.A) && !_prevButtons.HasFlag(GamepadButtons.A))
            {
                _menu.Select();
                if (_menu.SelectedIndex == 2) _isRunning = false; // Выход
            }

            _prevButtons = buttons;
        }
    }

    private static void UpdateGameLogic()
    {
        // Визуальная обратная связь: меняем цвет фона в зависимости от пункта меню
        switch (_menu.SelectedIndex)
        {
            case 0: // Start Game - Зеленоватый
                _graphics.SetClearColor(new Color4(0.2f, 0.6f, 0.2f, 1.0f));
                break;
            case 1: // Settings - Желтоватый
                _graphics.SetClearColor(new Color4(0.6f, 0.6f, 0.2f, 1.0f));
                break;
            case 2: // Exit - Красноватый
                _graphics.SetClearColor(new Color4(0.6f, 0.2f, 0.2f, 1.0f));
                break;
        }
    }

    private static void Cleanup()
    {
        _masterVoice?.Dispose();
        _audioEngine?.Dispose();
        _graphics?.Dispose();
    }
}