using System;
using UnityEngine;

namespace NiumaQuest.Data
{
    /// <summary>
    /// 任务奖励配置。
    /// 这里只描述奖励请求，实际发放成功与否由后续效果系统或外部模块确认。
    /// </summary>
    [Serializable]
    public sealed class QuestRewardData
    {
        [Tooltip("奖励唯一 ID。用于日志、回溯和防止重复发放。")]
        public string RewardId;

        [Tooltip("奖励类型。先用字符串保持低耦合，例如 item、gold、exp、skill_point。")]
        public string RewardType;

        [Tooltip("奖励目标 ID。例如物品 ID、技能点类型、货币 ID。")]
        public string TargetId;

        [Tooltip("奖励数量，最小为 1。")]
        [Min(1)]
        public int Amount = 1;
    }
}
