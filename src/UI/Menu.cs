using System;

namespace Cubix.UI;

public class Menu
{
    private int _selectedIndex = 0;
    private readonly string[] _options = { "Start Game", "Settings", "Exit" };

    // Возвращает индекс текущего выбранного пункта
    public int SelectedIndex => _selectedIndex;

    public void MoveUp()
    {
        _selectedIndex--;
        if (_selectedIndex < 0) 
            _selectedIndex = _options.Length - 1;
            
        Console.WriteLine($"[Menu] Selected: {_options[_selectedIndex]}");
    }

    public void MoveDown()
    {
        _selectedIndex++;
        if (_selectedIndex >= _options.Length) 
            _selectedIndex = 0;

        Console.WriteLine($"[Menu] Selected: {_options[_selectedIndex]}");
    }

    public void Select()
    {
        Console.WriteLine($"[Menu] Action: {_options[_selectedIndex]}");
    }
}