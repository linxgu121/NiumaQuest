namespace NiumaQuest.Bridge
{
    /// <summary>
    /// 任务 UI 更新数据。
    /// 该结构只承载版本号和全量追踪表现数据，避免细粒度字段与 TrackerData 互相打架。
    /// </summary>
    public readonly struct QuestUIUpdate
    {
        /// <summary>
        /// 更新类型。
        /// </summary>
        public readonly QuestUIUpdateType UpdateType;

        /// <summary>
        /// 任务数据版本号。
        /// </summary>
        public readonly int Revision;

        /// <summary>
        /// 当前追踪任务表现数据。
        /// 当前没有追踪任务时为空。
        /// </summary>
        public readonly QuestTrackerViewData TrackerData;

        /// <summary>
        /// 上一次追踪任务表现数据。
        /// 当 UpdateType 为 Cleared 时，UI 可用它判断被清除任务的最终状态。
        /// </summary>
        public readonly QuestTrackerViewData PreviousTrackerData;

        /// <summary>
        /// 当前是否存在追踪任务。
        /// </summary>
        public bool HasTrackedQuest => TrackerData != null;

        public QuestUIUpdate(
            QuestUIUpdateType updateType,
            int revision,
            QuestTrackerViewData trackerData,
            QuestTrackerViewData previousTrackerData)
        {
            UpdateType = updateType;
            Revision = revision;
            TrackerData = trackerData;
            PreviousTrackerData = previousTrackerData;
        }
    }
}
