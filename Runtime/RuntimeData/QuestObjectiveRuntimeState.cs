using System;
using NiumaQuest.Data;
using NiumaQuest.Enum;

namespace NiumaQuest.RuntimeData
{
    /// <summary>
    /// 单个任务目标的运行时进度。
    /// </summary>
    [Serializable]
    public sealed class QuestObjectiveRuntimeState
    {
        /// <summary>
        /// 表示运行期缓存是否已经从 QuestObjectiveData 刷新过。
        /// 从存档恢复后该值默认为 false，任务服务必须调用 RefreshFromConfig。
        /// </summary>
        [NonSerialized]
        public bool HasConfigCache;

        /// <summary>
        /// 目标唯一 ID，对应 QuestObjectiveData.ObjectiveId。
        /// </summary>
        public string ObjectiveId;

        /// <summary>
        /// 目标类型运行期缓存，来源于当前 QuestObjectiveData。
        /// 该字段不应写入存档，加载存档后请调用 RefreshFromConfig 同步当前配置。
        /// </summary>
        [NonSerialized]
        public QuestObjectiveType Type;

        /// <summary>
        /// 目标 ID 运行期缓存，来源于当前 QuestObjectiveData。
        /// 该字段不应写入存档，加载存档后请调用 RefreshFromConfig 同步当前配置。
        /// </summary>
        [NonSerialized]
        public string TargetId;

        /// <summary>
        /// 当前已完成次数。
        /// </summary>
        public int CurrentCount;

        /// <summary>
        /// 需要完成次数运行期缓存，来源于当前 QuestObjectiveData。
        /// 该字段不应写入存档，避免配置 RequiredCount 调整后与旧存档冲突。
        /// </summary>
        [NonSerialized]
        public int RequiredCount = 1;

        /// <summary>
        /// 当前目标是否已完成。
        /// </summary>
        public bool IsCompleted;

        /// <summary>
        /// 显式导出目标进度快照。
        /// 存档只保存玩家进度，不保存 Type、TargetId、RequiredCount 等配置缓存。
        /// </summary>
        public QuestObjectiveProgressSnapshot ToSnapshot()
        {
            if (!HasConfigCache)
            {
                return null;
            }

            return new QuestObjectiveProgressSnapshot
            {
                ObjectiveId = ObjectiveId,
                CurrentCount = CurrentCount,
                IsCompleted = IsCompleted
            };
        }

        /// <summary>
        /// 使用当前配置校验运行时缓存是否仍然一致。
        /// 该方法只做校验，不修改运行时状态。
        /// </summary>
        public bool ValidateAgainst(QuestObjectiveData config, out string message)
        {
            if (!HasConfigCache)
            {
                message = $"任务目标尚未刷新配置缓存：{ObjectiveId}";
                return false;
            }

            if (config == null)
            {
                message = $"任务目标配置不存在：{ObjectiveId}";
                return false;
            }

            if (!string.Equals(ObjectiveId, config.ObjectiveId, StringComparison.Ordinal))
            {
                message = $"任务目标 ID 不一致：运行时={ObjectiveId}，配置={config.ObjectiveId}";
                return false;
            }

            if (Type != config.Type)
            {
                message = $"任务目标类型已变化：{ObjectiveId}，运行时={Type}，配置={config.Type}";
                return false;
            }

            if (!string.Equals(TargetId, config.TargetId, StringComparison.Ordinal))
            {
                message = $"任务目标 TargetId 已变化：{ObjectiveId}，运行时={TargetId}，配置={config.TargetId}";
                return false;
            }

            var requiredCount = config.RequiredCount < 1 ? 1 : config.RequiredCount;
            if (RequiredCount != requiredCount)
            {
                message = $"任务目标 RequiredCount 已变化：{ObjectiveId}，运行时={RequiredCount}，配置={requiredCount}";
                return false;
            }

            message = null;
            return true;
        }

        /// <summary>
        /// 使用当前配置刷新运行时缓存。
        /// RequiredCount 变化属于可迁移配置变更：更新缓存并重新计算完成状态，不直接判定任务失败。
        /// </summary>
        public bool RefreshFromConfig(QuestObjectiveData config, out string message)
        {
            if (config == null)
            {
                message = $"任务目标配置不存在：{ObjectiveId}";
                return false;
            }

            if (!string.Equals(ObjectiveId, config.ObjectiveId, StringComparison.Ordinal))
            {
                message = $"任务目标 ID 不一致：运行时={ObjectiveId}，配置={config.ObjectiveId}";
                return false;
            }

            if (!HasConfigCache)
            {
                Type = config.Type;
                TargetId = config.TargetId;
                RequiredCount = config.RequiredCount < 1 ? 1 : config.RequiredCount;
                IsCompleted = CurrentCount >= RequiredCount;
                HasConfigCache = true;
                message = $"任务目标配置缓存已初始化：{ObjectiveId}";
                return true;
            }

            if (Type != config.Type)
            {
                message = $"任务目标类型已变化：{ObjectiveId}，运行时={Type}，配置={config.Type}";
                return false;
            }

            if (!string.Equals(TargetId, config.TargetId, StringComparison.Ordinal))
            {
                message = $"任务目标 TargetId 已变化：{ObjectiveId}，运行时={TargetId}，配置={config.TargetId}";
                return false;
            }

            var requiredCount = config.RequiredCount < 1 ? 1 : config.RequiredCount;
            if (RequiredCount != requiredCount)
            {
                RequiredCount = requiredCount;
                IsCompleted = CurrentCount >= RequiredCount;
                message = $"任务目标 RequiredCount 已同步：{ObjectiveId}，当前进度={CurrentCount}，新需求={RequiredCount}";
                return true;
            }

            IsCompleted = CurrentCount >= RequiredCount;
            message = null;
            return true;
        }
    }
}
