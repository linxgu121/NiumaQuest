namespace NiumaQuest.Enum
{
    /// <summary>
    /// 任务阶段完成模式。
    /// </summary>
    public enum QuestCompleteMode
    {
        /// <summary>
        /// 当前阶段所有目标完成后，阶段自动完成。
        /// </summary>
        All,

        /// <summary>
        /// 当前阶段任意一个目标完成后，阶段自动完成。
        /// </summary>
        Any,

        /// <summary>
        /// 当前阶段只能由剧情、脚本或任务效果显式推进。
        /// </summary>
        Manual
    }
}
