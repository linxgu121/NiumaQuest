namespace NiumaQuest.Event
{
    /// <summary>
    /// 任务失败事件。
    /// </summary>
    public readonly struct QuestFailedEvent
    {
        /// <summary>
        /// 失败的任务 ID。
        /// </summary>
        public readonly string QuestId;

        /// <summary>
        /// 失败时所在阶段 ID。
        /// </summary>
        public readonly string StageId;

        public QuestFailedEvent(string questId, string stageId)
        {
            QuestId = questId;
            StageId = stageId;
        }
    }
}
