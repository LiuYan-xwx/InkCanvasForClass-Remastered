using System.Windows.Ink;
using System.Windows.Input;

namespace InkCanvasForClass_Remastered.Services
{
    /// <summary>
    /// 时间机器服务，用于管理墨迹画布的撤销和重做历史记录
    /// </summary>
    public class TimeMachineService : ITimeMachineService
    {
        private readonly List<TimeMachineHistory> _currentStrokeHistory = new();

        private int _currentIndex = -1;

        /// <summary>
        /// 撤销状态变化事件
        /// </summary>
        public event Action<bool>? OnUndoStateChanged;

        /// <summary>
        /// 重做状态变化事件
        /// </summary>
        public event Action<bool>? OnRedoStateChanged;

        /// <summary>
        /// 提交历史记录的通用方法
        /// </summary>
        /// <param name="history">要提交的历史记录对象</param>
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

        /// <summary>
        /// 提交用户输入的笔画历史记录
        /// </summary>
        /// <param name="stroke">笔画集合</param>
        public void CommitStrokeUserInputHistory(StrokeCollection stroke)
        {
            CommitHistory(new TimeMachineHistory(stroke, TimeMachineHistoryType.UserInput, false));
        }

        /// <summary>
        /// 提交笔画操作历史记录（移动、缩放等）
        /// </summary>
        /// <param name="stylusPointDictionary">触控点字典，键为笔画，值为初始值和改变值的元组</param>
        public void CommitStrokeManipulationHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary)
        {
            CommitHistory(new TimeMachineHistory(stylusPointDictionary, TimeMachineHistoryType.Manipulation));
        }

