using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkCanvasForClass_Remastered.Enums;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using System.Windows.Controls;

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
        private AppMode _appMode = AppMode.Normal;
        [ObservableProperty]
        private InkCanvasEditingMode _appPenMode = InkCanvasEditingMode.None;
        [ObservableProperty]
        private string _nowTime;
        [ObservableProperty]
        private string _nowDate;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardPreviousPageButtonEnabled))]
        private int _whiteboardCurrentPage = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsWhiteboardPreviousPageButtonEnabled))]
        private int _whiteboardTotalPageCount = 1;

        public bool IsWhiteboardPreviousPageButtonEnabled => WhiteboardCurrentPage > 1;

        [ObservableProperty]
        private bool _isFloatingBarVisible = true;
        [ObservableProperty]
        private bool _canUndo = false;
        [ObservableProperty]
        private bool _canRedo = false;
        [ObservableProperty]
        private bool _isSettingsPanelVisible = false;

        [RelayCommand]
        private void OpenSettingsPanel()
        {
            IsSettingsPanelVisible = true;
            App.GetService<MainWindow>().HideToolsPanel();
        }
        [RelayCommand]
        private void CloseSettingsPanel()
        {
            IsSettingsPanelVisible = false;
        }

        [RelayCommand]
        private void CrashTest()
        {
            throw new Exception("Crash Test");
        }
    }
}
