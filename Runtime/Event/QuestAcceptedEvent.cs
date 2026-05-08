namespace NiumaQuest.Event
{
    /// <summary>
    /// 任务接取事件。
    /// </summary>
    public readonly struct QuestAcceptedEvent
    {
        /// <summary>
        /// 被接取的任务 ID。
        /// </summary>
        public readonly string QuestId;

        /// <summary>
        /// 接取后的当前阶段 ID。
        /// </summary>
        public readonly string StageId;

        /// <summary>
        /// 接取后是否被自动追踪。
        /// </summary>
        public readonly bool IsTracked;

        public QuestAcceptedEvent(string questId, string stageId, bool isTracked)
        {
            QuestId = questId;
            StageId = stageId;
            IsTracked = isTracked;
        }
    }
}
