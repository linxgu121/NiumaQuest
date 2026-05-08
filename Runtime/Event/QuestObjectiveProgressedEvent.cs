namespace NiumaQuest.Event
{
    /// <summary>
    /// 任务目标进度变化事件。
    /// </summary>
    public readonly struct QuestObjectiveProgressedEvent
    {
        /// <summary>
        /// 所属任务 ID。
        /// </summary>
        public readonly string QuestId;

        /// <summary>
        /// 所属阶段 ID。
        /// </summary>
        public readonly string StageId;

        /// <summary>
        /// 目标 ID。
        /// </summary>
        public readonly string ObjectiveId;

        /// <summary>
        /// 当前进度数量。
        /// </summary>
        public readonly int CurrentCount;

        /// <summary>
        /// 目标需求数量。
        /// </summary>
        public readonly int RequiredCount;

        /// <summary>
        /// 目标是否已经完成。
        /// </summary>
        public readonly bool IsCompleted;

        public QuestObjectiveProgressedEvent(
            string questId,
            string stageId,
            string objectiveId,
            int currentCount,
            int requiredCount,
            bool isCompleted)
        {
            QuestId = questId;
            StageId = stageId;
            ObjectiveId = objectiveId;
            CurrentCount = currentCount;
            RequiredCount = requiredCount;
            IsCompleted = isCompleted;
        }
    }
}
