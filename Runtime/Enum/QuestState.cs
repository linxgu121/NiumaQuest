namespace NiumaQuest.Enum
{
    /// <summary>
    /// 任务整体状态。
    /// </summary>
    public enum QuestState
    {
        /// <summary>
        /// 未解锁，玩家当前不可接取。
        /// </summary>
        Locked = 0,

        /// <summary>
        /// 已解锁，玩家可以接取。
        /// </summary>
        Available = 1,

        /// <summary>
        /// 已接取，任务正在进行中。
        /// </summary>
        Accepted = 2,

        /// <summary>
        /// 任务目标已完成，但奖励可能还未领取。
        /// </summary>
        Completed = 3,

        /// <summary>
        /// 正在发放奖励，或正在等待外部模块确认奖励结果。
        /// </summary>
        RewardPending = 6,

        /// <summary>
        /// 任务失败，常用于限时任务、分支任务或不可逆选择。
        /// </summary>
        Failed = 4,

        /// <summary>
        /// 存档或配置迁移失败。
        /// 例如存档中的 CurrentStageId 在当前 QuestAsset 中已经不存在。
        /// </summary>
        MigrationFailed = 5,

        /// <summary>
        /// 奖励已领取，任务完全结束。
        /// </summary>
        Rewarded = 7
    }
}
