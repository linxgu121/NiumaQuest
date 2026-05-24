using System;
using System.Collections.Generic;
using NiumaCore.Event;
using NiumaCore.Module;
using NiumaQuest.Data;
using NiumaQuest.Enum;
using NiumaQuest.Event;
using NiumaQuest.RuntimeData;
using NiumaQuest.Service;
using NiumaQuest.Signal;
using UnityEngine;

namespace NiumaQuest.Controller
{
    /// <summary>
    /// NiumaQuest 任务模块根控制器。
    /// 负责把纯 C# 的 QuestService 接入 Unity 生命周期和模块生命周期。
    /// </summary>
    public sealed class NiumaQuestController : MonoBehaviour, IGameModule
    {
        [Header("任务配置")]
        [Tooltip("任务静态配置列表。请拖入所有需要在当前场景或当前章节可用的 QuestAsset。")]
        [SerializeField] private QuestAsset[] questAssets;

        [Header("模块启动")]
        [Tooltip("Awake 时是否自动初始化任务服务。没有统一模块启动器时建议开启。")]
        [SerializeField] private bool initializeOnAwake = true;

        [Tooltip("OnEnable 时是否自动启动模块。没有统一模块启动器时建议开启。")]
        [SerializeField] private bool startOnEnable = true;

        [Tooltip("任务服务事件是否转发到 GameContext.EventBus。需要其它模块通过 Core 事件总线监听任务事件时开启。")]
        [SerializeField] private bool publishToEventBus = true;

        [Header("调试")]
        [Tooltip("调试用任务 ID。右键组件菜单可以用它接取、完成或推进任务。")]
        [SerializeField] private string debugQuestId;

        [Header("调试信号")]
        [Tooltip("调试推送的任务目标类型。需要和 QuestObjectiveData.Type 保持一致。")]
        [SerializeField] private QuestObjectiveType debugSignalType = QuestObjectiveType.Talk;

        [Tooltip("调试推送的目标 ID。需要和 QuestObjectiveData.TargetId 保持一致，例如对话 ID、交互物 ID、物品 ID。")]
        [SerializeField] private string debugSignalTargetId;

        [Tooltip("调试推送时增加的目标进度数量。小于 1 时会由 QuestSignal 自动修正为 1。")]
        [SerializeField] private int debugSignalCount = 1;

        [Tooltip("调试信号来源模块名。只用于日志和排查问题，可以填写 NiumaQuestController。")]
        [SerializeField] private string debugSignalSourceModule = "NiumaQuestController";

        /// <summary>
        /// 模块名称。
        /// </summary>
        public string ModuleName => "NiumaQuest";

        /// <summary>
        /// 任务服务接口。
        /// 外部桥接层应依赖 IQuestService，而不是直接依赖 QuestService 实现。
        /// </summary>
        public IQuestService QuestService => _questService;

        /// <summary>
        /// 当前模块是否已经初始化。
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// 当前模块是否正在运行。
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// 任务数据版本号。
        /// UI、存档或调试桥接层可以通过该值判断是否需要重新拉取快照。
        /// </summary>
        public int QuestRevision => _questService.Revision;

        /// <summary>
        /// 任务数据发生变化时触发。
        /// 桥接层通过控制器订阅事件，避免直接依赖 QuestService 实现。
        /// </summary>
        public event Action<QuestChangedEvent> OnQuestChanged
        {
            add => _questService.OnQuestChanged += value;
            remove => _questService.OnQuestChanged -= value;
        }

        /// <summary>
        /// 任务接取时触发。
        /// </summary>
        public event Action<QuestAcceptedEvent> OnQuestAccepted
        {
            add => _questService.OnQuestAccepted += value;
            remove => _questService.OnQuestAccepted -= value;
        }

        /// <summary>
        /// 任务目标进度变化时触发。
        /// </summary>
        public event Action<QuestObjectiveProgressedEvent> OnObjectiveProgressed
        {
            add => _questService.OnObjectiveProgressed += value;
            remove => _questService.OnObjectiveProgressed -= value;
        }

