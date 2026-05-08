using NiumaQuest.Enum;

namespace NiumaQuest.Signal
{
    /// <summary>
    /// 任务推进信号。
    /// 对话、交互、背包、剧情等模块都可以通过该信号请求任务模块推进目标。
    /// </summary>
    public readonly struct QuestSignal
    {
        /// <summary>
        /// 信号对应的任务目标类型。
        /// </summary>
        public readonly QuestObjectiveType Type;

        /// <summary>
        /// 信号目标 ID。
        /// 例如 DialogueId、InteractableId、ItemId、AreaId、PuzzleId。
        /// </summary>
        public readonly string TargetId;

        /// <summary>
        /// 本次信号增加的进度数量。
        /// </summary>
        public readonly int Count;

        /// <summary>
        /// 信号来源模块名，用于调试和日志追踪。
        /// </summary>
        public readonly string SourceModule;

        public QuestSignal(QuestObjectiveType type, string targetId, int count = 1, string sourceModule = null)
        {
            Type = type;
            TargetId = targetId;
            Count = count < 1 ? 1 : count;
            SourceModule = sourceModule;
        }
    }
}
