using System;
using System.Collections.Generic;
using NiumaQuest.Data;
using NiumaQuest.Enum;
using NiumaQuest.Event;
using NiumaQuest.RuntimeData;
using NiumaQuest.Signal;

namespace NiumaQuest.Service
{
    /// <summary>
    /// 默认任务服务实现。
    /// 只处理任务事实、目标进度和阶段推进，不播放剧情、不创建 UI、不直接控制其它模块。
    /// </summary>
    public sealed class QuestService : IQuestService
    {
        private readonly Dictionary<string, QuestAsset> _questAssets = new Dictionary<string, QuestAsset>();
        private readonly Dictionary<string, QuestRuntimeState> _runtimeStates = new Dictionary<string, QuestRuntimeState>();
        private int _mutationDepth;
        private bool _revisionDirtyInCurrentMutation;

        /// <inheritdoc />
        public event Action<QuestChangedEvent> OnQuestChanged;

        /// <inheritdoc />
        public event Action<QuestAcceptedEvent> OnQuestAccepted;

        /// <inheritdoc />
        public event Action<QuestObjectiveProgressedEvent> OnObjectiveProgressed;

        /// <inheritdoc />
        public event Action<QuestStageChangedEvent> OnStageChanged;

        /// <inheritdoc />
        public event Action<QuestStageAdvancedEvent> OnStageAdvanced;

        /// <inheritdoc />
        public event Action<QuestCompletedEvent> OnQuestCompleted;

        /// <inheritdoc />
        public event Action<QuestFailedEvent> OnQuestFailed;

        /// <inheritdoc />
        public event Action<QuestTrackingChangedEvent> OnTrackingChanged;

        /// <inheritdoc />
        public int Revision { get; private set; }

