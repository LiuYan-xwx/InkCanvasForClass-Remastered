using CommunityToolkit.Mvvm.ComponentModel;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class RandViewModel : ObservableRecipient
    {
        private readonly SettingsService _settingsService;

        public RandViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public Settings Settings => _settingsService.Settings;

        [ObservableProperty]
        private int _drawCount = 1;
        [ObservableProperty]
        private bool _isNoDuplicate = true;
        [ObservableProperty]
        private ObservableCollection<string> _selectedNames = new();
    }
}
