using CommunityToolkit.Mvvm.ComponentModel;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private readonly IPowerPointService _powerPointService;
        
        public MainViewModel(SettingsService settingsService, IPowerPointService powerPointService)
        {
            _settingsService = settingsService;
            _powerPointService = powerPointService;
            PPTButton = new PPTButtonViewModel(powerPointService);
            UpdatePPTButtonFromSettings();
        }
        
        public Settings Settings => _settingsService.Settings;
        public IPowerPointService PowerPointService => _powerPointService;
        public PPTButtonViewModel PPTButton { get; }

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

        public void UpdatePPTButtonFromSettings()
        {
            PPTButton.UpdateFromSettings(
                Settings.PPTButtonsDisplayOption,
                Settings.PPTSButtonsOption,
                Settings.PPTBButtonsOption,
                Settings.PPTLSButtonPosition,
                Settings.PPTRSButtonPosition);
        }

        public void RefreshPPTButtonState()
        {
            PPTButton.RefreshNavigationState();
        }
    }
}
