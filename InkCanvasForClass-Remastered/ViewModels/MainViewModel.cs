using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ISettingsService _settingsService;
        public MainViewModel(ISettingsService settingsService)
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
