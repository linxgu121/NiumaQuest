using NiumaQuest.Enum;

namespace NiumaQuest.Bridge
{
    /// <summary>
    /// 任务目标 UI 表现数据。
    /// 由桥接层把配置数据和运行时进度合并后生成，UI 层只负责显示。
    /// </summary>
    public sealed class QuestObjectiveViewData
    {
        /// <summary>
        /// 目标唯一 ID。
        /// </summary>
        public string ObjectiveId;

        /// <summary>
        /// 目标类型。
        /// </summary>
        public QuestObjectiveType Type;

        /// <summary>
        /// 目标配置中的业务目标 ID，例如 DialogueId、ItemId、InteractableId。
        /// </summary>
        public string TargetId;

        /// <summary>
        /// 目标描述，用于任务追踪 UI 展示。
        /// </summary>
        public string Description;

        /// <summary>
        /// 当前完成数量。
        /// </summary>
        public int CurrentCount;

        /// <summary>
        /// 目标需求数量。
        /// </summary>
        public int RequiredCount;

        /// <summary>
        /// 目标是否已经完成。
        /// </summary>
        public bool IsCompleted;

        /// <summary>
        /// 是否缺少对应配置。
        /// 为 true 时说明该目标来自存档快照，但当前 QuestAsset 中找不到对应 ObjectiveId。
        /// </summary>
        public bool MissingConfig;
    }
}
