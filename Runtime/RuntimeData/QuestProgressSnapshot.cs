using System;
using NiumaQuest.Enum;

namespace NiumaQuest.RuntimeData
{
    /// <summary>
    /// 单个任务的存档快照。
    /// 只保存稳定 ID 和轻量进度，不保存 ScriptableObject 引用。
    /// </summary>
    [Serializable]
    public sealed class QuestProgressSnapshot
    {
        /// <summary>
        /// 任务唯一 ID。
        /// </summary>
        public string QuestId;

        /// <summary>
        /// 保存时的任务状态。
        /// </summary>
        public QuestState State;

        /// <summary>
        /// 当前阶段 ID。
        /// 不保存 CurrentStageIndex，避免阶段顺序变化导致旧存档指向错误阶段。
        /// </summary>
        public string CurrentStageId;

        /// <summary>
        /// 是否为玩家当前追踪任务。
        /// </summary>
        public bool IsTracked;

        /// <summary>
        /// 目标进度快照。
        /// </summary>
        public QuestObjectiveProgressSnapshot[] Objectives = Array.Empty<QuestObjectiveProgressSnapshot>();
    }
}