        /// <summary>
        /// 任务阶段变化时触发。
        /// </summary>
        public event Action<QuestStageChangedEvent> OnStageChanged
        {
            add => _questService.OnStageChanged += value;
            remove => _questService.OnStageChanged -= value;
        }

        /// <summary>
        /// 任务按顺序推进阶段时触发。
        /// </summary>
        public event Action<QuestStageAdvancedEvent> OnStageAdvanced
        {
            add => _questService.OnStageAdvanced += value;
            remove => _questService.OnStageAdvanced -= value;
        }

        /// <summary>
        /// 任务完成时触发。
        /// </summary>
        public event Action<QuestCompletedEvent> OnQuestCompleted
        {
            add => _questService.OnQuestCompleted += value;
            remove => _questService.OnQuestCompleted -= value;
        }

        /// <summary>
        /// 任务失败时触发。
        /// </summary>
        public event Action<QuestFailedEvent> OnQuestFailed
        {
            add => _questService.OnQuestFailed += value;
            remove => _questService.OnQuestFailed -= value;
        }

        /// <summary>
        /// 任务追踪状态变化时触发。
        /// </summary>
        public event Action<QuestTrackingChangedEvent> OnTrackingChanged
        {
            add => _questService.OnTrackingChanged += value;
            remove => _questService.OnTrackingChanged -= value;
        }

        private readonly QuestService _questService = new QuestService();
        private GameContext _context;
        private IEventBus _eventBus;
        private bool _subscribed;
        private bool _warnedMissingEventBus;

        private void Awake()
        {
            if (initializeOnAwake && !IsInitialized)
            {
                Initialize(null);
            }
        }

        private void OnEnable()
        {
            if (startOnEnable && IsInitialized && !IsRunning)
            {
                StartModule();
            }
        }

