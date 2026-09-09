using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InkCanvasForClass_Remastered.Helpers;
using InkCanvasForClass_Remastered.Models;
using InkCanvasForClass_Remastered.Services;
using System.Collections.ObjectModel;
using System.Security.Cryptography;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class RandViewModel : ObservableObject
    {
        private readonly SettingsService _settingsService;
        private List<string> _names = [];
        private ShuffleBag<string> _shuffleBag = new([]);

        public RandViewModel(SettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public Settings Settings => _settingsService.Settings;

        public int NameCount => _names.Count;
        public string NamesButtonText => NameCount == 0 ? "点击此处以导入名单" : NameCount.ToString();
        public ObservableCollection<string> SelectedNames { get; } = [];
        public bool CanEditOptions => !IsDrawing && !IsAutoClose;
        public bool HasEnoughNames => !IsNoDuplicate || DrawCount <= _shuffleBag.RemainingCount;

        public string DrawHint => NameCount == 0
            ? "名单为空，请先导入名单"
            : !HasEnoughNames
                ? "没有足够的未被抽过的人，请减少人数、重新导入名单或关闭不重复抽取"
                : "开始抽取";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DrawHint), nameof(HasEnoughNames))]
        [NotifyCanExecuteChangedFor(nameof(IncreaseDrawCountCommand), nameof(DecreaseDrawCountCommand), nameof(DrawCommand))]
        public partial int DrawCount { get; set; } = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DrawHint), nameof(HasEnoughNames))]
        [NotifyCanExecuteChangedFor(nameof(DrawCommand))]
        public partial bool IsNoDuplicate { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEditOptions))]
        [NotifyCanExecuteChangedFor(nameof(IncreaseDrawCountCommand), nameof(DecreaseDrawCountCommand))]
        public partial bool IsAutoClose { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEditOptions), nameof(DrawHint), nameof(HasEnoughNames))]
        [NotifyCanExecuteChangedFor(nameof(IncreaseDrawCountCommand), nameof(DecreaseDrawCountCommand), nameof(DrawCommand))]
        public partial bool IsDrawing { get; private set; }

        public void SetNames(IEnumerable<string> names)
        {
            _names = names.Where(name => !string.IsNullOrWhiteSpace(name)).ToList();
            _shuffleBag = new(_names);
            DrawCount = Math.Clamp(DrawCount, 1, Math.Max(1, Math.Min(NameCount, 10)));
            SelectedNames.Clear();
            OnPropertyChanged(nameof(NameCount));
            OnPropertyChanged(nameof(NamesButtonText));
            OnPropertyChanged(nameof(DrawHint));
            OnPropertyChanged(nameof(HasEnoughNames));
            IncreaseDrawCountCommand.NotifyCanExecuteChanged();
            DecreaseDrawCountCommand.NotifyCanExecuteChanged();
            DrawCommand.NotifyCanExecuteChanged();
        }

        private bool CanIncreaseDrawCount() => CanEditOptions && DrawCount < Math.Min(NameCount, 10);
        private bool CanDecreaseDrawCount() => CanEditOptions && DrawCount > 1;
        private bool CanDraw() => !IsDrawing && NameCount > 0 && DrawCount >= 1 && DrawCount <= Math.Min(NameCount, 10);

        [RelayCommand(CanExecute = nameof(CanIncreaseDrawCount))]
        private void IncreaseDrawCount()
        {
            if (CanIncreaseDrawCount()) DrawCount++;
        }

        [RelayCommand(CanExecute = nameof(CanDecreaseDrawCount))]
        private void DecreaseDrawCount()
        {
            if (CanDecreaseDrawCount()) DrawCount--;
        }

        [RelayCommand(CanExecute = nameof(CanDraw))]
        private async Task DrawAsync(CancellationToken cancellationToken)
        {
            if (!CanDraw() || !HasEnoughNames) return;

            var names = _names;
            var bag = _shuffleBag;
            var drawCount = DrawCount;
            var noDuplicate = IsNoDuplicate;
            IsDrawing = true;
            try
            {
                for (var elapsed = 0; elapsed < 600; elapsed += 50)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    SelectedNames.Clear();
                    SelectedNames.Add(names[Random.Shared.Next(names.Count)]);
                    await Task.Delay(50, cancellationToken);
                }

                cancellationToken.ThrowIfCancellationRequested();
                SelectedNames.Clear();
                for (var i = 0; i < drawCount; i++)
                    SelectedNames.Add(noDuplicate ? bag.Next() : names[RandomNumberGenerator.GetInt32(names.Count)]);
                OnPropertyChanged(nameof(DrawHint));
                OnPropertyChanged(nameof(HasEnoughNames));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // 窗口关闭时停止动画，不消耗本次抽取名额。
            }
            finally
            {
                IsDrawing = false;
            }
        }
    }
}
