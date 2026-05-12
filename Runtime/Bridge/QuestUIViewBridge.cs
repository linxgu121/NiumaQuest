using System;
using System.Collections.Generic;
using NiumaQuest.Controller;
using NiumaQuest.Data;
using NiumaQuest.RuntimeData;
using UnityEngine;

namespace NiumaQuest.Bridge
{
    /// <summary>
    /// 任务模块到 UI 模块的数据驱动桥接层。
    /// 该桥接层不订阅任务事件，而是在固定时机读取任务版本号和快照，生成 UI 表现数据。
    /// </summary>
    public sealed class QuestUIViewBridge : MonoBehaviour
    {
        [Header("模块引用")]
        [Tooltip("任务模块根控制器。请拖入场景中的 NiumaQuestController。为空时可按配置自动查找。")]
        [SerializeField] private NiumaQuestController questController;

        [Tooltip("实现 IQuestUIReceiver 的 UI 组件。任务桥接层会把整理后的 UI 表现数据转交给它显示。")]
        [SerializeField] private MonoBehaviour questUIReceiverProvider;

        [Header("自动查找")]
        [Tooltip("没有手动绑定任务控制器时，是否在场景中自动查找 NiumaQuestController。正式场景建议手动绑定。")]
        [SerializeField] private bool autoFindQuestController = true;

        [Header("刷新策略")]
        [Tooltip("启用桥接层时是否立即刷新一次当前追踪任务。")]
        [SerializeField] private bool refreshOnEnable = true;

        [Tooltip("是否在 LateUpdate 中按任务版本号自动刷新 UI。关闭后需要外部手动调用 RefreshTrackedQuest。")]
        [SerializeField] private bool refreshInLateUpdate = true;

        [Tooltip("没有追踪任务时，是否发送 Cleared 更新给 UI 接收口。")]
        [SerializeField] private bool notifyWhenNoTrackedQuest = true;

        [Header("日志")]
        [Tooltip("桥接层缺少必要引用时是否打印警告。")]
        [SerializeField] private bool logWarnings = true;

        private readonly List<QuestObjectiveViewData> _objectiveBuffer = new List<QuestObjectiveViewData>();
        private IQuestUIReceiver _receiver;
        private int _observedRevision = -1;
        private QuestTrackerViewData _lastTrackerData;
        private bool _hadTrackedQuest;
        private bool _isApplyingUpdate;

        private void Reset()
        {
            ResolveReferences(false);
        }

        private void OnEnable()
        {
            ResolveReferences(true);
            _observedRevision = -1;

            if (refreshOnEnable)
            {
                RefreshTrackedQuest();
            }
        }

        private void LateUpdate()
        {
            if (!refreshInLateUpdate || !EnsureController())
            {
                return;
            }

            if (_observedRevision == questController.QuestRevision)
            {
                return;
            }

            RefreshTrackedQuest();
        }

        /// <summary>
        /// 手动刷新当前追踪任务。
        /// 只读取当前任务数据，不依赖事件订阅。
        /// </summary>
        public void RefreshTrackedQuest()
        {
            if (!EnsureController())
            {
                return;
            }

            _observedRevision = questController.QuestRevision;
            var trackedSnapshot = FindTrackedSnapshot();
            if (trackedSnapshot == null)
            {
                ApplyClearUpdate();
                return;
            }

            _hadTrackedQuest = true;
            var trackerData = BuildTrackerViewData(trackedSnapshot);
            ApplyRawUpdate(new QuestUIUpdate(
                QuestUIUpdateType.Refresh,
                _observedRevision,
                trackerData,
                _lastTrackerData));
            _lastTrackerData = trackerData;
        }

        private QuestProgressSnapshot FindTrackedSnapshot()
        {
            var snapshots = questController.ExportSnapshots();
            for (var i = 0; i < snapshots.Length; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot != null && snapshot.IsTracked)
                {
                    return snapshot;
                }
            }

            return null;
        }

        private void ApplyClearUpdate()
        {
            if (!notifyWhenNoTrackedQuest && !_hadTrackedQuest)
            {
                return;
            }

            _receiver = ResolveReceiver(true);
            ApplyRawUpdate(new QuestUIUpdate(
                QuestUIUpdateType.Cleared,
                _observedRevision,
                null,
                _lastTrackerData));

            _hadTrackedQuest = false;
            _lastTrackerData = null;
        }

        private void ApplyRawUpdate(QuestUIUpdate update)
        {
            _receiver = ResolveReceiver(true);
            if (_receiver == null)
            {
                return;
            }

            if (_isApplyingUpdate)
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[NiumaQuestUIBridge] 检测到 UI 刷新重入，已跳过本次 ApplyQuestUpdate。请不要在 IQuestUIReceiver.ApplyQuestUpdate 中推进任务状态。", this);
                }

