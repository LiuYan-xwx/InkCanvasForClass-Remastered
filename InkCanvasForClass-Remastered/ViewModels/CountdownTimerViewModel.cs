using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;
using System.Windows.Threading;

namespace InkCanvasForClass_Remastered.ViewModels
{
    public partial class CountdownTimerViewModel : ObservableObject, IDisposable
    {
        private enum TimerState { Ready, Editing, Running, Paused, Completed }

        private readonly DispatcherTimer _timer = new();
        private readonly Stopwatch _elapsed = new();
        private TimerState _state;
        private int _hours;
        private int _minutes = 1;
        private int _seconds;
        private bool _isDisposed;

        public CountdownTimerViewModel()
        {
            _timer.Tick += Timer_Tick;
        }

        public event EventHandler? Completed;

        public bool IsRunning => _state == TimerState.Running;
        public bool IsPaused => _state == TimerState.Paused;
        public bool IsEditing => _state == TimerState.Editing;
        public bool IsCompleted => _state == TimerState.Completed;
        public bool ShowCountdownControls => !IsEditing;
        public bool IsTimeHighlighted => IsRunning || IsEditing;
        public string StartButtonHint => IsRunning ? "暂停" : IsPaused ? "继续" : IsCompleted ? "请先重置或重新设置时间" : "开始";
        public string HourText => (DisplaySeconds / 3600).ToString("00");
        public string MinuteText => (DisplaySeconds / 60 % 60).ToString("00");
        public string SecondText => (DisplaySeconds % 60).ToString("00");
        public string TimeText => $"{HourText}:{MinuteText}:{SecondText}";
        public double Progress => IsRunning || IsPaused
            ? Math.Clamp(RemainingTime.TotalSeconds / ConfiguredDuration.TotalSeconds, 0, 1)
            : 0;

        private int DisplaySeconds => (int)Math.Ceiling(RemainingTime.TotalSeconds);
        private TimeSpan ConfiguredDuration => new(0, _hours, _minutes, _seconds);

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HourText), nameof(MinuteText), nameof(SecondText), nameof(TimeText), nameof(Progress))]
        public partial TimeSpan RemainingTime { get; private set; } = TimeSpan.FromMinutes(1);

        [ObservableProperty]
        public partial string StopTimeText { get; private set; } = string.Empty;

        private bool CanStartPause() => !_isDisposed && !IsEditing && !IsCompleted;
        private bool CanReset() => !_isDisposed && (IsRunning || IsPaused || IsCompleted);
        private bool CanEditTime() => !_isDisposed && !IsRunning && !IsPaused;
        private bool CanAdjustTime() => !_isDisposed && IsEditing;

        [RelayCommand(CanExecute = nameof(CanEditTime))]
        private void ToggleTimeEditing()
        {
            if (!CanEditTime()) return;

            if (IsEditing && ConfiguredDuration == TimeSpan.Zero) _seconds = 1;
            RemainingTime = ConfiguredDuration;
            SetState(IsEditing ? TimerState.Ready : TimerState.Editing);
        }

        [RelayCommand(CanExecute = nameof(CanAdjustTime))]
        private void AdjustHours(int delta) => AdjustTime(ref _hours, delta, 100);

        [RelayCommand(CanExecute = nameof(CanAdjustTime))]
        private void AdjustMinutes(int delta) => AdjustTime(ref _minutes, delta, 60);

        [RelayCommand(CanExecute = nameof(CanAdjustTime))]
        private void AdjustSeconds(int delta) => AdjustTime(ref _seconds, delta, 60);

        private void AdjustTime(ref int value, int delta, int limit)
        {
            if (!CanAdjustTime()) return;

            // 保留原有边界行为：超出上限回到 0，低于 0 回到最大值。
            var adjusted = value + delta;
            value = adjusted >= limit ? 0 : adjusted < 0 ? limit - 1 : adjusted;
            RemainingTime = ConfiguredDuration;
        }

        [RelayCommand(CanExecute = nameof(CanStartPause))]
        private void StartPause()
        {
            if (!CanStartPause()) return;

            if (IsRunning)
            {
                UpdateRemainingTime();
                if (!IsRunning) return;
                _elapsed.Stop();
                _timer.Stop();
                SetState(TimerState.Paused);
                return;
            }

            if (!IsPaused)
            {
                _elapsed.Reset();
                RemainingTime = ConfiguredDuration;
                _timer.Interval = TimeSpan.FromMilliseconds(ConfiguredDuration.TotalSeconds switch
                {
                    <= 10 => 20,
                    <= 60 => 30,
                    <= 120 => 50,
                    _ => 100
                });
            }

            UpdateStopTime();
            _elapsed.Start();
            SetState(TimerState.Running);
            _timer.Start();
        }

        [RelayCommand(CanExecute = nameof(CanReset))]
        private void Reset()
        {
            if (!CanReset()) return;

            _elapsed.Reset();
            RemainingTime = ConfiguredDuration;
            if (IsRunning)
            {
                UpdateStopTime();
                _elapsed.Start();
            }
            else
            {
                _timer.Stop();
                SetState(TimerState.Ready);
            }
        }

        private void UpdateStopTime() => StopTimeText = (DateTime.Now + RemainingTime).ToString("t");

        private void Timer_Tick(object? sender, EventArgs e) => UpdateRemainingTime();

        private void UpdateRemainingTime()
        {
            if (!IsRunning || _isDisposed) return;

            var remaining = ConfiguredDuration - _elapsed.Elapsed;
            RemainingTime = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            if (RemainingTime > TimeSpan.Zero) return;

            _timer.Stop();
            _elapsed.Stop();
            SetState(TimerState.Completed);
            Completed?.Invoke(this, EventArgs.Empty);
        }

        private void SetState(TimerState state)
        {
            _state = state;
            OnPropertyChanged(nameof(IsRunning));
            OnPropertyChanged(nameof(IsPaused));
            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(ShowCountdownControls));
            OnPropertyChanged(nameof(IsTimeHighlighted));
            OnPropertyChanged(nameof(StartButtonHint));
            OnPropertyChanged(nameof(Progress));
            StartPauseCommand.NotifyCanExecuteChanged();
            ResetCommand.NotifyCanExecuteChanged();
            ToggleTimeEditingCommand.NotifyCanExecuteChanged();
            AdjustHoursCommand.NotifyCanExecuteChanged();
            AdjustMinutesCommand.NotifyCanExecuteChanged();
            AdjustSecondsCommand.NotifyCanExecuteChanged();
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _timer.Stop();
            _timer.Tick -= Timer_Tick;
            _elapsed.Stop();
            SetState(TimerState.Ready);
        }
    }
}
