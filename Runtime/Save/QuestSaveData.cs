using System;
using NiumaQuest.RuntimeData;

namespace NiumaQuest.Save
{
    /// <summary>
    /// 任务模块存档数据。
    /// 只保存运行时进度快照，不保存 QuestAsset 或场景对象引用。
    /// </summary>
    [Serializable]
    public sealed class QuestSaveData
    {
        /// <summary>
        /// 已产生进度的任务快照列表。
        /// 未解锁且无进度的任务不需要写入存档。
        /// </summary>
        public QuestProgressSnapshot[] Quests = Array.Empty<QuestProgressSnapshot>();
    }
}
