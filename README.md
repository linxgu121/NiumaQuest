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
- `QuestRoot/UIBridge` 或 `UIRoot/Bridges`：挂 `QuestUIViewBridge`，正式场景把 `Receiver Provider` 绑定到 `QuestToolkitReceiver`。
- `QuestRoot/Debug`：开发阶段可挂 `NiumaQuestDebugEntry`，用于接取、推进、推送 QuestSignal。
- `DialogueRoot/QuestBridge`：挂 `QuestDialogueBridge`，把 Gal 对话完成转换为任务信号。
- `PlayerRoot/InteractionRoot/QuestBridge` 或交互目标旁：挂 `QuestInteractionBridge`，把交互行为转换为任务信号。
- `NPC_xxx`：任务 NPC 通常同时挂对话交互脚本，由对话或选项推进任务，不建议 NPC 直接改任务运行时状态。
- `UIRoot/UIBridges/QuestToolkitReceiver`：接收 `QuestUIViewBridge` 输出的 ViewData；`QuestTracker` View 在 `UIToolkitViewRegistrySO` 中注册。

## 协作边界
Quest 不发放具体物品、不直接播放对话。`QuestAsset.Rewards` 只作为奖励声明，实际发奖由 `NiumaRewardQuestBridge` 转交 `NiumaReward` 完成。

奖励状态约定：

- `Completed`：任务目标已完成，奖励还未进入发放流程。
- `RewardPending`：奖励正在发放或发放失败等待重试。
- `Rewarded`：奖励已成功发放，任务完全闭环。

因此任务模块只负责 `TrySetRewardPending` 和 `TryMarkRewarded` 这类状态收口，不直接调用背包、成长或自定义奖励处理器。

## 场景挂载与 Inspector 配置
### NiumaQuestController
建议挂载位置：`CoreScene/BootstrapRoot/GameplayServicesRoot/QuestRoot`。

用途：管理任务配置、接取、阶段推进、目标计数、任务状态和任务快照。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Quest Assets` | 拖所有任务配置资产 | 不建议 | 没有任务可接取或推进 |
| `Register Service To Context` | 核心场景开启 | 可以关闭 | Gal、Story、Reward 等模块无法通过 GameContext 获取任务服务 |
| `Publish To Event Bus` | 需要事件总线时开启 | 可以 | 关闭后只靠数据轮询，不发任务事件 |
| `Log Warnings` | 建议开启 | 可以 | 配置和迁移问题不提示 |

### NiumaQuestSaveAdapter
建议挂载位置：`CoreScene/BootstrapRoot/SaveRoot/SaveAdapters`。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Quest Controller` | 拖 `NiumaQuestController` | 不建议 | 任务进度不存档 |
| `Save Controller` | 拖 `NiumaSaveController` | 不建议 | 无法注册存档 Provider |

### QuestUIViewBridge
建议挂载位置：任务追踪面板 UI 物体。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Quest Controller` | 拖 `NiumaQuestController` | 不建议 | UI 不刷新 |
| `Receiver Provider` | 正式场景拖 `QuestToolkitReceiver`；只有历史自定义 UI 才拖自制 `IQuestUIReceiver` | 不可以 | ViewData 无处显示 |
| `Tracked Quest Id` | 默认追踪任务 ID | 可以 | 留空时由服务当前追踪状态决定 |

### QuestToolkitReceiver
建议挂载位置：`CoreScene/BootstrapRoot/UIRoot/UIBridges/QuestToolkitReceiver`。

用途：UI Toolkit 任务追踪接收器。它接收 `QuestUIViewBridge` 输出的 `QuestUIUpdate`，再把数据转发给 NiumaUI 的 Toolkit View。

`UIToolkitViewRegistrySO` 中建议注册：

| ViewId | LayerId | BindingProviderId | InputPolicy | InputBlockMode | 说明 |
| --- | --- | --- | --- | --- | --- |
| `QuestTracker` | `HUD` | `QuestTracker` | `None` | `Menu` | 当前追踪任务面板 |

`QuestToolkitReceiver` 字段：

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `UI Manager` | 拖 `UIRoot/UIManager` 上的 `UIToolkitUIManager` | 不建议 | 会尝试自动查找；找不到时任务追踪不刷新 |
| `Quest View Id` | 默认 `QuestTracker`，要与注册表 ViewId 一致 | 不建议 | ViewId 不匹配时窗口打不开 |
| `Auto Open View` | 建议开启 | 可以 | 关闭后需要其他脚本先打开 `QuestTracker` |
| `Close On Cleared` | 建议开启 | 可以 | 没有追踪任务时窗口可能留在屏幕上 |
| `Log Warnings` | 建议开启 | 可以 | ViewId 或 UIManager 配错时不提示 |

绑定步骤：

1. 在核心场景创建 `UIRoot/UIBridges/QuestToolkitReceiver`。
2. 挂 `QuestToolkitReceiver`。
3. 把 `UIRoot/UIManager` 上的 `UIToolkitUIManager` 拖到 `UI Manager`。
4. 选中 `QuestUIViewBridge`，把 `QuestToolkitReceiver` 拖到 `Receiver Provider`。
5. 确认 `UIToolkitViewRegistrySO` 中存在 `QuestTracker` 条目。

### QuestDialogueBridge / QuestInteractionBridge
建议挂载位置：`GameplayServicesRoot/QuestRoot/Bridges` 或对应业务入口物体。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| `Quest Controller` | 拖 `NiumaQuestController` | 不建议 | 无法把对话/交互转成任务信号 |
| `Dialogue Controller / Interaction Controller` | 拖对应模块控制器 | 不建议 | 桥接无法读取对应事实 |
| `Log Warnings` | 建议开启 | 可以 | 信号缺失不提示 |



### QuestToolkitBindingProvider
建议挂载位置：CoreScene/BootstrapRoot/UIRoot/UIToolkitRoot/BindingProviders/QuestBindingProvider。

用途：把 QuestToolkitReceiver 推给 QuestTracker 的 QuestUIUpdate 渲染到 UXML。没有它，任务追踪窗口会打开但不显示任务内容。

| 字段 | 怎么填 | 可否留空 | 不填会怎样 |
| --- | --- | --- | --- |
| Provider Id | 默认 QuestTracker，与 Registry 的 BindingProviderId 一致 | 不建议 | 不匹配时回退空 Binding |
| List Root Name | 任务目标列表容器，默认 ListRoot | 可以 | 不显示目标列表 |
| Detail Label Name | 当前任务/阶段描述，默认 DetailText | 可以 | 不显示任务详情 |
| Result Label Name | 配置缺失提示，默认 ResultText | 可以 | 不显示缺失配置提示 |

UXML 至少建议包含：TitleText、StatusText、ListRoot、DetailText、ResultText、EmptyRoot。
