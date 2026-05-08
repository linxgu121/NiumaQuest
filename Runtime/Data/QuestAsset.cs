using System;
using UnityEngine;

namespace NiumaQuest.Data
{
    /// <summary>
    /// 任务静态配置。
    /// 只保存任务定义，不保存玩家运行时进度。
    /// </summary>
    [CreateAssetMenu(menuName = "NiumaQuest/Quest Asset", fileName = "QuestAsset")]
    public sealed class QuestAsset : ScriptableObject
    {
        [Tooltip("任务唯一 ID。必须稳定，不要使用资源名或显示文本作为唯一 ID。")]
        public string QuestId;

        [Tooltip("任务标题。后续接本地化时可以改成本地化 Key。")]
        public string Title;

        [Tooltip("任务描述。用于任务列表或详情界面展示。")]
        [TextArea]
        public string Description;

        [Tooltip("任务阶段列表。阶段身份以 StageId 为准，不以数组下标为准。")]
        public QuestStageData[] Stages = Array.Empty<QuestStageData>();

        [Tooltip("任务奖励配置。奖励实际发放由后续效果系统或外部模块确认。")]
        public QuestRewardData[] Rewards = Array.Empty<QuestRewardData>();

        [Tooltip("接取任务后是否自动设置为当前追踪任务。")]
        public bool AutoTrackOnAccept = true;

        [Tooltip("任务是否允许重复接取。MVP 阶段可以先不实现重复任务。")]
        public bool Repeatable;
    }
}
