using System;
using NiumaQuest.Enum;
using UnityEngine;

namespace NiumaQuest.Data
{
    /// <summary>
    /// 任务阶段配置。
    /// 一个任务可以由多个阶段组成，例如对话、收集、回交任务。
    /// </summary>
    [Serializable]
    public sealed class QuestStageData
    {
        [Tooltip("阶段唯一 ID。必须稳定，存档会保存该 ID，不保存数组下标。")]
        public string StageId;

        [Tooltip("阶段描述。通常用于任务追踪界面展示当前要做什么。")]
        [TextArea]
        public string Description;

        [Tooltip("当前阶段包含的任务目标。")]
        public QuestObjectiveData[] Objectives = Array.Empty<QuestObjectiveData>();

        [Tooltip("当前阶段的完成模式。All=全部目标完成，Any=任意目标完成，Manual=必须由脚本显式推进。")]
        public QuestCompleteMode CompleteMode = QuestCompleteMode.All;
    }
}
