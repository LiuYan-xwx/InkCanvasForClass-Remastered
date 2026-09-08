using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Ink;

namespace InkCanvasForClass_Remastered.Models
{
    public partial class WhiteboardPage(int index) : ObservableObject
    {
        public int Index { get; } = index;

        [ObservableProperty]
        public partial StrokeCollection Strokes { get; set; } = new();
    }
}
