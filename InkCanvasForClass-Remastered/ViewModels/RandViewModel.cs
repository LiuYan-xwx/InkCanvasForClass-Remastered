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
        private const int SingleColumnThreshold = 3;
        private const int TwoColumnThreshold = 6;

        private readonly SettingsService _settingsService;

        public RandViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
            SelectedNames.CollectionChanged += OnSelectedNamesChanged;
        }

        public Settings Settings => _settingsService.Settings;

        [ObservableProperty]
        private int _drawCount = 1;
        [ObservableProperty]
        private bool _isNoDuplicate = true;
        [ObservableProperty]
        private ObservableCollection<string> _selectedNames = new();

        private void OnSelectedNamesChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Note: This fires on every collection change. For small collections (max 10 items),
            // the performance impact is negligible. DisplayColumns calculation is trivial.
            OnPropertyChanged(nameof(DisplayColumns));
        }

        public int DisplayColumns
        {
            get
            {
                int count = SelectedNames.Count;
                if (count <= SingleColumnThreshold) return 1;
                if (count <= TwoColumnThreshold) return 2;
                return 3;
            }
        }
    }
}