                return;
            }

            var revisionBeforeApply = questController != null ? questController.QuestRevision : _observedRevision;
            _isApplyingUpdate = true;
            try
            {
                _receiver.ApplyQuestUpdate(update);
            }
            finally
            {
                _isApplyingUpdate = false;
            }

            if (questController != null && questController.QuestRevision != revisionBeforeApply)
            {
                _observedRevision = questController.QuestRevision;
                if (logWarnings)
                {
                    Debug.LogWarning("[NiumaQuestUIBridge] IQuestUIReceiver.ApplyQuestUpdate 内修改了任务数据，桥接层已吞掉这次回流刷新以避免循环。请把任务推进放到输入/交互/剧情管线中处理。", this);
                }
            }
        }

        private QuestTrackerViewData BuildTrackerViewData(QuestProgressSnapshot snapshot)
        {
            questController.TryGetQuestAsset(snapshot.QuestId, out var asset);
            var stage = FindStage(asset, snapshot.CurrentStageId);

            _objectiveBuffer.Clear();
            var snapshotObjectives = snapshot.Objectives;
            if (snapshotObjectives != null)
            {
                for (var i = 0; i < snapshotObjectives.Length; i++)
                {
                    var objectiveSnapshot = snapshotObjectives[i];
                    if (objectiveSnapshot == null)
                    {
                        continue;
                    }

                    var objectiveConfig = FindObjective(stage, objectiveSnapshot.ObjectiveId);
                    if (objectiveConfig != null && objectiveConfig.HiddenInTracker)
                    {
                        continue;
                    }

                    _objectiveBuffer.Add(BuildObjectiveViewData(objectiveSnapshot, objectiveConfig));
                }
            }

            return new QuestTrackerViewData
            {
                QuestId = snapshot.QuestId,
                Title = asset != null ? asset.Title : snapshot.QuestId,
                Description = asset != null ? asset.Description : string.Empty,
                State = snapshot.State,
                CurrentStageId = snapshot.CurrentStageId,
                StageDescription = stage != null ? stage.Description : string.Empty,
                IsTracked = snapshot.IsTracked,
                MissingQuestConfig = asset == null,
                MissingStageConfig = stage == null,
                Objectives = _objectiveBuffer.ToArray()
            };
        }

        private static QuestObjectiveViewData BuildObjectiveViewData(
            QuestObjectiveProgressSnapshot snapshot,
            QuestObjectiveData config)
        {
            var requiredCount = config != null ? config.RequiredCount : 1;
            if (requiredCount < 1)
            {
                requiredCount = 1;
            }

            return new QuestObjectiveViewData
            {
                ObjectiveId = snapshot.ObjectiveId,
                Type = config != null ? config.Type : default,
                TargetId = config != null ? config.TargetId : null,
                Description = config != null ? config.Description : snapshot.ObjectiveId,
                CurrentCount = snapshot.CurrentCount,
                RequiredCount = requiredCount,
                IsCompleted = snapshot.IsCompleted,
                MissingConfig = config == null
            };
        }

        private static QuestStageData FindStage(QuestAsset asset, string stageId)
        {
            if (asset == null || asset.Stages == null || string.IsNullOrWhiteSpace(stageId))
            {
                return null;
            }

            for (var i = 0; i < asset.Stages.Length; i++)
            {
                var stage = asset.Stages[i];
                if (stage != null && string.Equals(stage.StageId, stageId, StringComparison.Ordinal))
                {
                    return stage;
                }
            }

            return null;
        }

        private static QuestObjectiveData FindObjective(QuestStageData stage, string objectiveId)
        {
            if (stage == null || stage.Objectives == null || string.IsNullOrWhiteSpace(objectiveId))
            {
                return null;
            }

            for (var i = 0; i < stage.Objectives.Length; i++)
            {
                var objective = stage.Objectives[i];
                if (objective != null && string.Equals(objective.ObjectiveId, objectiveId, StringComparison.Ordinal))
                {
                    return objective;
                }
            }

            return null;
        }

        private bool EnsureController()
        {
            ResolveQuestController(true);
            return questController != null;
        }

        private void ResolveReferences(bool logMissing)
        {
            ResolveQuestController(logMissing);
            _receiver = ResolveReceiver(logMissing);
        }

        private void ResolveQuestController(bool logMissing)
        {
            if (questController != null)
            {
                return;
            }

            if (autoFindQuestController)
            {
#if UNITY_2023_1_OR_NEWER
                questController = FindFirstObjectByType<NiumaQuestController>();
#else
                questController = FindObjectOfType<NiumaQuestController>();
#endif
            }

            if (questController == null && logWarnings && logMissing)
            {
                Debug.LogWarning("[NiumaQuestUIBridge] 未找到 NiumaQuestController，请在 Inspector 中绑定任务控制器。", this);
            }
        }

        private IQuestUIReceiver ResolveReceiver(bool logMissing)
        {
            var receiver = questUIReceiverProvider as IQuestUIReceiver;
            if (receiver == null && logWarnings && logMissing && questUIReceiverProvider != null)
            {
                Debug.LogWarning("[NiumaQuestUIBridge] Quest UI Receiver Provider 没有实现 IQuestUIReceiver。", this);
            }

            return receiver;
        }
    }
}
