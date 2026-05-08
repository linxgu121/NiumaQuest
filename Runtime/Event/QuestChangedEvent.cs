using NiumaQuest.Enum;

namespace NiumaQuest.Event
{
    /// <summary>
    /// 通用任务变化事件。
    /// 存档模块可以监听该事件统一标记任务数据为脏。
    /// </summary>
    public readonly struct QuestChangedEvent
    {
        /// <summary>
        /// 发生变化的任务 ID。
        /// </summary>
        public readonly string QuestId;

        /// <summary>
        /// 变化类型。
        /// </summary>
        public readonly QuestChangeType ChangeType;

        public QuestChangedEvent(string questId, QuestChangeType changeType)
        {
            QuestId = questId;
            ChangeType = changeType;
        }
    }
}
