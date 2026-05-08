namespace NiumaQuest.Event
{
    /// <summary>
    /// 任务阶段按顺序推进事件。
    /// 与 QuestStageChangedEvent 分开，避免事件总线订阅者收到重复的同类型阶段事件。
    /// </summary>
    public readonly struct QuestStageAdvancedEvent
    {
        /// <summary>
        /// 所属任务 ID。
        /// </summary>
        public readonly string QuestId;

        /// <summary>
        /// 推进前的阶段 ID。
        /// </summary>
        public readonly string PreviousStageId;

        /// <summary>
        /// 推进后的阶段 ID。
        /// </summary>
        public readonly string CurrentStageId;

        public QuestStageAdvancedEvent(string questId, string previousStageId, string currentStageId)
        {
            QuestId = questId;
            PreviousStageId = previousStageId;
            CurrentStageId = currentStageId;
        }
    }
}