        /// <summary>
        /// 提交笔画绘制属性历史记录（颜色、粗细等）
        /// </summary>
        /// <param name="drawingAttributes">绘制属性字典，键为笔画，值为初始值和改变值的元组</param>
        public void CommitStrokeDrawingAttributesHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes)
        {
            CommitHistory(new TimeMachineHistory(drawingAttributes, TimeMachineHistoryType.DrawingAttributes));
        }

        /// <summary>
        /// 提交笔画擦除历史记录
        /// </summary>
        /// <param name="stroke">被擦除的笔画集合</param>
        /// <param name="sourceStroke">源笔画集合（擦除前的笔画）</param>
        public void CommitStrokeEraseHistory(StrokeCollection? stroke, StrokeCollection? sourceStroke = null)
        {
            CommitHistory(new TimeMachineHistory(stroke, TimeMachineHistoryType.Clear, true, sourceStroke));
        }

        /// <summary>
        /// 清空历史记录
        /// </summary>
        public void ClearStrokeHistory()
        {
            _currentStrokeHistory.Clear();
            _currentIndex = -1;
            NotifyUndoRedoState();
        }

        /// <summary>
        /// 撤销操作
        /// </summary>
        /// <returns>需要撤销的历史记录对象</returns>
        public TimeMachineHistory Undo()
        {
            TimeMachineHistory item = _currentStrokeHistory[_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            _currentIndex--;
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            OnRedoStateChanged?.Invoke(_currentStrokeHistory.Count - _currentIndex - 1 > 0);
            return item;
        }

        /// <summary>
        /// 重做操作
        /// </summary>
        /// <returns>需要重做的历史记录对象</returns>
        public TimeMachineHistory Redo()
        {
            TimeMachineHistory item = _currentStrokeHistory[++_currentIndex];
            item.StrokeHasBeenCleared = !item.StrokeHasBeenCleared;
            NotifyUndoRedoState();
            return item;
        }

        /// <summary>
        /// 导出时间机器历史记录
        /// </summary>
        /// <returns>历史记录数组</returns>
        public TimeMachineHistory[] ExportTimeMachineHistory()
        {
            if (_currentIndex + 1 < _currentStrokeHistory.Count)
            {
                _currentStrokeHistory.RemoveRange(_currentIndex + 1, _currentStrokeHistory.Count - 1 - _currentIndex);
            }
            return _currentStrokeHistory.ToArray();
        }

        /// <summary>
        /// 导入时间机器历史记录
        /// </summary>
        /// <param name="sourceHistory">源历史记录数组</param>
        /// <returns>导入是否成功</returns>
        public bool ImportTimeMachineHistory(TimeMachineHistory[] sourceHistory)
        {
            _currentStrokeHistory.Clear();
            _currentStrokeHistory.AddRange(sourceHistory);
            _currentIndex = _currentStrokeHistory.Count - 1;
            NotifyUndoRedoState();
            return true;
        }

        /// <summary>
        /// 通知撤销和重做状态变化
        /// </summary>
        private void NotifyUndoRedoState()
        {
            OnUndoStateChanged?.Invoke(_currentIndex > -1);
            OnRedoStateChanged?.Invoke(_currentStrokeHistory.Count - _currentIndex - 1 > 0);
        }
    }

    /// <summary>
    /// 时间机器历史记录类，存储墨迹画布操作的历史信息
    /// </summary>
    public class TimeMachineHistory
    {
        /// <summary>
        /// 提交类型
        /// </summary>
        public TimeMachineHistoryType CommitType { get; set; }

        /// <summary>
        /// 笔画是否已被清除（用于撤销/重做方向控制）
        /// </summary>
        public bool StrokeHasBeenCleared { get; set; }

        /// <summary>
        /// 当前笔画集合
        /// </summary>
        public StrokeCollection? CurrentStroke { get; set; }

        /// <summary>
        /// 替换的笔画集合（用于擦除操作）
        /// </summary>
        public StrokeCollection? ReplacedStroke { get; set; }

        /// <summary>
        /// 触控点字典，键为笔画，值为初始值和改变值的元组
        /// </summary>
        //这里说一下 Tuple的 Value1 是初始值 ; Value 2 是改变值
        public Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>>? StylusPointDictionary { get; set; }

        /// <summary>
        /// 绘制属性字典，键为笔画，值为初始值和改变值的元组
        /// </summary>
        public Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>>? DrawingAttributes { get; set; }

        /// <summary>
        /// 创建用户输入或擦除操作的历史记录
        /// </summary>
        /// <param name="currentStroke">当前笔画集合</param>
        /// <param name="commitType">提交类型</param>
        /// <param name="strokeHasBeenCleared">笔画是否已被清除</param>
        public TimeMachineHistory(StrokeCollection currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = null;
        }

        /// <summary>
        /// 创建笔画操作（移动、缩放等）的历史记录
        /// </summary>
        /// <param name="stylusPointDictionary">触控点字典</param>
        /// <param name="commitType">提交类型</param>
        public TimeMachineHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary, TimeMachineHistoryType commitType)
        {
            CommitType = commitType;
            StylusPointDictionary = stylusPointDictionary;
        }

        /// <summary>
        /// 创建绘制属性修改的历史记录
        /// </summary>
        /// <param name="drawingAttributes">绘制属性字典</param>
        /// <param name="commitType">提交类型</param>
        public TimeMachineHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes, TimeMachineHistoryType commitType)
        {
            CommitType = commitType;
            DrawingAttributes = drawingAttributes;
        }

        /// <summary>
        /// 创建清除操作的历史记录
        /// </summary>
        /// <param name="currentStroke">当前笔画集合</param>
        /// <param name="commitType">提交类型</param>
        /// <param name="strokeHasBeenCleared">笔画是否已被清除</param>
        /// <param name="replacedStroke">替换的笔画集合</param>
        public TimeMachineHistory(StrokeCollection? currentStroke, TimeMachineHistoryType commitType, bool strokeHasBeenCleared, StrokeCollection? replacedStroke)
        {
            CommitType = commitType;
            CurrentStroke = currentStroke;
            StrokeHasBeenCleared = strokeHasBeenCleared;
            ReplacedStroke = replacedStroke;
        }
    }

    /// <summary>
    /// 时间机器历史记录类型
    /// </summary>
    public enum TimeMachineHistoryType
    {
        /// <summary>
        /// 用户输入（绘制新笔画）
        /// </summary>
        UserInput,

        /// <summary>
        /// 清除操作（擦除笔画）
        /// </summary>
        Clear,

        /// <summary>
        /// 操作（移动、缩放等）
        /// </summary>
        Manipulation,

        /// <summary>
        /// 绘制属性修改（颜色、粗细等）
        /// </summary>
        DrawingAttributes
    }
}
