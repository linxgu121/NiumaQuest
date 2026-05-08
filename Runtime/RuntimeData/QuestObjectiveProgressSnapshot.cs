using System;

namespace NiumaQuest.RuntimeData
{
    /// <summary>
    /// 单个任务目标的存档快照。
    /// </summary>
    [Serializable]
    public sealed class QuestObjectiveProgressSnapshot
    {
        /// <summary>
        /// 目标唯一 ID。
        /// </summary>
        public string ObjectiveId;

        /// <summary>
        /// 当前已完成次数。
        /// </summary>
        public int CurrentCount;

        /// <summary>
        /// 保存时该目标是否已经完成。
        /// </summary>
        public bool IsCompleted;
    }
}
