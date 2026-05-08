namespace NiumaQuest.Event
{
    /// <summary>
    /// 任务追踪状态变化事件。
    /// </summary>
    public readonly struct QuestTrackingChangedEvent
    {
        /// <summary>
        /// 任务 ID。
        /// </summary>
        public readonly string QuestId;

        /// <summary>
        /// 是否正在追踪。
        /// </summary>
        public readonly bool IsTracked;

        public QuestTrackingChangedEvent(string questId, bool isTracked)
        {
            QuestId = questId;
            IsTracked = isTracked;
        }
    }
}
