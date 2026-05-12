namespace NiumaQuest.Bridge
{
    /// <summary>
    /// 任务 UI 接收接口。
    /// 由具体 UI 组件实现，任务桥接层只负责调用接口，不直接修改具体控件。
    /// </summary>
    public interface IQuestUIReceiver
    {
        /// <summary>
        /// 应用任务 UI 更新。
        /// update 中已经包含 UI 需要的标题、描述、目标描述和 RequiredCount，
        /// UI 层不需要再回查 QuestAsset。
        /// </summary>
        void ApplyQuestUpdate(QuestUIUpdate update);
    }
}
