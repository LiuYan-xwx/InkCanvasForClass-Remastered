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
            SelectedNames.CollectionChanged += (s, e) => OnPropertyChanged(nameof(DisplayColumns));
        }

        public Settings Settings => _settingsService.Settings;

        [ObservableProperty]
        private int _drawCount = 1;
        [ObservableProperty]
        private bool _isNoDuplicate = true;
        [ObservableProperty]
        private ObservableCollection<string> _selectedNames = new();

        public int DisplayColumns
        {
            get
            {
                int count = SelectedNames.Count;
                if (count <= 3) return 1;
                if (count <= 6) return 2;
                return 3;
            }
        }
    }
}
