using CommunityToolkit.Mvvm.ComponentModel;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class NamesInputViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial string NameText { get; set; } = string.Empty;
    }
}
