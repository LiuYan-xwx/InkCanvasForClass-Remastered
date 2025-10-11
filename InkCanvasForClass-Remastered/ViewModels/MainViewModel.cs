﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
        }
        
        public Settings Settings => _settingsService.Settings;
        public IPowerPointService PowerPointService => _powerPointService;

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
        [ObservableProperty]
        private bool _isFloatingBarVisible = true;
        [ObservableProperty]
        private bool _canUndo = false;
        [ObservableProperty]
        private bool _canRedo = false;
        [ObservableProperty]
        private bool _isSettingsPanelVisible = false;

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

        [RelayCommand]
        private void CrashTest()
        {
            throw new Exception("Crash Test");
        }
    }
}
