using CommunityToolkit.Mvvm.ComponentModel;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        public MainViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }
        public Settings Settings => _settingsService.Settings;

        [ObservableProperty]
        private string _nowTime;
        [ObservableProperty]
        private string _nowDate;
        [ObservableProperty]
        private int _whiteboardCurrentPage = 1;
        [ObservableProperty]
        private int _whiteboardTotalPageCount = 1;
        [ObservableProperty]
        private bool _isWhiteboardPreviousPageButtonEnabled = false;

        partial void OnWhiteboardCurrentPageChanged(int value)
        {
            UpdateWhiteboardButtonStates();
        }

        partial void OnWhiteboardTotalPageCountChanged(int value)
        {
            UpdateWhiteboardButtonStates();
        }

        private void UpdateWhiteboardButtonStates()
        {
            IsWhiteboardPreviousPageButtonEnabled = WhiteboardCurrentPage > 1;
        }
    }
}
