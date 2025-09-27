using CommunityToolkit.Mvvm.ComponentModel;
using InkCanvasForClass_Remastered.Services;
using System.Windows;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class PPTButtonViewModel : ObservableObject
    {
        private readonly IPowerPointService _powerPointService;

        public PPTButtonViewModel(IPowerPointService powerPointService)
        {
            _powerPointService = powerPointService;
        }

        public IPowerPointService PowerPointService => _powerPointService;

        [ObservableProperty]
        private bool _isLeftBottomVisible = true;

        [ObservableProperty]
        private bool _isRightBottomVisible = true;

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

        public bool IsNavigationEnabled => _powerPointService.IsInSlideShow;

        public bool CanGoToPrevious => _powerPointService.CurrentSlidePosition > 1;

        public bool CanGoToNext => _powerPointService.IsInSlideShow && 
                                   _powerPointService.CurrentSlidePosition < _powerPointService.CurrentPresentationSlideCount;

        public string CurrentPageText => _powerPointService.IsInSlideShow 
            ? _powerPointService.CurrentSlidePosition.ToString() 
            : "1";

        public string TotalPageText => _powerPointService.IsInSlideShow 
            ? _powerPointService.CurrentPresentationSlideCount.ToString() 
            : "1";

        public void UpdateFromSettings(int displayOption, int sideButtonsOption, int bottomButtonsOption, int leftPosition, int rightPosition)
        {
            var displayOptions = displayOption.ToString().PadLeft(4, '1');
            IsLeftBottomVisible = displayOptions[0] == '2';
            IsRightBottomVisible = displayOptions[1] == '2';
            IsLeftSideVisible = displayOptions[2] == '2';
            IsRightSideVisible = displayOptions[3] == '2';

            var sideOptions = sideButtonsOption.ToString().PadLeft(3, '1');
            ShowPageNumbers = sideOptions[0] == '2';
            UseHalfOpacity = sideOptions[1] == '2';
            UseBlackBackground = sideOptions[2] == '2';

            LeftSidePosition = leftPosition;
            RightSidePosition = rightPosition;
        }

        public void RefreshNavigationState()
        {
            OnPropertyChanged(nameof(IsNavigationEnabled));
            OnPropertyChanged(nameof(CanGoToPrevious));
            OnPropertyChanged(nameof(CanGoToNext));
            OnPropertyChanged(nameof(CurrentPageText));
            OnPropertyChanged(nameof(TotalPageText));
        }
    }
}