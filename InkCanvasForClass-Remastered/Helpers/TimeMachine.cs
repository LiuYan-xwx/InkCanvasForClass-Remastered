using System.Windows.Ink;
using System.Windows.Input;

namespace InkCanvasForClass_Remastered.Helpers
{
    public class TimeMachine
    {
        private readonly List<TimeMachineHistory> _currentStrokeHistory = new();

        private int _currentIndex = -1;

        public delegate void OnUndoStateChange(bool status);

        public event OnUndoStateChange OnUndoStateChanged;

        public delegate void OnRedoStateChange(bool status);

        public event OnRedoStateChange OnRedoStateChanged;

        private void CommitHistory(TimeMachineHistory history)
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, _currentStrokeHistory.Count - 1 - _currentIndex);
            }
            _currentStrokeHistory.Add(history);
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
        }

        public void CommitStrokeUserInputHistory(StrokeCollection stroke)
        {
            CommitHistory(new TimeMachineHistory(stroke, TimeMachineHistoryType.UserInput, false));
        }

        public void CommitStrokeManipulationHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary)
        {
            CommitHistory(new TimeMachineHistory(stylusPointDictionary, TimeMachineHistoryType.Manipulation));
        }

        public void CommitStrokeDrawingAttributesHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes)
        {
            CommitHistory(new TimeMachineHistory(drawingAttributes, TimeMachineHistoryType.DrawingAttributes));
        }

        public void CommitStrokeEraseHistory(StrokeCollection? stroke, StrokeCollection? sourceStroke = null)
        {
            CommitHistory(new TimeMachineHistory(stroke, TimeMachineHistoryType.Clear, true, sourceStroke));
        }

        public void ClearStrokeHistory()
        {
            _currentStrokeHistory.Clear();
            _currentIndex = -1;
            NotifyUndoRedoState();
        }

        public TimeMachineHistory Undo()
        {
            TimeMachineHistory item = _currentStrokeHistory[_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            _currentIndex--;
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            OnRedoStateChanged?.Invoke(_currentStrokeHistory.Count - _currentIndex - 1 > 0);
            return item;
        }

        public TimeMachineHistory Redo()
        {
            TimeMachineHistory item = _currentStrokeHistory[++_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            NotifyUndoRedoState();
            return item;
        }

        public TimeMachineHistory[] ExportTimeMachineHistory()
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, _currentStrokeHistory.Count - 1 - _currentIndex);
            }
            return _currentStrokeHistory.ToArray();
        }

        public bool ImportTimeMachineHistory(TimeMachineHistory[] sourceHistory)
        {
            _currentStrokeHistory.Clear();
            _currentStrokeHistory.AddRange(sourceHistory);
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
            return true;
        }
        private void NotifyUndoRedoState()
        {
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            OnRedoStateChanged?.Invoke(_currentStrokeHistory.Count - _currentIndex - 1 > 0);
        }
    }

    public class TimeMachineHistory
    {
        public TimeMachineHistoryType CommitType { get; set; }
        public bool StrokeHasBeenCleared { get; set; }
        public StrokeCollection? CurrentStroke { get; set; }
        public StrokeCollection? ReplacedStroke { get; set; }
        //这里说一下 Tuple的 Value1 是初始值 ; Value 2 是改变值
        public Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>>? StylusPointDictionary { get; set; }
        public Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>? DrawingAttributes { get; set; }

        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = null;
        }

        public TimeMachineHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary, TimeMachineHistoryType commitType)
        {
            CommitType = commitType;
            StylusPointDictionary = stylusPointDictionary;
        }

        public TimeMachineHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes, TimeMachineHistoryType commitType)
        {
            CommitType = commitType;
            DrawingAttributes = drawingAttributes;
        }

        public TimeMachineHistory(StrokeCollection? currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared, StrokeCollection? replacedStroke)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = replacedStroke;
        }
    }

    public enum TimeMachineHistoryType
    {
        UserInput,
        Clear,
        Manipulation,
        DrawingAttributes
    }
}