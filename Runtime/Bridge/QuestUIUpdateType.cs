namespace NiumaQuest.Bridge
{
    /// <summary>
    /// 任务 UI 更新类型。
    /// 数据驱动 UI 只关心当前是否有追踪数据，不表达接取、完成等瞬时事件。
    /// </summary>
    public enum QuestUIUpdateType
    {
        /// <summary>
        /// 当前存在追踪任务，需要刷新追踪 UI。
        /// </summary>
        Refresh,

        /// <summary>
        /// 当前没有追踪任务，UI 可以隐藏任务追踪面板。
        /// </summary>
        Cleared
    }
}
