using NiumaQuest.Bridge;
using NiumaUI.Toolkit;
using UnityEngine;

namespace NiumaQuest.ToolkitBridge
{
    /// <summary>
    /// 任务 UI Toolkit 接收器。
    /// 挂在 UIRoot/UIBridges 下，并拖给 QuestUIViewBridge 的 Quest UI Receiver Provider。
    /// </summary>
    public sealed class QuestToolkitReceiver : MonoBehaviour, IQuestUIReceiver
    {
        [Header("Toolkit")]
        [Tooltip("UI Toolkit 根控制器。拖核心场景 UIRoot/UIManager 上的 UIToolkitUIManager。")]
        [SerializeField] private UIToolkitUIManager uiManager;

        [Tooltip("任务追踪 ViewId。需要在 UIToolkitViewRegistrySO 中注册同名 View。")]
        [SerializeField] private string questViewId = "QuestTracker";

        [Tooltip("收到任务追踪刷新时，如果窗口尚未打开，是否自动打开。")]
        [SerializeField] private bool autoOpenView = true;

        [Tooltip("收到 Cleared 更新时是否关闭任务追踪窗口。")]
        [SerializeField] private bool closeOnCleared = true;

        [Header("调试")]
        [Tooltip("缺少 UIManager 或 ViewId 未注册时是否输出警告。")]
        [SerializeField] private bool logWarnings = true;

        public void ApplyQuestUpdate(QuestUIUpdate update)
        {
            if (update.UpdateType == QuestUIUpdateType.Cleared)
            {
                if (closeOnCleared && uiManager != null)
                    uiManager.CloseView(questViewId);

                RefreshOrOpen(update);
                return;
            }

            RefreshOrOpen(update);
        }

        private void RefreshOrOpen(QuestUIUpdate update)
        {
            if (!EnsureUIManager())
                return;

            var refreshed = uiManager.RefreshView(questViewId, update);
            if (!refreshed && autoOpenView)
                refreshed = uiManager.OpenView(questViewId, update);

            if (!refreshed)
                Warn($"没有刷新到任务 Toolkit View：ViewId={questViewId}。请检查 UIToolkitViewRegistrySO 和 BindingProvider。");
        }

        private bool EnsureUIManager()
        {
            if (uiManager == null)
                uiManager = FindSceneObject<UIToolkitUIManager>();

            if (uiManager != null)
                return true;

            Warn("未绑定 UIToolkitUIManager，任务 Toolkit 面板无法刷新。");
            return false;
        }

        private void Warn(string message)
        {
            if (logWarnings && !string.IsNullOrWhiteSpace(message))
                UnityEngine.Debug.LogWarning($"[QuestToolkitReceiver] {message}", this);
        }

        private static T FindSceneObject<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return FindFirstObjectByType<T>();
#else
            return FindObjectOfType<T>();
#endif
        }
    }
}
