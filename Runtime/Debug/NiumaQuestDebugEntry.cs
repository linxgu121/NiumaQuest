using System.Text;
using NiumaQuest.Controller;
using NiumaQuest.Enum;
using NiumaQuest.RuntimeData;
using NiumaQuest.Signal;
using UnityEngine;

namespace NiumaQuest.DebugTools
{
    /// <summary>
    /// NiumaQuest 基础调试入口。
    /// 该组件只用于开发期验证任务流程，不承载正式业务逻辑。
    /// </summary>
    public sealed class NiumaQuestDebugEntry : MonoBehaviour
    {
        [Header("控制器引用")]
        [Tooltip("任务模块根控制器。请拖入场景中的 NiumaQuestController；为空时可按配置自动查找。")]
        [SerializeField] private NiumaQuestController questController;

        [Tooltip("没有手动绑定控制器时，是否在场景中自动查找 NiumaQuestController。调试阶段建议开启，正式场景建议手动绑定。")]
        [SerializeField] private bool autoFindController = true;

        [Header("任务调试")]
        [Tooltip("调试用任务 ID。接取、推进、完成、失败、追踪任务时会使用该 ID。")]
        [SerializeField] private string questId;

        [Tooltip("调试切换阶段用的阶段 ID。调用切换指定阶段时需要填写。")]
        [SerializeField] private string stageId;

        [Header("信号调试")]
        [Tooltip("调试推送的任务目标类型。需要和 QuestObjectiveData.Type 保持一致。")]
        [SerializeField] private QuestObjectiveType signalType = QuestObjectiveType.Talk;

        [Tooltip("调试推送的目标 ID。需要和 QuestObjectiveData.TargetId 保持一致，例如对话 ID、交互物 ID、物品 ID。")]
        [SerializeField] private string signalTargetId;

        [Tooltip("调试推送时增加的目标进度数量。小于 1 时会由 QuestSignal 自动修正为 1。")]
        [SerializeField] private int signalCount = 1;

        [Tooltip("调试信号来源模块名。只用于日志和排查问题。")]
        [SerializeField] private string signalSourceModule = "NiumaQuestDebugEntry";

        private void Reset()
        {
            ResolveController(false);
        }

        private void Awake()
        {
            ResolveController(false);
        }

        /// <summary>
        /// 尝试接取调试任务。
        /// 可被 Unity Button 或临时测试脚本调用。
        /// </summary>
        public void AcceptQuest()
        {
            if (!TryGetControllerAndQuestId(out var controller))
            {
                return;
            }

            LogResult("接取任务", controller.TryAcceptQuest(questId));
        }

        /// <summary>
        /// 尝试推进调试任务到下一阶段。
        /// </summary>
        public void AdvanceStage()
        {
            if (!TryGetControllerAndQuestId(out var controller))
            {
                return;
            }

            LogResult("推进阶段", controller.TryAdvanceStage(questId));
        }

        /// <summary>
        /// 尝试将调试任务切换到指定阶段。
        /// </summary>
        public void SetStage()
        {
            if (!TryGetControllerAndQuestId(out var controller))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(stageId))
            {
                Debug.LogWarning("[NiumaQuestDebug] 阶段 ID 为空，无法切换指定阶段。", this);
                return;
            }

            LogResult($"切换阶段 {stageId}", controller.TrySetStage(questId, stageId));
        }

        /// <summary>
        /// 尝试直接完成调试任务。
        /// </summary>
        public void CompleteQuest()
        {
            if (!TryGetControllerAndQuestId(out var controller))
            {
                return;
            }

            LogResult("完成任务", controller.TryCompleteQuest(questId));
        }

        /// <summary>
        /// 尝试将调试任务标记为失败。
        /// </summary>
        public void FailQuest()
        {
            if (!TryGetControllerAndQuestId(out var controller))
            {
                return;
            }

            LogResult("任务失败", controller.TryFailQuest(questId));
        }

        /// <summary>
        /// 尝试追踪调试任务。
        /// </summary>
        public void TrackQuest()
        {
            if (!TryGetControllerAndQuestId(out var controller))
            {
                return;
            }

            LogResult("追踪任务", controller.TryTrackQuest(questId));
        }

        /// <summary>
        /// 尝试取消追踪调试任务。
        /// </summary>
        public void UntrackQuest()
        {
            if (!TryGetControllerAndQuestId(out var controller))
            {
                return;
            }

            LogResult("取消追踪任务", controller.TryUntrackQuest(questId));
        }

