using System;
using NiumaQuest.Bridge;
using NiumaUI.Toolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace NiumaQuest.ToolkitBridge
{
    public sealed class QuestToolkitBindingProvider : MonoBehaviour, IToolkitViewBindingProvider
    {
        [SerializeField, Tooltip("BindingProviderId，默认 QuestTracker。需要和 UIToolkitViewRegistrySO 任务 View 的 BindingProviderId 一致。")] private string providerId = "QuestTracker";
        [SerializeField] private string titleLabelName = "TitleText";
        [SerializeField] private string statusLabelName = "StatusText";
        [SerializeField] private string listRootName = "ListRoot";
        [SerializeField] private string detailLabelName = "DetailText";
        [SerializeField] private string resultLabelName = "ResultText";
        [SerializeField] private string emptyRootName = "EmptyRoot";
        [SerializeField] private int maxRows = 20;
        [SerializeField] private string rowClass = "niuma-quest-objective-row";

        public string ProviderId => string.IsNullOrWhiteSpace(providerId) ? "QuestTracker" : providerId.Trim();
        public IToolkitViewBinding CreateBinding() => new QuestToolkitBinding(titleLabelName, statusLabelName, listRootName, detailLabelName, resultLabelName, emptyRootName, maxRows, rowClass);
    }

    public sealed class QuestToolkitBinding : ToolkitViewBindingBase
    {
        private readonly string _titleName, _statusName, _listName, _detailName, _resultName, _emptyName, _rowClass;
        private readonly int _maxRows;
        private Label _title, _status, _detail, _result;
        private VisualElement _list, _empty;

        public QuestToolkitBinding(string titleName, string statusName, string listName, string detailName, string resultName, string emptyName, int maxRows, string rowClass)
        {
            _titleName = titleName; _statusName = statusName; _listName = listName; _detailName = detailName; _resultName = resultName; _emptyName = emptyName;
            _maxRows = Mathf.Max(1, maxRows);
            _rowClass = string.IsNullOrWhiteSpace(rowClass) ? "niuma-quest-objective-row" : rowClass.Trim();
        }

        protected override void OnInitialize()
        {
            _title = QL(_titleName); _status = QL(_statusName); _list = QE(_listName); _detail = QL(_detailName); _result = QL(_resultName); _empty = QE(_emptyName);
            Apply(null, QuestUIUpdateType.Cleared, 0);
        }

        protected override void OnRefresh(object viewData)
        {
            if (viewData is QuestUIUpdate update) Apply(update.TrackerData, update.UpdateType, update.Revision);
            else Apply(null, QuestUIUpdateType.Cleared, 0);
        }

        protected override void OnClose() => Apply(null, QuestUIUpdateType.Cleared, 0);

        private void Apply(QuestTrackerViewData tracker, QuestUIUpdateType updateType, int revision)
        {
            Clear();
            Set(_title, tracker == null ? "任务追踪" : Text(tracker.Title, tracker.QuestId));
            SetVisible(_empty, tracker == null);

            if (tracker == null)
            {
                Set(_status, $"状态：{updateType}");
                Set(_detail, "当前没有追踪任务。");
                Set(_result, string.Empty);
                return;
            }

            var objectives = tracker.Objectives ?? Array.Empty<QuestObjectiveViewData>();
            Set(_status, $"Revision {revision} | {tracker.State} | 阶段 {Text(tracker.CurrentStageId, "无")}");
            Set(_detail, $"{tracker.Description}\n{tracker.StageDescription}".Trim());
            Set(_result, tracker.MissingQuestConfig || tracker.MissingStageConfig ? "配置缺失，请检查 QuestAsset。" : string.Empty);

            for (var i = 0; i < objectives.Length && i < _maxRows; i++)
            {
                var objective = objectives[i];
                if (objective == null) continue;
                Add($"{(objective.IsCompleted ? "[完成]" : "[进行]")} {Text(objective.Description, objective.ObjectiveId)} {objective.CurrentCount}/{objective.RequiredCount}{(objective.MissingConfig ? " | 缺失配置" : string.Empty)}");
            }
        }

        private Label QL(string name) => string.IsNullOrWhiteSpace(name) ? null : Query<Label>(name.Trim());
        private VisualElement QE(string name) => string.IsNullOrWhiteSpace(name) ? null : Root?.Q<VisualElement>(name.Trim());
        private void Clear() { if (_list != null) _list.Clear(); }
        private void Add(string text) { if (_list == null) return; var row = new Label(text ?? string.Empty); row.AddToClassList(_rowClass); _list.Add(row); }
        private static void Set(Label label, string text) { if (label != null) label.text = text ?? string.Empty; }
        private static void SetVisible(VisualElement element, bool visible) { if (element != null) element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None; }
        private static string Text(string value, string fallback) => string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value;
    }
}
