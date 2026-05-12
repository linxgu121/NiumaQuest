using NiumaGal.Dialogue;
using NiumaGal.Dialogue.Data;
using NiumaGal.Dialogue.RuntimeData;
using NiumaGal.Enum;
using NiumaQuest.Controller;
using NiumaQuest.Enum;
using NiumaQuest.Signal;
using UnityEngine;

namespace NiumaQuest.Bridge.Gal
{
    /// <summary>
    /// NiumaGal 到 NiumaQuest 的数据驱动桥接层。
    /// 通过读取对话黑板状态边沿，把一次完整对话转换为任务 Talk 信号。
    /// </summary>
    public sealed class QuestDialogueBridge : MonoBehaviour
    {
        [Header("模块引用")]
        [Tooltip("任务模块根控制器。请拖入场景中的 NiumaQuestController。为空时可按配置自动查找。")]
        [SerializeField] private NiumaQuestController questController;

        [Tooltip("对话模块控制器。请拖入场景中的 NiumaDialogueController。为空时可按配置自动查找。")]
        [SerializeField] private NiumaDialogueController dialogueController;

        [Header("自动查找")]
        [Tooltip("未手动绑定任务控制器时，是否自动在场景中查找 NiumaQuestController。")]
        [SerializeField] private bool autoFindQuestController = true;

        [Tooltip("未手动绑定对话控制器时，是否自动在场景中查找 NiumaDialogueController。")]
        [SerializeField] private bool autoFindDialogueController = true;

        [Header("信号配置")]
        [Tooltip("推送给任务模块的目标类型。对话桥接通常保持为 Talk。")]
        [SerializeField] private QuestObjectiveType objectiveType = QuestObjectiveType.Talk;

        [Tooltip("信号来源模块名。只用于日志和调试追踪。")]
        [SerializeField] private string sourceModule = "NiumaGal";

        [Tooltip("开发期兜底：DialogueAsset.DialogueId 为空时是否回退使用资源名。正式内容应关闭，并要求所有任务对话填写稳定 DialogueId。")]
        [SerializeField] private bool fallbackToAssetName = true;

        [Tooltip("是否要求对话推进到最后一句之后才推送任务信号。开启后，强制关闭或中途跳出不会计入任务进度。")]
        [SerializeField] private bool requireAllSentencesAdvanced = true;

        [Header("刷新策略")]
        [Tooltip("是否在 LateUpdate 中轮询对话黑板。关闭后桥接层不会自动推送任务信号。")]
        [SerializeField] private bool tickInLateUpdate = true;

        [Header("日志")]
        [Tooltip("缺少必要引用或配置时是否打印警告。")]
        [SerializeField] private bool logWarnings = true;

        private NiumaGalBlackboard _blackboard;
        private DialogueAsset _activeDialogue;
        private bool _wasInteracting;

        private void Reset()
        {
            ResolveReferences(false);
        }

        private void OnEnable()
        {
            ResolveReferences(true);
            CaptureCurrentDialogueState();
        }

        private void LateUpdate()
        {
            if (!tickInLateUpdate)
            {
                return;
            }

            TickBridge();
        }

        /// <summary>
        /// 手动驱动一次桥接检查。
        /// 用于自定义模块管线统一调度时替代 LateUpdate。
        /// </summary>
        public void TickBridge()
        {
            if (!ResolveRuntime(true))
            {
                return;
            }

            var isInteracting = _blackboard.InteractionState != InteractionState.Idle;
            if (isInteracting)
            {
                _activeDialogue = _blackboard.CurrentDialogue;
                _wasInteracting = true;
                return;
            }

            if (!_wasInteracting)
            {
                return;
            }

            var completedDialogue = _activeDialogue;
            _activeDialogue = null;
            _wasInteracting = false;

            if (completedDialogue != null)
            {
                if (!IsDialogueCompleted(completedDialogue))
                {
                    return;
                }

                PushDialogueSignal(completedDialogue);
            }
        }

        private void CaptureCurrentDialogueState()
        {
            if (!ResolveRuntime(false))
            {
                return;
            }

            _wasInteracting = _blackboard.InteractionState != InteractionState.Idle;
            _activeDialogue = _wasInteracting ? _blackboard.CurrentDialogue : null;
        }

        private void PushDialogueSignal(DialogueAsset dialogueAsset)
        {
            var targetId = GetDialogueId(dialogueAsset);
            if (string.IsNullOrWhiteSpace(targetId))
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[QuestDialogueBridge] 对话完成但 DialogueId 为空，无法推送任务 Talk 信号。", this);
                }

                return;
            }

            if (!ResolveQuestController(true))
            {
                return;
            }

            questController.PushSignal(new QuestSignal(objectiveType, targetId, 1, sourceModule));
        }

        private string GetDialogueId(DialogueAsset dialogueAsset)
        {
            if (dialogueAsset == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(dialogueAsset.DialogueId))
            {
                return dialogueAsset.DialogueId;
            }

            if (!fallbackToAssetName)
            {
                return null;
            }

            if (logWarnings)
            {
                Debug.LogWarning("[QuestDialogueBridge] DialogueAsset.DialogueId 为空，当前临时回退使用资源名。正式内容请填写稳定 DialogueId 并关闭 fallbackToAssetName。", dialogueAsset);
            }

            return dialogueAsset.name;
        }

        private bool IsDialogueCompleted(DialogueAsset dialogueAsset)
        {
            if (!requireAllSentencesAdvanced)
            {
                return true;
            }

            var sentenceCount = dialogueAsset?.Sentences?.Count ?? 0;
            return sentenceCount > 0 && _blackboard.CurrentSentenceIndex >= sentenceCount;
        }

        private bool ResolveRuntime(bool logMissing)
        {
            if (!ResolveDialogueController(logMissing))
            {
                return false;
            }

            _blackboard = dialogueController.Blackboard;
            if (_blackboard == null && logWarnings && logMissing)
            {
                Debug.LogWarning("[QuestDialogueBridge] DialogueController.Blackboard 为空，无法读取对话状态。", this);
            }

            return _blackboard != null;
        }

        private void ResolveReferences(bool logMissing)
        {
            ResolveQuestController(logMissing);
            ResolveDialogueController(logMissing);
        }

        private bool ResolveQuestController(bool logMissing)
        {
            if (questController != null)
            {
                return true;
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
                Debug.LogWarning("[QuestDialogueBridge] 未找到 NiumaQuestController，请在 Inspector 中绑定任务控制器。", this);
            }

            return questController != null;
        }

        private bool ResolveDialogueController(bool logMissing)
        {
            if (dialogueController != null)
            {
                return true;
            }

            if (autoFindDialogueController)
            {
#if UNITY_2023_1_OR_NEWER
                dialogueController = FindFirstObjectByType<NiumaDialogueController>();
#else
                dialogueController = FindObjectOfType<NiumaDialogueController>();
#endif
            }

            if (dialogueController == null && logWarnings && logMissing)
            {
                Debug.LogWarning("[QuestDialogueBridge] 未找到 NiumaDialogueController，请在 Inspector 中绑定对话控制器。", this);
            }

            return dialogueController != null;
        }
    }
}
