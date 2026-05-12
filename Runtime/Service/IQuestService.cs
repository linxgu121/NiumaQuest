using System;
using System.Collections.Generic;
using NiumaQuest.Data;
using NiumaQuest.Event;
using NiumaQuest.RuntimeData;
using NiumaQuest.Signal;

namespace NiumaQuest.Service
{
    /// <summary>
    /// 任务服务接口。
    /// 外部模块通过该接口查询和推进任务，不直接修改任务运行时状态。
    /// </summary>
    public interface IQuestService
    {
        /// <summary>
        /// 任意任务数据发生变化时触发。
        /// 存档模块可以监听该事件标记任务存档为脏。
        /// </summary>
        event Action<QuestChangedEvent> OnQuestChanged;

        /// <summary>
        /// 任务接取时触发。
        /// </summary>
        event Action<QuestAcceptedEvent> OnQuestAccepted;

        /// <summary>
        /// 任务目标进度变化时触发。
        /// </summary>
        event Action<QuestObjectiveProgressedEvent> OnObjectiveProgressed;

        /// <summary>
        /// 任务阶段变化时触发。
        /// </summary>
        event Action<QuestStageChangedEvent> OnStageChanged;

        /// <summary>
        /// 任务按阶段顺序推进时触发。
        /// TrySetStage 这种指定跳转只触发 OnStageChanged，不触发该事件。
        /// </summary>
        event Action<QuestStageAdvancedEvent> OnStageAdvanced;

        /// <summary>
        /// 任务完成时触发。
        /// </summary>
        event Action<QuestCompletedEvent> OnQuestCompleted;

        /// <summary>
        /// 任务失败时触发。
        /// </summary>
        event Action<QuestFailedEvent> OnQuestFailed;

        /// <summary>
        /// 任务追踪状态变化时触发。
        /// </summary>
        event Action<QuestTrackingChangedEvent> OnTrackingChanged;

        /// <summary>
        /// 任务数据版本号。
        /// 每当任务运行时状态或任务配置引用发生变化时递增，供数据驱动桥接层按顺序拉取快照。
        /// </summary>
        int Revision { get; }

        /// <summary>
        /// 设置任务静态配置数据库。
        /// 通常由 NiumaQuestController 在初始化时传入。
        /// 如果已有运行时状态，该方法会刷新运行时缓存，并通过事件通知外部。
        /// </summary>
        void SetQuestAssets(IEnumerable<QuestAsset> questAssets);

        /// <summary>
        /// 尝试接取任务。
        /// </summary>
        bool TryAcceptQuest(string questId);

        /// <summary>
        /// 尝试直接完成任务。
        /// 主要用于剧情脚本、调试或 Manual 任务。
        /// </summary>
        bool TryCompleteQuest(string questId);

        /// <summary>
        /// 尝试推进到下一个阶段。
        /// Manual 阶段必须通过该方法或 TrySetStage 显式推进。
        /// </summary>
        bool TryAdvanceStage(string questId);

        /// <summary>
        /// 尝试切换到指定阶段。
        /// </summary>
        bool TrySetStage(string questId, string stageId);

        /// <summary>
        /// 尝试将任务标记为失败。
        /// </summary>
        bool TryFailQuest(string questId);

        /// <summary>
        /// 尝试设置任务为当前追踪任务。
        /// </summary>
        bool TryTrackQuest(string questId);

        /// <summary>
        /// 尝试取消任务追踪。
        /// </summary>
        bool TryUntrackQuest(string questId);

        /// <summary>
        /// 查询任务是否已经接取。
        /// </summary>
        bool IsQuestAccepted(string questId);

        /// <summary>
        /// 查询任务是否已经完成或领取奖励。
        /// </summary>
        bool IsQuestCompleted(string questId);

        /// <summary>
        /// 尝试获取任务快照。
        /// 返回的是脱离内部状态的快照，外部无法通过它修改任务服务内部数据。
        /// </summary>
        bool TryGetQuestSnapshot(string questId, out QuestProgressSnapshot snapshot);

        /// <summary>
        /// 推入任务信号。
        /// 返回 true 表示至少有一个任务目标被推进。
        /// </summary>
        bool PushSignal(QuestSignal signal);

        /// <summary>
        /// 显式导出任务存档快照。
        /// 不允许直接序列化 QuestRuntimeState。
        /// </summary>
        QuestProgressSnapshot[] ExportSnapshots();

        /// <summary>
        /// 从存档快照恢复任务运行时状态。
        /// </summary>
        void ImportSnapshots(IEnumerable<QuestProgressSnapshot> snapshots);
    }
}