        private void OnDisable()
        {
            if (IsRunning)
            {
                StopModule();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeServiceEvents();
        }

        /// <summary>
        /// 初始化任务模块。
        /// </summary>
        public void Initialize(GameContext context)
        {
            _context = context;
            _eventBus = context?.EventBus;

            if (publishToEventBus && _eventBus == null && !_warnedMissingEventBus)
            {
                Debug.LogWarning("[NiumaQuest] 已开启事件总线转发，但 Initialize 传入的 GameContext 或 EventBus 为空。任务事件仍会在 QuestService 内部派发，但不会转发到 Core EventBus。", this);
                _warnedMissingEventBus = true;
            }

            if (publishToEventBus)
            {
                SubscribeServiceEvents();
            }
            else
            {
                UnsubscribeServiceEvents();
            }

            _questService.SetQuestAssets(questAssets);

            IsInitialized = true;
        }

        /// <summary>
        /// 启动任务模块。
        /// </summary>
        public void StartModule()
        {
            if (!IsInitialized)
            {
                Initialize(_context);
            }

            IsRunning = true;
        }

        /// <summary>
        /// 停止任务模块。
        /// 这里只关闭模块运行状态，不导出存档；任务存档由统一存档模块或上层流程在合适时机处理。
        /// </summary>
        public void StopModule()
        {
            IsRunning = false;
        }

        /// <summary>
        /// 任务模块帧更新。
        /// 当前任务服务是事件驱动，MVP 阶段不需要每帧逻辑。
        /// </summary>
        public void Tick(float deltaTime)
        {
        }

        /// <summary>
        /// 运行时替换任务配置。
        /// 会刷新 QuestService 中已有的运行时状态，并按服务层规则派发变化事件。
        /// </summary>
        public void SetQuestAssets(QuestAsset[] assets)
        {
            questAssets = assets;
            _questService.SetQuestAssets(questAssets);
        }

        /// <summary>
        /// 尝试接取任务。
        /// </summary>
        public bool TryAcceptQuest(string questId)
        {
            return IsInitialized && _questService.TryAcceptQuest(questId);
        }

        /// <summary>
        /// 尝试完成任务。
        /// </summary>
        public bool TryCompleteQuest(string questId)
        {
            return IsInitialized && _questService.TryCompleteQuest(questId);
        }

        /// <summary>
        /// 尝试推进任务阶段。
        /// </summary>
        public bool TryAdvanceStage(string questId)
        {
            return IsInitialized && _questService.TryAdvanceStage(questId);
        }

        /// <summary>
        /// 尝试切换到指定任务阶段。
        /// </summary>
        public bool TrySetStage(string questId, string stageId)
        {
            return IsInitialized && _questService.TrySetStage(questId, stageId);
        }

        /// <summary>
        /// 尝试将任务标记为失败。
        /// </summary>
        public bool TryFailQuest(string questId)
        {
            return IsInitialized && _questService.TryFailQuest(questId);
        }

        /// <summary>
        /// 尝试追踪任务。
        /// </summary>
        public bool TryTrackQuest(string questId)
        {
            return IsInitialized && _questService.TryTrackQuest(questId);
        }

        /// <summary>
        /// 尝试取消任务追踪。
        /// </summary>
        public bool TryUntrackQuest(string questId)
        {
            return IsInitialized && _questService.TryUntrackQuest(questId);
        }

        /// <summary>
        /// 查询任务是否已经接取。
        /// </summary>
        public bool IsQuestAccepted(string questId)
        {
            return IsInitialized && _questService.IsQuestAccepted(questId);
        }

        /// <summary>
        /// 查询任务是否已经完成。
        /// </summary>
        public bool IsQuestCompleted(string questId)
        {
            return IsInitialized && _questService.IsQuestCompleted(questId);
        }

        /// <summary>
        /// 尝试获取任务静态配置。
        /// 用于桥接层把运行时快照翻译成 UI 表现数据，不允许外部修改返回的配置对象。
        /// </summary>
        public bool TryGetQuestAsset(string questId, out QuestAsset asset)
        {
            if (string.IsNullOrWhiteSpace(questId) || questAssets == null)
            {
                asset = null;
                return false;
            }

            for (var i = 0; i < questAssets.Length; i++)
            {
                var candidate = questAssets[i];
                if (candidate != null && candidate.QuestId == questId)
                {
                    asset = candidate;
                    return true;
                }
            }

            asset = null;
            return false;
        }

        /// <summary>
        /// 尝试获取任务存档快照。
        /// 返回的是脱离内部状态的快照，外部不能通过它修改任务服务内部数据。
        /// </summary>
        public bool TryGetQuestSnapshot(string questId, out QuestProgressSnapshot snapshot)
        {
            if (!IsInitialized)
            {
                snapshot = null;
                return false;
            }

            return _questService.TryGetQuestSnapshot(questId, out snapshot);
        }

        /// <summary>
        /// 向任务服务推送任务信号。
        /// </summary>
        public bool PushSignal(QuestSignal signal)
        {
            return IsInitialized && _questService.PushSignal(signal);
        }

        /// <summary>
        /// 导出任务存档快照。
        /// </summary>
        public QuestProgressSnapshot[] ExportSnapshots()
        {
            return _questService.ExportSnapshots();
        }

        /// <summary>
        /// 复制任务快照到调用方缓存列表。
        /// UI 桥接层优先使用该轻量查询，不要为了刷新界面调用 ExportSnapshots。
        /// </summary>
        public void CopyQuestSnapshots(List<QuestProgressSnapshot> output)
        {
            if (!IsInitialized || _questService == null)
            {
                output?.Clear();
                return;
            }

            _questService.CopyQuestSnapshots(output);
        }

        /// <summary>
        /// 导入任务存档快照。
        /// </summary>
        public void ImportSnapshots(QuestProgressSnapshot[] snapshots)
        {
            _questService.ImportSnapshots(snapshots);
        }

        [ContextMenu("调试/接取任务")]
        private void DebugAcceptQuest()
        {
            if (string.IsNullOrWhiteSpace(debugQuestId))
            {
                Debug.LogWarning("[NiumaQuest] 调试任务 ID 为空，无法接取任务。", this);
                return;
            }

            Debug.Log($"[NiumaQuest] 接取任务 {debugQuestId}：{TryAcceptQuest(debugQuestId)}", this);
        }

        [ContextMenu("调试/推进阶段")]
        private void DebugAdvanceStage()
        {
            if (string.IsNullOrWhiteSpace(debugQuestId))
            {
                Debug.LogWarning("[NiumaQuest] 调试任务 ID 为空，无法推进阶段。", this);
                return;
            }

            Debug.Log($"[NiumaQuest] 推进任务阶段 {debugQuestId}：{TryAdvanceStage(debugQuestId)}", this);
        }

        [ContextMenu("调试/完成任务")]
        private void DebugCompleteQuest()
        {
            if (string.IsNullOrWhiteSpace(debugQuestId))
            {
                Debug.LogWarning("[NiumaQuest] 调试任务 ID 为空，无法完成任务。", this);
                return;
            }

            Debug.Log($"[NiumaQuest] 完成任务 {debugQuestId}：{TryCompleteQuest(debugQuestId)}", this);
        }

        [ContextMenu("调试/推送任务信号")]
        private void DebugPushSignal()
        {
            if (string.IsNullOrWhiteSpace(debugSignalTargetId))
            {
                Debug.LogWarning("[NiumaQuest] 调试信号目标 ID 为空，无法推送任务信号。", this);
                return;
            }

            var signal = new QuestSignal(
                debugSignalType,
                debugSignalTargetId,
                debugSignalCount,
                debugSignalSourceModule);

            Debug.Log($"[NiumaQuest] 推送任务信号 Type={debugSignalType}, TargetId={debugSignalTargetId}, Count={debugSignalCount}：{PushSignal(signal)}", this);
        }

        private void SubscribeServiceEvents()
        {
            if (_subscribed)
            {
                return;
            }

            _questService.OnQuestChanged += PublishQuestChanged;
            _questService.OnQuestAccepted += PublishQuestAccepted;
            _questService.OnObjectiveProgressed += PublishObjectiveProgressed;
            _questService.OnStageChanged += PublishStageChanged;
            _questService.OnStageAdvanced += PublishStageAdvanced;
            _questService.OnQuestCompleted += PublishQuestCompleted;
            _questService.OnQuestFailed += PublishQuestFailed;
            _questService.OnTrackingChanged += PublishTrackingChanged;
            _subscribed = true;
        }

        private void UnsubscribeServiceEvents()
        {
            if (!_subscribed)
            {
                return;
            }

            _questService.OnQuestChanged -= PublishQuestChanged;
            _questService.OnQuestAccepted -= PublishQuestAccepted;
            _questService.OnObjectiveProgressed -= PublishObjectiveProgressed;
            _questService.OnStageChanged -= PublishStageChanged;
            _questService.OnStageAdvanced -= PublishStageAdvanced;
            _questService.OnQuestCompleted -= PublishQuestCompleted;
            _questService.OnQuestFailed -= PublishQuestFailed;
            _questService.OnTrackingChanged -= PublishTrackingChanged;
            _subscribed = false;
        }

        private void PublishQuestChanged(QuestChangedEvent evt)
        {
            _eventBus?.Publish(evt);
        }

        private void PublishQuestAccepted(QuestAcceptedEvent evt)
        {
            _eventBus?.Publish(evt);
        }

        private void PublishObjectiveProgressed(QuestObjectiveProgressedEvent evt)
        {
            _eventBus?.Publish(evt);
        }

        private void PublishStageChanged(QuestStageChangedEvent evt)
        {
            _eventBus?.Publish(evt);
        }

        private void PublishStageAdvanced(QuestStageAdvancedEvent evt)
        {
            _eventBus?.Publish(evt);
        }

        private void PublishQuestCompleted(QuestCompletedEvent evt)
        {
            _eventBus?.Publish(evt);
        }

        private void PublishQuestFailed(QuestFailedEvent evt)
        {
            _eventBus?.Publish(evt);
        }

        private void PublishTrackingChanged(QuestTrackingChangedEvent evt)
        {
            _eventBus?.Publish(evt);
        }
    }
}