        /// <inheritdoc />
        public void SetQuestAssets(IEnumerable<QuestAsset> questAssets)
        {
            BeginMutation();
            try
            {
                _questAssets.Clear();

                if (questAssets == null)
                {
                    MarkRevisionDirty();
                    return;
                }

                foreach (var asset in questAssets)
                {
                    if (asset == null || string.IsNullOrWhiteSpace(asset.QuestId))
                    {
                        continue;
                    }

                    if (_questAssets.ContainsKey(asset.QuestId))
                    {
                        continue;
                    }

                    _questAssets.Add(asset.QuestId, asset);
                }

                RefreshAllRuntimeStates();
                MarkRevisionDirty();
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool TryAcceptQuest(string questId)
        {
            BeginMutation();
            try
            {
                if (!TryGetAsset(questId, out var asset))
                {
                    return false;
                }

                if (_runtimeStates.TryGetValue(questId, out var existingState))
                {
                    if (!asset.Repeatable && existingState.State != QuestState.Locked && existingState.State != QuestState.Available)
                    {
                        return false;
                    }
                }

                var firstStage = GetFirstValidStage(asset);
                if (firstStage == null)
                {
                    return false;
                }

                var state = new QuestRuntimeState
                {
                    QuestId = asset.QuestId,
                    State = QuestState.Accepted,
                    CurrentStageId = firstStage.StageId,
                    IsTracked = asset.AutoTrackOnAccept,
                    Objectives = BuildObjectiveStates(firstStage, null)
                };

                _runtimeStates[asset.QuestId] = state;
                PublishAccepted(state);
                EvaluateCurrentStage(state);
                return true;
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool TryCompleteQuest(string questId)
        {
            BeginMutation();
            try
            {
                if (!TryGetCompletableState(questId, out var state))
                {
                    return false;
                }

                if (state.State == QuestState.Completed
                    || state.State == QuestState.RewardPending
                    || state.State == QuestState.Failed
                    || state.State == QuestState.Rewarded)
                {
                    return false;
                }

                state.State = QuestState.Completed;
                PublishCompleted(state);
                return true;
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool TrySetRewardPending(string questId)
        {
            BeginMutation();
            try
            {
                if (!TryGetInternalQuestState(questId, out var state))
                {
                    return false;
                }

                if (state.State == QuestState.RewardPending)
                {
                    return true;
                }

                if (state.State != QuestState.Completed)
                {
                    return false;
                }

                state.State = QuestState.RewardPending;
                PublishChanged(state.QuestId, QuestChangeType.RewardPending);
                return true;
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool TryMarkRewarded(string questId)
        {
            BeginMutation();
            try
            {
                if (!TryGetInternalQuestState(questId, out var state))
                {
                    return false;
                }

                if (state.State == QuestState.Rewarded)
                {
                    return true;
                }

                if (state.State != QuestState.Completed && state.State != QuestState.RewardPending)
                {
                    return false;
                }

                state.State = QuestState.Rewarded;
                PublishChanged(state.QuestId, QuestChangeType.Rewarded);
                return true;
            }
            finally
            {
                EndMutation();
            }
        }

        public bool TryAdvanceStage(string questId)
        {
            BeginMutation();
            try
            {
                if (!TryGetStageEditableState(questId, out var state) || !TryGetAsset(questId, out var asset))
                {
                    return false;
                }

                return AdvanceOrComplete(state, asset);
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool TrySetStage(string questId, string stageId)
        {
            BeginMutation();
            try
            {
                if (!TryGetStageEditableState(questId, out var state) || !TryGetAsset(questId, out var asset))
                {
                    return false;
                }

                var stage = FindStage(asset, stageId);
                if (stage == null)
                {
                    return false;
                }

                var previousStageId = state.CurrentStageId;
                state.CurrentStageId = stage.StageId;
                state.State = QuestState.Accepted;
                state.Objectives = BuildObjectiveStates(stage, state.Objectives);
                PublishStageChanged(state.QuestId, previousStageId, state.CurrentStageId, isManual: true);
                EvaluateCurrentStage(state);
                return true;
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool TryFailQuest(string questId)
        {
            BeginMutation();
            try
            {
                if (!TryGetFailableState(questId, out var state))
                {
                    return false;
                }

                if (state.State == QuestState.Completed || state.State == QuestState.Rewarded)
                {
                    return false;
                }

                state.State = QuestState.Failed;
                PublishFailed(state);
                return true;
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool TryTrackQuest(string questId)
        {
            BeginMutation();
            try
            {
                if (!TryGetInternalQuestState(questId, out var state))
                {
                    return false;
                }

                foreach (var runtimeState in _runtimeStates.Values)
                {
                    if (runtimeState == null || ReferenceEquals(runtimeState, state) || !runtimeState.IsTracked)
                    {
                        continue;
                    }

                    runtimeState.IsTracked = false;
                    PublishTrackingChanged(runtimeState);
                }

                if (state.IsTracked)
                {
                    return true;
                }

                state.IsTracked = true;
                PublishTrackingChanged(state);
                return true;
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool TryUntrackQuest(string questId)
        {
            BeginMutation();
            try
            {
                if (!TryGetInternalQuestState(questId, out var state))
                {
                    return false;
                }

                if (!state.IsTracked)
                {
                    return true;
                }

                state.IsTracked = false;
                PublishTrackingChanged(state);
                return true;
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public bool IsQuestAccepted(string questId)
        {
            return TryGetInternalQuestState(questId, out var state) && state.State == QuestState.Accepted;
        }

        /// <inheritdoc />
        public bool IsQuestCompleted(string questId)
        {
            return TryGetInternalQuestState(questId, out var state)
                   && (state.State == QuestState.Completed
                       || state.State == QuestState.RewardPending
                       || state.State == QuestState.Rewarded);
        }

        /// <inheritdoc />
        public bool TryGetQuestSnapshot(string questId, out QuestProgressSnapshot snapshot)
        {
            snapshot = null;
            if (!TryGetInternalQuestState(questId, out var state))
            {
                return false;
            }

            snapshot = state.ToSnapshot();
            return snapshot != null;
        }

        /// <inheritdoc />
        public bool PushSignal(QuestSignal signal)
        {
            BeginMutation();
            try
            {
                if (string.IsNullOrWhiteSpace(signal.TargetId))
                {
                    return false;
                }

                var progressed = false;
                foreach (var state in _runtimeStates.Values)
                {
                    if (state == null || state.State != QuestState.Accepted || state.Objectives == null)
                    {
                        continue;
                    }

                    var stateProgressed = false;
                    for (var i = 0; i < state.Objectives.Length; i++)
                    {
                        var objective = state.Objectives[i];
                        if (objective == null || objective.IsCompleted || !objective.HasConfigCache)
                        {
                            continue;
                        }

                        if (objective.Type != signal.Type || !string.Equals(objective.TargetId, signal.TargetId, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var increment = signal.Count < 1 ? 1 : signal.Count;
                        if (objective.CurrentCount < 0)
                        {
                            objective.CurrentCount = 0;
                        }

                        if (objective.RequiredCount < 1)
                        {
                            objective.RequiredCount = 1;
                        }

                        var remaining = objective.RequiredCount - objective.CurrentCount;
                        if (remaining <= 0)
                        {
                            objective.CurrentCount = objective.RequiredCount;
                            objective.IsCompleted = true;
                            continue;
                        }

                        if (increment >= remaining)
                        {
                            objective.CurrentCount = objective.RequiredCount;
                        }
                        else
                        {
                            objective.CurrentCount += increment;
                        }

                        if (objective.CurrentCount >= objective.RequiredCount)
                        {
                            objective.CurrentCount = objective.RequiredCount;
                            objective.IsCompleted = true;
                        }

                        PublishObjectiveProgressed(state, objective);
                        stateProgressed = true;
                        progressed = true;
                    }

                    if (stateProgressed)
                    {
                        EvaluateCurrentStage(state);
                    }
                }

                return progressed;
            }
            finally
            {
                EndMutation();
            }
        }

        /// <inheritdoc />
        public QuestProgressSnapshot[] ExportSnapshots()
        {
            if (_runtimeStates.Count == 0)
            {
                return Array.Empty<QuestProgressSnapshot>();
            }

            var snapshots = new List<QuestProgressSnapshot>(_runtimeStates.Count);
            CopyQuestSnapshots(snapshots);
            return snapshots.ToArray();
        }

        /// <inheritdoc />
        public void CopyQuestSnapshots(List<QuestProgressSnapshot> output)
        {
            if (output == null)
            {
                return;
            }

            output.Clear();
            foreach (var state in _runtimeStates.Values)
            {
                if (state == null || state.State == QuestState.MigrationFailed)
                {
                    continue;
                }

                var snapshot = state.ToSnapshot();
                if (snapshot != null)
                {
                    output.Add(snapshot);
                }
            }
        }

        /// <inheritdoc />
        public void ImportSnapshots(IEnumerable<QuestProgressSnapshot> snapshots)
        {
            BeginMutation();
            try
            {
                _runtimeStates.Clear();
                MarkRevisionDirty();

                if (snapshots == null)
                {
                    return;
                }

                foreach (var snapshot in snapshots)
                {
                    if (snapshot == null || !TryGetAsset(snapshot.QuestId, out var asset))
                    {
                        continue;
                    }

                    var stage = FindStage(asset, snapshot.CurrentStageId);
                    if (stage == null)
                    {
                        var failedState = new QuestRuntimeState
                        {
                            QuestId = snapshot.QuestId,
                            State = QuestState.MigrationFailed,
                            CurrentStageId = snapshot.CurrentStageId,
                            IsTracked = snapshot.IsTracked,
                            Objectives = Array.Empty<QuestObjectiveRuntimeState>()
                        };

                        _runtimeStates[failedState.QuestId] = failedState;
                        PublishChanged(failedState.QuestId, QuestChangeType.MigrationFailed);
                        continue;
                    }

                    var state = new QuestRuntimeState
                    {
                        QuestId = snapshot.QuestId,
                        State = snapshot.State,
                        CurrentStageId = stage.StageId,
                        IsTracked = snapshot.IsTracked,
                        Objectives = BuildObjectiveStates(stage, null, snapshot.Objectives)
                    };

                    _runtimeStates[state.QuestId] = state;
                    if (state.State == QuestState.Accepted)
                    {
                        EvaluateCurrentStage(state);
                    }
                }
            }
            finally
            {
                EndMutation();
            }
        }

        private bool TryGetAsset(string questId, out QuestAsset asset)
        {
            asset = null;
            return !string.IsNullOrWhiteSpace(questId) && _questAssets.TryGetValue(questId, out asset);
        }

        private bool TryGetInternalQuestState(string questId, out QuestRuntimeState state)
        {
            state = null;
            return !string.IsNullOrWhiteSpace(questId) && _runtimeStates.TryGetValue(questId, out state);
        }

        private bool TryGetStageEditableState(string questId, out QuestRuntimeState state)
        {
            if (!TryGetInternalQuestState(questId, out state))
            {
                return false;
            }

            return state.State == QuestState.Accepted;
        }

        private bool TryGetCompletableState(string questId, out QuestRuntimeState state)
        {
            if (!TryGetInternalQuestState(questId, out state))
            {
                return false;
            }

            return state.State == QuestState.Accepted;
        }

        private bool TryGetFailableState(string questId, out QuestRuntimeState state)
        {
            if (!TryGetInternalQuestState(questId, out state))
            {
                return false;
            }

            return state.State == QuestState.Accepted;
        }

        private void RefreshAllRuntimeStates()
        {
            foreach (var state in _runtimeStates.Values)
            {
                if (state == null || !TryGetAsset(state.QuestId, out var asset))
                {
                    continue;
                }

                var stage = FindStage(asset, state.CurrentStageId);
                if (stage == null)
                {
                    state.State = QuestState.MigrationFailed;
                    state.Objectives = Array.Empty<QuestObjectiveRuntimeState>();
                    PublishChanged(state.QuestId, QuestChangeType.MigrationFailed);
                    continue;
                }

                var beforeSnapshot = state.ToSnapshot();
                state.CurrentStageId = stage.StageId;
                state.Objectives = BuildObjectiveStates(stage, state.Objectives);
                if (state.State == QuestState.Accepted)
                {
                    EvaluateCurrentStage(state);
                }

                var afterSnapshot = state.ToSnapshot();
                if (HasSnapshotChanged(beforeSnapshot, afterSnapshot))
                {
                    PublishChanged(state.QuestId, QuestChangeType.ConfigurationRefreshed);
                }
            }
        }

        private bool EvaluateCurrentStage(QuestRuntimeState state)
        {
            if (state == null || state.State != QuestState.Accepted || !TryGetAsset(state.QuestId, out var asset))
            {
                return false;
            }

            var stage = FindStage(asset, state.CurrentStageId);
            if (stage == null || stage.CompleteMode == QuestCompleteMode.Manual)
            {
                return false;
            }

            if (!IsStageCompleted(stage.CompleteMode, state.Objectives))
            {
                return false;
            }

            return AdvanceOrComplete(state, asset);
        }

        private bool AdvanceOrComplete(QuestRuntimeState state, QuestAsset asset)
        {
            var previousStageId = state.CurrentStageId;
            var nextStage = GetNextStage(asset, state.CurrentStageId);
            if (nextStage == null)
            {
                state.State = QuestState.Completed;
                PublishCompleted(state);
                return true;
            }

            state.CurrentStageId = nextStage.StageId;
            state.State = QuestState.Accepted;
            state.Objectives = BuildObjectiveStates(nextStage, state.Objectives);
            PublishStageChanged(state.QuestId, previousStageId, state.CurrentStageId, isManual: false);
            return true;
        }

        private static bool IsStageCompleted(QuestCompleteMode completeMode, QuestObjectiveRuntimeState[] objectives)
        {
            if (objectives == null || objectives.Length == 0)
            {
                return false;
            }

            switch (completeMode)
            {
                case QuestCompleteMode.Any:
                    for (var i = 0; i < objectives.Length; i++)
                    {
                        if (objectives[i] != null && objectives[i].IsCompleted)
                        {
                            return true;
                        }
                    }

                    return false;

                case QuestCompleteMode.All:
                    for (var i = 0; i < objectives.Length; i++)
                    {
                        if (objectives[i] == null || !objectives[i].IsCompleted)
                        {
                            return false;
                        }
                    }

                    return true;

                default:
                    return false;
            }
        }

        private static QuestStageData GetFirstValidStage(QuestAsset asset)
        {
            if (asset?.Stages == null)
            {
                return null;
            }

            for (var i = 0; i < asset.Stages.Length; i++)
            {
                var stage = asset.Stages[i];
                if (stage != null && !string.IsNullOrWhiteSpace(stage.StageId))
                {
                    return stage;
                }
            }

            return null;
        }

        private static QuestStageData FindStage(QuestAsset asset, string stageId)
        {
            if (asset?.Stages == null || string.IsNullOrWhiteSpace(stageId))
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

        private static QuestStageData GetNextStage(QuestAsset asset, string currentStageId)
        {
            if (asset?.Stages == null || string.IsNullOrWhiteSpace(currentStageId))
            {
                return null;
            }

            for (var i = 0; i < asset.Stages.Length; i++)
            {
                var stage = asset.Stages[i];
                if (stage == null || !string.Equals(stage.StageId, currentStageId, StringComparison.Ordinal))
                {
                    continue;
                }

                for (var j = i + 1; j < asset.Stages.Length; j++)
                {
                    var nextStage = asset.Stages[j];
                    if (nextStage != null && !string.IsNullOrWhiteSpace(nextStage.StageId))
                    {
                        return nextStage;
                    }
                }

                return null;
            }

            return null;
        }

        private static QuestObjectiveRuntimeState[] BuildObjectiveStates(
            QuestStageData stage,
            QuestObjectiveRuntimeState[] previousObjectives,
            QuestObjectiveProgressSnapshot[] snapshots = null)
        {
            if (stage?.Objectives == null || stage.Objectives.Length == 0)
            {
                return Array.Empty<QuestObjectiveRuntimeState>();
            }

            var states = new List<QuestObjectiveRuntimeState>(stage.Objectives.Length);
            for (var i = 0; i < stage.Objectives.Length; i++)
            {
                var config = stage.Objectives[i];
                if (config == null || string.IsNullOrWhiteSpace(config.ObjectiveId))
                {
                    continue;
                }

                var state = FindPreviousObjective(previousObjectives, config.ObjectiveId)
                            ?? CreateObjectiveFromSnapshot(snapshots, config.ObjectiveId)
                            ?? new QuestObjectiveRuntimeState { ObjectiveId = config.ObjectiveId };

                if (!state.RefreshFromConfig(config, out _))
                {
                    state = new QuestObjectiveRuntimeState { ObjectiveId = config.ObjectiveId };
                    state.RefreshFromConfig(config, out _);
                }

                states.Add(state);
            }

            return states.ToArray();
        }

        private static QuestObjectiveRuntimeState FindPreviousObjective(
            QuestObjectiveRuntimeState[] previousObjectives,
            string objectiveId)
        {
            if (previousObjectives == null || string.IsNullOrWhiteSpace(objectiveId))
            {
                return null;
            }

            for (var i = 0; i < previousObjectives.Length; i++)
            {
                var objective = previousObjectives[i];
                if (objective != null && string.Equals(objective.ObjectiveId, objectiveId, StringComparison.Ordinal))
                {
                    return objective;
                }
            }

            return null;
        }

        private static QuestObjectiveRuntimeState CreateObjectiveFromSnapshot(
            QuestObjectiveProgressSnapshot[] snapshots,
            string objectiveId)
        {
            if (snapshots == null || string.IsNullOrWhiteSpace(objectiveId))
            {
                return null;
            }

            for (var i = 0; i < snapshots.Length; i++)
            {
                var snapshot = snapshots[i];
                if (snapshot == null || !string.Equals(snapshot.ObjectiveId, objectiveId, StringComparison.Ordinal))
                {
                    continue;
                }

                return new QuestObjectiveRuntimeState
                {
                    ObjectiveId = snapshot.ObjectiveId,
                    CurrentCount = snapshot.CurrentCount < 0 ? 0 : snapshot.CurrentCount,
                    IsCompleted = snapshot.IsCompleted
                };
            }

            return null;
        }

        private static bool HasSnapshotChanged(QuestProgressSnapshot before, QuestProgressSnapshot after)
        {
            if (before == null && after == null)
            {
                return false;
            }

            if (before == null || after == null)
            {
                return true;
            }

            if (!string.Equals(before.CurrentStageId, after.CurrentStageId, StringComparison.Ordinal)
                || before.State != after.State
                || before.IsTracked != after.IsTracked)
            {
                return true;
            }

            var beforeObjectives = before.Objectives ?? Array.Empty<QuestObjectiveProgressSnapshot>();
            var afterObjectives = after.Objectives ?? Array.Empty<QuestObjectiveProgressSnapshot>();
            if (beforeObjectives.Length != afterObjectives.Length)
            {
                return true;
            }

            for (var i = 0; i < beforeObjectives.Length; i++)
            {
                var left = beforeObjectives[i];
                var right = afterObjectives[i];
                if (left == null && right == null)
                {
                    continue;
                }

                if (left == null || right == null)
                {
                    return true;
                }

                if (!string.Equals(left.ObjectiveId, right.ObjectiveId, StringComparison.Ordinal)
                    || left.CurrentCount != right.CurrentCount
                    || left.IsCompleted != right.IsCompleted)
                {
                    return true;
                }
            }

            return false;
        }

        private void PublishAccepted(QuestRuntimeState state)
        {
            OnQuestAccepted?.Invoke(new QuestAcceptedEvent(state.QuestId, state.CurrentStageId, state.IsTracked));
            PublishChanged(state.QuestId, QuestChangeType.Accepted);
        }

        private void PublishObjectiveProgressed(QuestRuntimeState state, QuestObjectiveRuntimeState objective)
        {
            OnObjectiveProgressed?.Invoke(new QuestObjectiveProgressedEvent(
                state.QuestId,
                state.CurrentStageId,
                objective.ObjectiveId,
                objective.CurrentCount,
                objective.RequiredCount,
                objective.IsCompleted));

            PublishChanged(state.QuestId, QuestChangeType.ObjectiveProgressed);
        }

        private void PublishStageChanged(string questId, string previousStageId, string currentStageId, bool isManual)
        {
            var stageEvent = new QuestStageChangedEvent(questId, previousStageId, currentStageId, isManual);
            OnStageChanged?.Invoke(stageEvent);
            if (!isManual)
            {
                OnStageAdvanced?.Invoke(new QuestStageAdvancedEvent(questId, previousStageId, currentStageId));
            }

            PublishChanged(questId, QuestChangeType.StageChanged);
        }

        private void PublishCompleted(QuestRuntimeState state)
        {
            OnQuestCompleted?.Invoke(new QuestCompletedEvent(state.QuestId, state.CurrentStageId));
            PublishChanged(state.QuestId, QuestChangeType.Completed);
        }

        private void PublishFailed(QuestRuntimeState state)
        {
            OnQuestFailed?.Invoke(new QuestFailedEvent(state.QuestId, state.CurrentStageId));
            PublishChanged(state.QuestId, QuestChangeType.Failed);
        }

        private void PublishTrackingChanged(QuestRuntimeState state)
        {
            OnTrackingChanged?.Invoke(new QuestTrackingChangedEvent(state.QuestId, state.IsTracked));
            PublishChanged(state.QuestId, QuestChangeType.TrackingChanged);
        }

        private void PublishChanged(string questId, QuestChangeType changeType)
        {
            MarkRevisionDirty();
            OnQuestChanged?.Invoke(new QuestChangedEvent(questId, changeType));
        }

        private void BeginMutation()
        {
            _mutationDepth++;
        }

        private void EndMutation()
        {
            if (_mutationDepth <= 0)
            {
                _mutationDepth = 0;
                _revisionDirtyInCurrentMutation = false;
                return;
            }

            _mutationDepth--;
            if (_mutationDepth == 0)
            {
                _revisionDirtyInCurrentMutation = false;
            }
        }

        private void MarkRevisionDirty()
        {
            if (_mutationDepth > 0)
            {
                if (_revisionDirtyInCurrentMutation)
                {
                    return;
                }

                BumpRevision();
                _revisionDirtyInCurrentMutation = true;
                return;
            }

            BumpRevision();
        }

        private void BumpRevision()
        {
            if (Revision == int.MaxValue)
            {
                Revision = 1;
                return;
            }

            Revision++;
        }
    }
}
