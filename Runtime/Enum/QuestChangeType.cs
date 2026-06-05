namespace NiumaQuest.Enum
{
    /// <summary>
    /// 任务变化类型。
    /// 用于通用脏标记、自动保存和调试日志。
    /// </summary>
    public enum QuestChangeType
    {
        /// <summary>
        /// 任务被接取。
        /// </summary>
        Accepted,

        /// <summary>
        /// 任务目标进度发生变化。
        /// </summary>
        ObjectiveProgressed,

        /// <summary>
        /// 当前任务阶段发生变化。
        /// </summary>
        StageChanged,

        /// <summary>
        /// 任务完成。
        /// </summary>
        Completed,

        /// <summary>
        /// 任务已进入待发奖状态。
        /// </summary>
        RewardPending,

        /// <summary>
        /// 任务奖励已成功发放或已命中幂等记录。
        /// </summary>
        Rewarded,

        /// <summary>
        /// 任务失败。
        /// </summary>
        Failed,

        /// <summary>
        /// 任务追踪状态发生变化。
        /// </summary>
        TrackingChanged,

        /// <summary>
        /// 任务配置刷新导致运行时状态发生变化。
        /// </summary>
        ConfigurationRefreshed,

        /// <summary>
        /// 存档或配置迁移失败。
        /// </summary>
        MigrationFailed
    }
}
