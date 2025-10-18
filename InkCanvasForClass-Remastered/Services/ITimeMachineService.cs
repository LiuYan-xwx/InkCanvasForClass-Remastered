using System.Windows.Ink;
using System.Windows.Input;

namespace InkCanvasForClass_Remastered.Services
{
    /// <summary>
    /// 时间机器服务接口，用于管理墨迹画布的撤销和重做历史记录
    /// </summary>
    public interface ITimeMachineService
    {
        /// <summary>
        /// 撤销状态变化事件
        /// </summary>
        event Action<bool> OnUndoStateChanged;

        /// <summary>
        /// 重做状态变化事件
        /// </summary>
        event Action<bool> OnRedoStateChanged;

        /// <summary>
        /// 提交用户输入的笔画历史记录
        /// </summary>
        /// <param name="stroke">笔画集合</param>
        void CommitStrokeUserInputHistory(StrokeCollection stroke);

        /// <summary>
        /// 提交笔画操作历史记录（移动、缩放等）
        /// </summary>
        /// <param name="stylusPointDictionary">触控点字典，键为笔画，值为初始值和改变值的元组</param>
        void CommitStrokeManipulationHistory(Dictionary<Stroke, Tuple<StylusPointCollection, StylusPointCollection>> stylusPointDictionary);

        /// <summary>
        /// 提交笔画绘制属性历史记录（颜色、粗细等）
        /// </summary>
        /// <param name="drawingAttributes">绘制属性字典，键为笔画，值为初始值和改变值的元组</param>
        void CommitStrokeDrawingAttributesHistory(Dictionary<Stroke, Tuple<DrawingAttributes, DrawingAttributes>> drawingAttributes);

        /// <summary>
        /// 提交笔画擦除历史记录
        /// </summary>
        /// <param name="stroke">被擦除的笔画集合</param>
        /// <param name="sourceStroke">源笔画集合（擦除前的笔画）</param>
        void CommitStrokeEraseHistory(StrokeCollection? stroke, StrokeCollection? sourceStroke = null);

        /// <summary>
        /// 清空历史记录
        /// </summary>
        void ClearStrokeHistory();

        /// <summary>
        /// 撤销操作
        /// </summary>
        /// <returns>需要撤销的历史记录对象</returns>
        TimeMachineHistory Undo();

        /// <summary>
        /// 重做操作
        /// </summary>
        /// <returns>需要重做的历史记录对象</returns>
        TimeMachineHistory Redo();

        /// <summary>
        /// 导出时间机器历史记录
        /// </summary>
        /// <returns>历史记录数组</returns>
        TimeMachineHistory[] ExportTimeMachineHistory();

        /// <summary>
        /// 导入时间机器历史记录
        /// </summary>
        /// <param name="sourceHistory">源历史记录数组</param>
        /// <returns>导入是否成功</returns>
        bool ImportTimeMachineHistory(TimeMachineHistory[] sourceHistory);
    }
}
