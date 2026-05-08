namespace NiumaQuest.Enum
{
    /// <summary>
    /// 任务目标类型。
    /// </summary>
    public enum QuestObjectiveType
    {
        /// <summary>
        /// 完成指定对话。
        /// </summary>
        Talk,

        /// <summary>
        /// 与指定交互目标交互。
        /// </summary>
        Interact,

        /// <summary>
        /// 收集指定物品。
        /// </summary>
        Collect,

        /// <summary>
        /// 击败指定敌人。
        /// 战斗模块完成前可先不实现。
        /// </summary>
        Kill,

        /// <summary>
        /// 进入指定区域。
        /// </summary>
        EnterArea,

        /// <summary>
        /// 完成指定解谜。
        /// </summary>
        PuzzleSolved,

        /// <summary>
        /// 自定义任务信号。
        /// 用于特殊机关、隐藏任务、彩蛋等非标准目标。
        /// </summary>
        CustomSignal
    }
}
