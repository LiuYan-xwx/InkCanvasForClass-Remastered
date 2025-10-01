using CommunityToolkit.Mvvm.ComponentModel;
using InkCanvasForClass_Remastered.Services;
using System.Windows;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class PPTButtonViewModel : ObservableObject
    {
        private readonly IPowerPointService _powerPointService;
        private Action<int, int>? _onSettingsChanged;
        private Action? _onPreviewUpdate;
        private bool _showPPTButton = true;
        private bool _suppressSettingsSave = false;

        public PPTButtonViewModel(IPowerPointService powerPointService)
        {
            _powerPointService = powerPointService;
        }

        public void SetSettingsChangedCallback(Action<int, int> callback)
        {
            _onSettingsChanged = callback;
        }

        public void SetPreviewUpdateCallback(Action callback)
        {
            _onPreviewUpdate = callback;
        }

        public IPowerPointService PowerPointService => _powerPointService;

        [ObservableProperty]
        private bool _isLeftSideVisible = true;

        [ObservableProperty]
        private bool _isRightSideVisible = true;

        [ObservableProperty]
        private bool _showPageNumbers = true;

        [ObservableProperty]
        private bool _useHalfOpacity = true;

        [ObservableProperty]
        private bool _useBlackBackground = false;

        [ObservableProperty]
        private int _leftSidePosition = 0;

        [ObservableProperty]
        private int _rightSidePosition = 0;

        partial void OnIsLeftSideVisibleChanged(bool value)
        {
            SaveSettings();
            UpdatePreview();
        }
        
        partial void OnIsRightSideVisibleChanged(bool value)
        {
            SaveSettings();
            UpdatePreview();
        }
        
        partial void OnShowPageNumbersChanged(bool value)
        {
            SaveSettings();
            UpdatePreview();
        }
        
        partial void OnUseHalfOpacityChanged(bool value)
        {
            SaveSettings();
            UpdatePreview();
        }
        
        partial void OnUseBlackBackgroundChanged(bool value)
        {
            SaveSettings();
            UpdatePreview();
        }

        private void SaveSettings()
        {
            if (_suppressSettingsSave) return;
            _onSettingsChanged?.Invoke(GetDisplayOption(), GetSideButtonsOption());
        }

        private void UpdatePreview()
        {
            if (_suppressSettingsSave) return;
            _onPreviewUpdate?.Invoke();
        }

        public bool IsNavigationEnabled => _powerPointService.IsInSlideShow;

        public bool CanGoToPrevious => _powerPointService.IsInSlideShow && _powerPointService.CurrentSlidePosition > 1;

        public bool CanGoToNext => _powerPointService.IsInSlideShow && 
                                   _powerPointService.CurrentSlidePosition < _powerPointService.CurrentPresentationSlideCount;

        public string CurrentPageText => _powerPointService.IsInSlideShow 
            ? _powerPointService.CurrentSlidePosition.ToString() 
            : "1";

        public string TotalPageText => _powerPointService.IsInSlideShow 
            ? _powerPointService.CurrentPresentationSlideCount.ToString() 
            : "1";

        public Visibility PanelVisibility => _showPPTButton && _powerPointService.IsInSlideShow ? Visibility.Visible : Visibility.Collapsed;

        public void UpdateFromSettings(int displayOption, int sideButtonsOption, int leftPosition, int rightPosition, bool showPPTButton = true)
        {
            _suppressSettingsSave = true;
            
            _showPPTButton = showPPTButton;
            
            var displayOptions = displayOption.ToString().PadLeft(4, '1');
            // Bottom buttons removed - only use side buttons (indices 2 and 3)
            IsLeftSideVisible = displayOptions[2] == '2';
            IsRightSideVisible = displayOptions[3] == '2';

            var sideOptions = sideButtonsOption.ToString().PadLeft(3, '1');
            ShowPageNumbers = sideOptions[0] == '2';
            UseHalfOpacity = sideOptions[1] == '2';
            UseBlackBackground = sideOptions[2] == '2';

            LeftSidePosition = leftPosition;
            RightSidePosition = rightPosition;
            
            OnPropertyChanged(nameof(PanelVisibility));
            
            _suppressSettingsSave = false;
        }

        public int GetDisplayOption()
        {
            // Encode visibility as 4-digit number (only last 2 digits used for side buttons)
            int leftBottom = 1; // Removed
            int rightBottom = 1; // Removed
            int leftSide = IsLeftSideVisible ? 2 : 1;
            int rightSide = IsRightSideVisible ? 2 : 1;
            
            return leftBottom * 1000 + rightBottom * 100 + leftSide * 10 + rightSide;
        }

        public int GetSideButtonsOption()
        {
            // Encode side button options as 3-digit number
            int showPage = ShowPageNumbers ? 2 : 1;
            int halfOpacity = UseHalfOpacity ? 2 : 1;
            int blackBg = UseBlackBackground ? 2 : 1;
            
            return showPage * 100 + halfOpacity * 10 + blackBg;
        }

        public void RefreshNavigationState()
        {
            OnPropertyChanged(nameof(IsNavigationEnabled));
            OnPropertyChanged(nameof(CanGoToPrevious));
            OnPropertyChanged(nameof(CanGoToNext));
            OnPropertyChanged(nameof(CurrentPageText));
            OnPropertyChanged(nameof(TotalPageText));
            OnPropertyChanged(nameof(PanelVisibility));
        }
    }
}