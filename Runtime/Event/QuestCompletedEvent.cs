namespace NiumaQuest.Event
{
    /// <summary>
    /// 任务完成事件。
    /// </summary>
    public readonly struct QuestCompletedEvent
    {
        /// <summary>
        /// 完成的任务 ID。
        /// </summary>
        public readonly string QuestId;

        /// <summary>
        /// 完成时所在阶段 ID。
        /// </summary>
        public readonly string StageId;

        public QuestCompletedEvent(string questId, string stageId)
        {
            QuestId = questId;
            StageId = stageId;
        }
    }
}
