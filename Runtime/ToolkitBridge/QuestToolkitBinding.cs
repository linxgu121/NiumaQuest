using System;
using System.Collections.Generic;
using NiumaQuest.Bridge;
using NiumaUI.Toolkit;
using NiumaUI.Toolkit.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace NiumaQuest.ToolkitBridge
{
    public sealed class QuestToolkitBindingProvider : ToolkitViewBindingProviderBase
    {
        [Serializable] public sealed class QuestIdEvent : UnityEvent<string> { }

        [Header("元素名称")]
        [SerializeField, Tooltip("标题 Label 的 name。默认 TitleText。")]
        private string titleLabelName = "TitleText";
        [SerializeField, Tooltip("状态 Label 的 name。默认 StatusText。")]
        private string statusLabelName = "StatusText";
        [SerializeField, Tooltip("目标列表 ListView 的 name。默认 ListRoot。")]
        private string listViewName = "ListRoot";
        [SerializeField, Tooltip("详情 Label 的 name。显示当前任务阶段描述。")]
        private string detailLabelName = "DetailText";
        [SerializeField, Tooltip("结果 Label 的 name。显示缺失配置等提示。")]
        private string resultLabelName = "ResultText";
        [SerializeField, Tooltip("空状态节点的 name。没有追踪任务时显示。")]
        private string emptyRootName = "EmptyRoot";

        [Header("按钮名称")]
        [SerializeField, Tooltip("追踪/取消追踪按钮 name。点击时把当前 QuestId 传给 On Track Requested。")]
        private string trackButtonName = "TrackButton";
        [SerializeField, Tooltip("放弃任务按钮 name。点击时把当前 QuestId 传给 On Abandon Requested。")]
        private string abandonButtonName = "AbandonButton";

        [Header("列表")]
        [SerializeField, Tooltip("最多显示多少个目标。")]
        private int maxRows = 20;
        [SerializeField, Tooltip("目标行 USS class。")]
        private string rowClass = "niuma-quest-objective-row";
        [SerializeField, Tooltip("选中行 USS class。")]
        private string selectedRowClass = "is-selected";
        [SerializeField, Tooltip("禁用行 USS class。")]
        private string disabledRowClass = "is-disabled";

        [Header("交互事件")]
        [SerializeField, Tooltip("点击目标行时触发。参数为 ObjectiveId。")]
        private QuestIdEvent onObjectiveSelected = new QuestIdEvent();
        [SerializeField, Tooltip("点击 TrackButton 时触发。参数为 QuestId。")]
        private QuestIdEvent onTrackRequested = new QuestIdEvent();
        [SerializeField, Tooltip("点击 AbandonButton 时触发。参数为 QuestId。")]
        private QuestIdEvent onAbandonRequested = new QuestIdEvent();

        protected override string DefaultProviderId => "QuestTracker";

        public override IToolkitViewBinding CreateBinding()
        {
            return new QuestToolkitBinding(
                titleLabelName,
                statusLabelName,
                listViewName,
                detailLabelName,
                resultLabelName,
                emptyRootName,
                trackButtonName,
                abandonButtonName,
                maxRows,
                rowClass,
                selectedRowClass,
                disabledRowClass,
                id => onObjectiveSelected?.Invoke(id),
                id => onTrackRequested?.Invoke(id),
                id => onAbandonRequested?.Invoke(id));
        }
    }

    public sealed class QuestToolkitViewModel : UIPanelViewModelBase
    {
        public readonly List<ToolkitTextRowData> Rows = new List<ToolkitTextRowData>();
        public QuestTrackerViewData Tracker { get; private set; }
        public QuestUIUpdateType UpdateType { get; private set; }
        public int Revision { get; private set; }
        public string SelectedObjectiveId { get; private set; }
        public int PageIndex { get; private set; }
        public string SearchKeyword { get; private set; }

        public void Apply(QuestUIUpdate update, int maxRows)
        {
            Tracker = update.TrackerData;
            UpdateType = update.UpdateType;
            Revision = update.Revision;
            SetContext(Tracker?.QuestId);
            RebuildRows(maxRows);
            MarkDirty();
        }

        public void Select(string objectiveId)
        {
            SelectedObjectiveId = string.IsNullOrWhiteSpace(objectiveId) ? null : objectiveId.Trim();
            RebuildRows(int.MaxValue);
            MarkDirty();
        }

        protected override void OnClear(UIViewModelClearReason reason)
        {
            Tracker = null;
            UpdateType = QuestUIUpdateType.Cleared;
            Revision = 0;
            SelectedObjectiveId = null;
            PageIndex = 0;
            SearchKeyword = string.Empty;
            Rows.Clear();
        }

        private void RebuildRows(int maxRows)
        {
            Rows.Clear();
            var objectives = Tracker?.Objectives ?? Array.Empty<QuestObjectiveViewData>();
            var limit = Math.Max(1, maxRows);
            for (var i = 0; i < objectives.Length && i < limit; i++)
            {
                var objective = objectives[i];
                if (objective == null)
                    continue;

                var id = string.IsNullOrWhiteSpace(objective.ObjectiveId) ? $"objective:{i}" : objective.ObjectiveId;
                Rows.Add(new ToolkitTextRowData(id, $"{(objective.IsCompleted ? "[完成]" : "[进行]")} {Text(objective.Description, objective.ObjectiveId)} {objective.CurrentCount}/{objective.RequiredCount}{(objective.MissingConfig ? " | 缺失配置" : string.Empty)}", string.Equals(SelectedObjectiveId, id, StringComparison.Ordinal), !objective.MissingConfig, objective));
            }
        }

        private static string Text(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value;
        }
    }

    public sealed class QuestToolkitBinding : ToolkitViewBindingBase<QuestUIUpdate, QuestToolkitViewModel>
    {
        private readonly string _titleName;
        private readonly string _statusName;
        private readonly string _listName;
        private readonly string _detailName;
        private readonly string _resultName;
        private readonly string _emptyName;
        private readonly string _trackButtonName;
        private readonly string _abandonButtonName;
        private readonly int _maxRows;
        private readonly string _rowClass;
        private readonly string _selectedClass;
        private readonly string _disabledClass;
        private readonly Action<string> _objectiveSelected;
        private readonly Action<string> _trackRequested;
        private readonly Action<string> _abandonRequested;
        private readonly ToolkitListBinding<ToolkitTextRowData> _listBinding = new ToolkitListBinding<ToolkitTextRowData>();
        private Label _title;
        private Label _status;
        private Label _detail;
        private Label _result;

        public QuestToolkitBinding(string titleName, string statusName, string listName, string detailName, string resultName, string emptyName, string trackButtonName, string abandonButtonName, int maxRows, string rowClass, string selectedClass, string disabledClass, Action<string> objectiveSelected, Action<string> trackRequested, Action<string> abandonRequested)
        {
            _titleName = titleName;
            _statusName = statusName;
            _listName = listName;
            _detailName = detailName;
            _resultName = resultName;
            _emptyName = emptyName;
            _trackButtonName = trackButtonName;
            _abandonButtonName = abandonButtonName;
            _maxRows = Mathf.Max(1, maxRows);
            _rowClass = string.IsNullOrWhiteSpace(rowClass) ? "niuma-quest-objective-row" : rowClass.Trim();
            _selectedClass = selectedClass;
            _disabledClass = disabledClass;
            _objectiveSelected = objectiveSelected;
            _trackRequested = trackRequested;
            _abandonRequested = abandonRequested;
        }

        protected override void OnInitializeTyped()
        {
            _title = QLabel(_titleName);
            _status = QLabel(_statusName);
            _detail = QLabel(_detailName);
            _result = QLabel(_resultName);
            _listBinding.Bind(Root, _listName, new ToolkitTextRowItemBinder(_rowClass, _selectedClass, _disabledClass, HandleRowClicked), _emptyName);
            Callbacks.RegisterButton(Root, _trackButtonName, () => InvokeQuest(_trackRequested), HasQuest);
            Callbacks.RegisterButton(Root, _abandonButtonName, () => InvokeQuest(_abandonRequested), HasQuest);
            ApplyVisualState(ViewModel);
        }

        protected override void OnRefreshTyped(QuestUIUpdate viewData, QuestToolkitViewModel viewModel)
        {
            viewModel.Apply(viewData, _maxRows);
            ApplyVisualState(viewModel);
        }

        protected override void OnClearTyped(UIViewModelClearReason reason)
        {
            _listBinding.Clear();
            ApplyVisualState(ViewModel);
        }

        protected override void OnDisposeTyped()
        {
            _listBinding.Dispose();
        }

        private void HandleRowClicked(ToolkitTextRowData row)
        {
            if (row == null)
                return;

            ViewModel.Select(row.Id);
            _objectiveSelected?.Invoke(row.Id);
            ApplyVisualState(ViewModel);
        }

        private void ApplyVisualState(QuestToolkitViewModel viewModel)
        {
            var tracker = viewModel?.Tracker;
            SetText(_title, tracker == null ? "????" : Text(tracker.Title, tracker.QuestId));
            _listBinding.ReplaceAll(viewModel != null ? viewModel.Rows : Array.Empty<ToolkitTextRowData>());
            var updateType = viewModel != null ? viewModel.UpdateType : QuestUIUpdateType.Cleared;
            SetText(_status, tracker == null ? $"???{updateType}" : $"Revision {viewModel.Revision} | {tracker.State} | ?? {Text(tracker.CurrentStageId, "?")}");
            SetText(_detail, tracker == null ? "?????????" : $"{tracker.Description}\n{tracker.StageDescription}".Trim());
            SetText(_result, tracker != null && (tracker.MissingQuestConfig || tracker.MissingStageConfig) ? "???????? QuestAsset?" : string.Empty);
        }

        private bool HasQuest()
        {
            return !string.IsNullOrWhiteSpace(ViewModel?.Tracker?.QuestId);
        }

        private void InvokeQuest(Action<string> action)
        {
            if (HasQuest())
                action?.Invoke(ViewModel.Tracker.QuestId);
        }

        private static string Text(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback ?? string.Empty : value;
        }
    }
}
