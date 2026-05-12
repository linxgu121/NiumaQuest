using NiumaInteract.Core;
using NiumaInteract.Core.Data;
using NiumaInteract.Core.Interface;
using NiumaQuest.Controller;
using NiumaQuest.Enum;
using NiumaQuest.Signal;
using UnityEngine;

namespace NiumaQuest.Bridge.Interact
{
    /// <summary>
    /// NiumaInteract 到 NiumaQuest 的数据驱动桥接层。
    /// 通过读取交互黑板结果版本号，把一次成功交互转换为任务 Interact 信号。
    /// </summary>
    public sealed class QuestInteractionBridge : MonoBehaviour
    {
        [Header("模块引用")]
        [Tooltip("任务模块根控制器。请拖入场景中的 NiumaQuestController。为空时可按配置自动查找。")]
        [SerializeField] private NiumaQuestController questController;

        [Tooltip("交互模块控制器。请拖入场景中的 NiumaInteractionController。为空时可按配置自动查找。")]
        [SerializeField] private NiumaInteractionController interactionController;

        [Header("自动查找")]
        [Tooltip("未手动绑定任务控制器时，是否自动在场景中查找 NiumaQuestController。")]
        [SerializeField] private bool autoFindQuestController = true;

        [Tooltip("未手动绑定交互控制器时，是否自动在场景中查找 NiumaInteractionController。")]
        [SerializeField] private bool autoFindInteractionController = true;

        [Header("信号配置")]
        [Tooltip("推送给任务模块的目标类型。普通交互桥接通常保持为 Interact。")]
        [SerializeField] private QuestObjectiveType objectiveType = QuestObjectiveType.Interact;

        [Tooltip("信号来源模块名。只用于日志和调试追踪。")]
        [SerializeField] private string sourceModule = "NiumaInteract";

        [Tooltip("是否忽略 InteractionId 为空的交互目标。关闭时会回退使用目标显示名。正式内容建议保持开启并配置稳定 InteractionId。")]
        [SerializeField] private bool requireInteractionId = true;

        [Header("刷新策略")]
        [Tooltip("是否在 LateUpdate 中按交互结果版本号自动检查。关闭后需要外部手动调用 TickBridge。")]
        [SerializeField] private bool tickInLateUpdate = true;

        [Header("日志")]
        [Tooltip("缺少必要引用或配置时是否打印警告。")]
        [SerializeField] private bool logWarnings = true;

        private InteractionBlackboard _blackboard;
        private int _observedResultRevision = -1;

        private void Reset()
        {
            ResolveReferences(false);
        }

        private void OnEnable()
        {
            ResolveReferences(true);
            CaptureCurrentResultRevision();
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

            if (_observedResultRevision == _blackboard.ResultRevision)
            {
                return;
            }

            _observedResultRevision = _blackboard.ResultRevision;
            if (!_blackboard.HasLastResult)
            {
                return;
            }

            var result = _blackboard.LastResult;
            if (!result.Succeeded || result.Target == null)
            {
                return;
            }

            PushInteractionSignal(result);
        }

        private void CaptureCurrentResultRevision()
        {
            if (!ResolveRuntime(false))
            {
                return;
            }

            _observedResultRevision = _blackboard.ResultRevision;
        }

        private void PushInteractionSignal(in InteractionResult result)
        {
            var targetId = GetTargetId(result.Target);
            if (string.IsNullOrWhiteSpace(targetId))
            {
                if (logWarnings)
                {
                    Debug.LogWarning("[QuestInteractionBridge] 交互成功但目标 ID 为空，无法推送任务 Interact 信号。", this);
                }

                return;
            }

            if (!ResolveQuestController(true))
            {
                return;
            }

            questController.PushSignal(new QuestSignal(objectiveType, targetId, 1, sourceModule));
        }

        private string GetTargetId(IInteractable target)
        {
            if (target == null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(target.InteractionId))
            {
                return target.InteractionId;
            }

            return requireInteractionId ? null : target.DisplayName;
        }

        private bool ResolveRuntime(bool logMissing)
        {
            if (!ResolveInteractionController(logMissing))
            {
                return false;
            }

            _blackboard = interactionController.Blackboard;
            if (_blackboard == null && logWarnings && logMissing)
            {
                Debug.LogWarning("[QuestInteractionBridge] InteractionController.Blackboard 为空，无法读取交互结果。", this);
            }

            return _blackboard != null;
        }

        private void ResolveReferences(bool logMissing)
        {
            ResolveQuestController(logMissing);
            ResolveInteractionController(logMissing);
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
                Debug.LogWarning("[QuestInteractionBridge] 未找到 NiumaQuestController，请在 Inspector 中绑定任务控制器。", this);
            }

            return questController != null;
        }

        private bool ResolveInteractionController(bool logMissing)
        {
            if (interactionController != null)
            {
                return true;
            }

            if (autoFindInteractionController)
            {
#if UNITY_2023_1_OR_NEWER
                interactionController = FindFirstObjectByType<NiumaInteractionController>();
#else
                interactionController = FindObjectOfType<NiumaInteractionController>();
#endif
            }

            if (interactionController == null && logWarnings && logMissing)
            {
                Debug.LogWarning("[QuestInteractionBridge] 未找到 NiumaInteractionController，请在 Inspector 中绑定交互控制器。", this);
            }

            return interactionController != null;
        }
    }
}
