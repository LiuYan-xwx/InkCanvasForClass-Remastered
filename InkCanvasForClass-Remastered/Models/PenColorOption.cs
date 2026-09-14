using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.Models;

public sealed partial class PenColorOption : ObservableObject
{
    private bool _useLightPalette;

    public PenColorOption(
        int id,
        string name,
        Color color,
        Color? darkColor = null,
        bool useDarkCheckMark = false)
    {
        Id = id;
        Name = name;
        Color = color;
        DarkColor = darkColor ?? color;
        UseDarkCheckMark = useDarkCheckMark;
        Brush = CreateBrush(CurrentColor);
    }

    public int Id { get; }

    public string Name { get; }

    public Color Color { get; }

    public Color DarkColor { get; }

    public bool UseDarkCheckMark { get; }

    public Color CurrentColor => _useLightPalette ? Color : DarkColor;

    [ObservableProperty]
    public partial SolidColorBrush Brush { get; set; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public void SetPalette(bool useLightPalette)
    {
        if (_useLightPalette == useLightPalette) return;

        _useLightPalette = useLightPalette;
        Brush = CreateBrush(CurrentColor);
        OnPropertyChanged(nameof(CurrentColor));
    }

    private static SolidColorBrush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
