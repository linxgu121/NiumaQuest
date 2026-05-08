using System;
using NiumaQuest.Enum;
using UnityEngine;

namespace NiumaQuest.Data
{
    /// <summary>
    /// 任务目标配置。
    /// 目标通过 QuestSignal 推进，不直接依赖对话、交互、背包等模块实现。
    /// </summary>
    [Serializable]
    public sealed class QuestObjectiveData
    {
        [Tooltip("目标唯一 ID。用于存档恢复和 UI 进度刷新，不能依赖数组下标。")]
        public string ObjectiveId;

        [Tooltip("目标类型。例如对话、交互、收集、进入区域或自定义信号。")]
        public QuestObjectiveType Type = QuestObjectiveType.CustomSignal;

        [Tooltip("目标 ID。比如 DialogueId、InteractableId、ItemId、AreaId 或 PuzzleId。")]
        public string TargetId;

        [Tooltip("需要完成的次数，最小为 1。Talk 默认建议为 1，只有多次对话触发彩蛋或隐藏任务时才设置大于 1。")]
        [Min(1)]
        public int RequiredCount = 1;

        [Tooltip("目标描述。用于任务追踪 UI，例如：收集草药 0/3。")]
        [TextArea]
        public string Description;

        [Tooltip("该目标是否在任务追踪 UI 中显示。隐藏目标适合暗线、彩蛋或内部推进条件。")]
        public bool HiddenInTracker;
    }
}
