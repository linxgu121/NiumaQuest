using System;
using System.Collections.Generic;
using NiumaQuest.Enum;

namespace NiumaQuest.RuntimeData
{
    /// <summary>
    /// 单个任务的运行时状态。
    /// 运行时状态不应写回 QuestAsset。
    /// </summary>
    [Serializable]
    public sealed class QuestRuntimeState
    {
        /// <summary>
        /// 任务唯一 ID，对应 QuestAsset.QuestId。
        /// </summary>
        public string QuestId;

        /// <summary>
        /// 当前任务整体状态。
        /// </summary>
        public QuestState State;

        /// <summary>
        /// 当前阶段 ID。
        /// 使用稳定 StageId，不使用数组下标，避免策划调整阶段顺序后读错存档。
        /// </summary>
        public string CurrentStageId;

        /// <summary>
        /// 当前阶段或当前任务相关目标的运行时进度。
        /// </summary>
        public QuestObjectiveRuntimeState[] Objectives = Array.Empty<QuestObjectiveRuntimeState>();

        /// <summary>
        /// 是否正在被 UI 追踪显示。
        /// </summary>
        public bool IsTracked;

        /// <summary>
        /// 显式导出存档快照。
        /// 不要直接序列化 QuestRuntimeState，因为运行时对象可能包含配置缓存或调试字段。
        /// </summary>
        public QuestProgressSnapshot ToSnapshot()
        {
            var objectiveSnapshots = Array.Empty<QuestObjectiveProgressSnapshot>();
            if (Objectives != null && Objectives.Length > 0)
            {
                var validSnapshots = new List<QuestObjectiveProgressSnapshot>(Objectives.Length);
                for (var i = 0; i < Objectives.Length; i++)
                {
                    var objective = Objectives[i];
                    if (objective == null)
                    {
                        continue;
                    }

                    var snapshot = objective.ToSnapshot();
                    if (snapshot != null)
                    {
                        validSnapshots.Add(snapshot);
                    }
                }

                objectiveSnapshots = validSnapshots.ToArray();
            }

            return new QuestProgressSnapshot
            {
                QuestId = QuestId,
                State = State,
                CurrentStageId = CurrentStageId,
                IsTracked = IsTracked,
                Objectives = objectiveSnapshots
            };
        }
    }
}
