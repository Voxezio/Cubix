using System;
using System.Drawing; // Rectangle
using Cubix;

namespace Cubix.UI.GUI;

public class Button
{
    private Texture _texture;
    private int _sliceWidth; // Ширина одной части (левой, средней или правой)

    public Button(Texture texture)
    {
        _texture = texture;
        // Предполагаем, что текстура состоит из 3-х равных частей: Лево, Центр, Право
        _sliceWidth = texture.Width / 3; 
    }

    public void Draw(SpriteRenderer renderer, int x, int y, int totalWidth, int screenW, int screenH)
    {
        int height = _texture.Height;
        int middleWidth = totalWidth - (_sliceWidth * 2);

        if (middleWidth < 0) middleWidth = 0; // Защита от слишком узких кнопок

        // 1. Левая часть (Start)
        renderer.Draw(
            _texture,
            new Rectangle(x, y, _sliceWidth, height),
            new Rectangle(0, 0, _sliceWidth, height),
            screenW, screenH
        );

        // 2. Середина (Repeating Middle) - растягиваем её
        if (middleWidth > 0)
        {
            renderer.Draw(
                _texture,
                new Rectangle(x + _sliceWidth, y, middleWidth, height),
                new Rectangle(_sliceWidth, 0, _sliceWidth, height),
                screenW, screenH
            );
        }

        // 3. Правая часть (End)
        renderer.Draw(
            _texture,
            new Rectangle(x + _sliceWidth + middleWidth, y, _sliceWidth, height),
            new Rectangle(_sliceWidth * 2, 0, _sliceWidth, height),
            screenW, screenH
        );
    }
}