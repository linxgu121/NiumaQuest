namespace NiumaQuest.Event
{
    /// <summary>
    /// 任务阶段变化事件。
    /// </summary>
    public readonly struct QuestStageChangedEvent
    {
        /// <summary>
        /// 所属任务 ID。
        /// </summary>
        public readonly string QuestId;

        /// <summary>
        /// 变化前的阶段 ID。
        /// </summary>
        public readonly string PreviousStageId;

        /// <summary>
        /// 变化后的阶段 ID。
        /// </summary>
        public readonly string CurrentStageId;

        /// <summary>
        /// 是否由外部显式推进。
        /// Manual 阶段通常为 true。
        /// </summary>
        public readonly bool IsManual;

        public QuestStageChangedEvent(string questId, string previousStageId, string currentStageId, bool isManual)
        {
            QuestId = questId;
            PreviousStageId = previousStageId;
            CurrentStageId = currentStageId;
            IsManual = isManual;
        }
    }
}