        /// <summary>
        /// 向任务服务推送调试信号。
        /// 用于验证 Talk、Interact、Collect 等目标计数是否正常推进。
        /// </summary>
        public void PushSignal()
        {
            var controller = ResolveController(true);
            if (controller == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(signalTargetId))
            {
                Debug.LogWarning("[NiumaQuestDebug] 信号目标 ID 为空，无法推送任务信号。", this);
                return;
            }

            var signal = new QuestSignal(signalType, signalTargetId, signalCount, signalSourceModule);
            LogResult($"推送信号 Type={signalType}, TargetId={signalTargetId}, Count={signalCount}", controller.PushSignal(signal));
        }

        /// <summary>
        /// 打印当前调试任务的快照。
        /// </summary>
        public void PrintQuestSnapshot()
        {
            if (!TryGetControllerAndQuestId(out var controller))
            {
                return;
            }

            if (!controller.TryGetQuestSnapshot(questId, out var snapshot))
            {
                Debug.LogWarning($"[NiumaQuestDebug] 未找到任务快照：{questId}", this);
                return;
            }

            Debug.Log(BuildSnapshotLog(snapshot), this);
        }

        /// <summary>
        /// 打印当前所有任务存档快照。
        /// </summary>
        public void PrintAllSnapshots()
        {
            var controller = ResolveController(true);
            if (controller == null)
            {
                return;
            }

            var snapshots = controller.ExportSnapshots();
            var builder = new StringBuilder();
            builder.AppendLine($"[NiumaQuestDebug] 当前导出任务快照数量：{snapshots.Length}");

            for (var i = 0; i < snapshots.Length; i++)
            {
                builder.AppendLine(BuildSnapshotLog(snapshots[i]));
            }

            Debug.Log(builder.ToString(), this);
        }

        [ContextMenu("任务调试/接取任务")]
        private void ContextAcceptQuest()
        {
            AcceptQuest();
        }

        [ContextMenu("任务调试/推进阶段")]
        private void ContextAdvanceStage()
        {
            AdvanceStage();
        }

        [ContextMenu("任务调试/切换指定阶段")]
        private void ContextSetStage()
        {
            SetStage();
        }

        [ContextMenu("任务调试/完成任务")]
        private void ContextCompleteQuest()
        {
            CompleteQuest();
        }

        [ContextMenu("任务调试/标记失败")]
        private void ContextFailQuest()
        {
            FailQuest();
        }

        [ContextMenu("任务调试/追踪任务")]
        private void ContextTrackQuest()
        {
            TrackQuest();
        }

        [ContextMenu("任务调试/取消追踪")]
        private void ContextUntrackQuest()
        {
            UntrackQuest();
        }

        [ContextMenu("任务调试/推送任务信号")]
        private void ContextPushSignal()
        {
            PushSignal();
        }

        [ContextMenu("任务调试/打印当前任务快照")]
        private void ContextPrintQuestSnapshot()
        {
            PrintQuestSnapshot();
        }

        [ContextMenu("任务调试/打印全部任务快照")]
        private void ContextPrintAllSnapshots()
        {
            PrintAllSnapshots();
        }

        private bool TryGetControllerAndQuestId(out NiumaQuestController controller)
        {
            controller = ResolveController(true);
            if (controller == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(questId))
            {
                Debug.LogWarning("[NiumaQuestDebug] 任务 ID 为空，无法执行任务调试操作。", this);
                return false;
            }

            return true;
        }

        private NiumaQuestController ResolveController(bool logMissing)
        {
            if (questController != null)
            {
                return questController;
            }

            if (autoFindController)
            {
#if UNITY_2023_1_OR_NEWER
                questController = FindFirstObjectByType<NiumaQuestController>();
#else
                questController = FindObjectOfType<NiumaQuestController>();
#endif
            }

            if (questController == null && logMissing)
            {
                Debug.LogWarning("[NiumaQuestDebug] 未找到 NiumaQuestController，请在 Inspector 中绑定控制器。", this);
            }

            return questController;
        }

        private void LogResult(string actionName, bool result)
        {
            Debug.Log($"[NiumaQuestDebug] {actionName} QuestId={questId}：{result}", this);
        }

        private static string BuildSnapshotLog(QuestProgressSnapshot snapshot)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"[NiumaQuestDebug] QuestId={snapshot.QuestId}, State={snapshot.State}, Stage={snapshot.CurrentStageId}, Tracked={snapshot.IsTracked}");

            if (snapshot.Objectives == null || snapshot.Objectives.Length == 0)
            {
                builder.AppendLine("  Objectives: Empty");
                return builder.ToString();
            }

            for (var i = 0; i < snapshot.Objectives.Length; i++)
            {
                var objective = snapshot.Objectives[i];
                if (objective == null)
                {
                    builder.AppendLine($"  [{i}] Null");
                    continue;
                }

                builder.AppendLine($"  [{i}] ObjectiveId={objective.ObjectiveId}, Count={objective.CurrentCount}, Completed={objective.IsCompleted}");
            }

            return builder.ToString();
        }
    }
}
