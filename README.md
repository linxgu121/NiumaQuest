# NiumaQuest

## 模块定位
NiumaQuest 是任务模块，负责任务配置、接取、阶段推进、目标计数、奖励状态、追踪状态、存档快照和任务 UI 表现数据。

## 框架设计思路
- 剧情为轴，任务为枝，对话为叶：剧情控制主进度，任务管理目标事实，对话负责表现。
- QuestRuntimeState 只保存稳定 ID 和玩家进度，不保存配置顺序下标。
- 目标推进走 QuestSignal，避免任务模块直接依赖对话、交互、背包等具体实现。
- UI 桥接采用数据驱动 Revision 轮询，输出 QuestTrackerViewData。

## 核心流程
1. QuestController 加载 QuestAsset 配置并创建 QuestService。
2. 外部调用 TryAcceptQuest 接取任务。
3. Gal / Interact / Inventory / Story 桥接推送 QuestSignal。
4. QuestService 匹配当前阶段目标并更新计数。
5. 目标完成后按配置手动或自动推进阶段。
6. 任务完成后进入 Completed，Reward 模块通过 NiumaRewardQuestBridge 接管实际发奖。
7. Reward 发奖成功后回写 Rewarded；发奖失败时任务保持 RewardPending，等待重试。
8. SaveAdapter 保存任务进度与奖励状态，不保存具体物品或经验。

## 模块用法
- QuestId、StageId、ObjectiveId 必须稳定，避免策划调顺序造成存档错位。
- Manual 阶段需要外部显式 TryAdvanceStage。
- 任务 UI 读取桥接层输出的 ViewData，不直接查 QuestAsset。

## 场景使用方法
推荐放置方式：`QuestRoot` 一个任务根物体承载任务服务、桥接和存档；任务触发点放到 NPC/交互物体上。

- `QuestRoot`：挂 `NiumaQuestController`，绑定 QuestAsset 列表，负责创建 IQuestService。
- `QuestRoot/SaveAdapter` 或全局 `SaveRoot`：挂 `NiumaQuestSaveAdapter`。
- `QuestRoot/UIBridge` 或 `UIRoot/Bridges`：挂 `QuestUIViewBridge`，绑定任务追踪 UI Receiver。
- `QuestRoot/Debug`：开发阶段可挂 `NiumaQuestDebugEntry`，用于接取、推进、推送 QuestSignal。
- `DialogueRoot/QuestBridge`：挂 `QuestDialogueBridge`，把 Gal 对话完成转换为任务信号。
- `PlayerRoot/InteractionRoot/QuestBridge` 或交互目标旁：挂 `QuestInteractionBridge`，把交互行为转换为任务信号。
- `NPC_xxx`：任务 NPC 通常同时挂对话交互脚本，由对话或选项推进任务，不建议 NPC 直接改任务运行时状态。
- `UIRoot/QuestTracker`：放任务追踪面板，接收 QuestUIViewBridge 的 ViewData。

## 协作边界
Quest 不发放具体物品、不直接播放对话。`QuestAsset.Rewards` 只作为奖励声明，实际发奖由 `NiumaRewardQuestBridge` 转交 `NiumaReward` 完成。

奖励状态约定：

- `Completed`：任务目标已完成，奖励还未进入发放流程。
- `RewardPending`：奖励正在发放或发放失败等待重试。
- `Rewarded`：奖励已成功发放，任务完全闭环。

因此任务模块只负责 `TrySetRewardPending` 和 `TryMarkRewarded` 这类状态收口，不直接调用背包、成长或自定义奖励处理器。


