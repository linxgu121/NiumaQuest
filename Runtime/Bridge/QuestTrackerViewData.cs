using System;
using NiumaQuest.Enum;

namespace NiumaQuest.Bridge
{
    /// <summary>
    /// 任务追踪 UI 表现数据。
    /// 包含 UI 展示所需的配置文本和运行时进度，避免 UI 层再次查询 QuestAsset。
    /// </summary>
    public sealed class QuestTrackerViewData
    {
        /// <summary>
        /// 任务唯一 ID。
        /// </summary>
        public string QuestId;

        /// <summary>
        /// 任务标题。
        /// </summary>
        public string Title;

        /// <summary>
        /// 任务描述。
        /// </summary>
        public string Description;

        /// <summary>
        /// 当前任务状态。
        /// </summary>
        public QuestState State;

        /// <summary>
        /// 当前阶段 ID。
        /// </summary>
        public string CurrentStageId;

        /// <summary>
        /// 当前阶段描述。
        /// </summary>
        public string StageDescription;

        /// <summary>
        /// 当前是否为追踪任务。
        /// </summary>
        public bool IsTracked;

        /// <summary>
        /// 是否缺少任务配置。
        /// </summary>
        public bool MissingQuestConfig;

        /// <summary>
        /// 是否缺少当前阶段配置。
        /// </summary>
        public bool MissingStageConfig;

        /// <summary>
        /// 当前阶段中应该显示到追踪 UI 的目标列表。
        /// </summary>
        public QuestObjectiveViewData[] Objectives = Array.Empty<QuestObjectiveViewData>();
    }
}
