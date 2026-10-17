# 《汪归》Feu Follet

Unity 网格解谜游戏项目。玩家控制小狗 Pomme，在单一地图中移动、推物品并 Bark，引导鬼魂到达相同频道的墓碑。关卡编辑器可在游戏内摆放地图、墙和物品，并保存、加载和测试关卡。

## 项目环境与入口

- Unity `2022.3.47f1c1`。
- 主要编辑场景：`Assets/Scenes/Re/LevelEditorTest.unity`。
- 主要玩法场景：`Assets/Scenes/Re/Gameplay.unity`。
- 主菜单和 Hub 场景位于 `Assets/Scenes/Re/`。
- 输入动作资源：`Assets/Data/InputSystem_Actions.inputactions`。

## 资源目录

- `Assets/_Scripts/` 按功能划分运行时代码；旧 Hub/附身玩法和旧管理器集中在 `Assets/_Scripts/Legacy/`，仅用于兼容旧场景和测试。
- `Assets/Data/` 保存关卡、地块、物品、墙、频道配色和输入配置等项目数据。
- `Assets/Prefabs/` 保存当前项目使用的角色、物品、地图和 UI 预制体。
- `Assets/Scenes/Re/` 保存当前编辑器、Gameplay 和菜单场景；旧关卡和测试场景保留在相应场景目录中。
- `Assets/DownloadedAssets/` 按素材用途分组，组内保留供应商原始目录，见 [素材包目录索引](Assets/DownloadedAssets/README.md)。

Unity 资源移动时必须连同同名 `.meta` 一起移动，以保留 GUID 和场景、预制体中的引用。不要直接删除无法确认用途的素材包；先检查引用并确认对应旧场景不再需要。

旧的 Hub/附身玩法脚本和测试场景仍留在工程中；当前关卡数据与主要编辑、Gameplay 流程使用单一 `MapData` 世界。

## 当前已实现的玩法

### 网格、地块与墙

- 地图逻辑使用二维整数坐标；物品占格数据仍保留三维偏移，编辑器当前将物品放在地面高度，以便将来扩展多格或高度规则。
- 地块类型由 `TileType` 标记，当前包括普通可走 `Walkable`、可摆放 `Placeable` 和幽灵可走 `GhostWalkable` 能力。一个地块可以组合多个标记。
- 地图数据从初始网格开始；在编辑器摆放物品超出当前逻辑范围时会扩展网格，并保留已有内容。
- 墙存储在相邻地块的公共边上。设置或删除一侧墙会规范到同一条边；移动时检查该边，阻止角色和物品穿墙。
- 墙只能放在已摆放的地块边上。墙的视觉 prefab 从 `WallData` 对应的资源配置中取得。

### 角色、鬼魂和目标

- 小狗由方向输入逐格移动；不能穿过不允许共享格子的实体，可推动可推动物品。
- 鬼魂由 `RollingMovementStrategy` 沿一个方向滑行，遇到地图边缘、墙、不可通行地块或阻挡实体时停止。
- Bark 会查找与小狗同行或同列的鬼魂，并规划鬼魂朝小狗方向滑动的终点。一次 Bark 可影响多只鬼魂，移动命令集中计算并记录，供撤销使用。
- 鬼魂根据 `channelID` 与同频道墓碑配对。鬼魂到达配对墓碑后登记完成；测试关卡完成时在屏幕中央显示短暂的 Clear 提示，不跳转关卡。
- 墓碑作为目标标记保存，不占据移动阻挡格；每个频道的鬼魂和墓碑在编辑器中以移动/替换方式保持各一个。

### 可摆放物品与机关

- 木箱支持推箱和按 `ChainPushable` 配置参与连续推物。铁箱是否允许继续链推由物品数据标记决定；组合规则由移动规划检查。
- 木桶支持立起和横躺两种初始摆放方向。立起时被推会倒下；横躺时从端部推动会立起，从侧面推动会沿该方向滚动到受阻位置。模型旋转通过 DOTween，移动和声音仍使用 prefab 上的 Feel/MMFeedback 配置。
- 魂灯是可推动物品。灯所在格周围的 `3×3` 区域允许鬼魂进入；墙仍然阻挡移动。
- 挡路石头和南瓜可使用多个随机款式 prefab，款式选择由 `RandomObjData` 根据物品 ID 和格子坐标稳定决定。
- 压力板本身允许和其他对象共享地格。具备 `CanActivatePressurePlate` 标记的对象停在压力板上时，会激活相同频道的机关；由 `PressureControlled` 标记的南瓜隐藏并播放溶解效果。机关状态在完整移动或 Bark 后刷新，滚动物体只在整次移动结束时按最终位置检查。
- 南瓜和压力板的频道颜色集中保存在 `Assets/Resources/ChannelPalette.asset`；编辑器频道选择按钮也读取该资源，可在 Inspector 中用颜色选择器修改。鬼魂和墓碑沿用 prefab 上按频道配置的 `NPCGhost` 材质，二者同频道使用一致材质。
- `BlocksMovement` 用于阻止两个实体共占一格，`BlocksGhost` 用于阻止鬼魂滑行，`BlocksGoal` 用于阻止墓碑与特定实体重叠；这些能力通过 `ObjectType` flags 配置。
- `occupiedGrids` 支持一个物品占据多个格子，旋转后会重新计算占格位置。

### 编辑器与关卡数据

- 编辑器提供关卡列表和物品列表两个面板，两个面板通过各自 GameObject 的 `SetActive` 切换。
- 从主菜单进入编辑器时显示关卡列表；新建关卡先在内存创建单地图数据，不会因创建动作立即写入文件。保存/另存为按钮写入关卡文件；测试关卡时会先保存当前关卡，再切换至 Gameplay。
- 关卡和物品通过鼠标交互摆放。当前提示包括：中键拖动平移、右键拖动旋转相机、滚轮缩放、左键放置、右键单击移除、双击取消摆放、小键盘 `2` 旋转物品。
- 道具可按 `maxCount` 限制数量；`-1` 表示不限制。唯一物品再次摆放时会移动既有实例。幽灵和墓碑按频道关联。
- `LevelData` 只包含一个 `MapData`，并记录小狗起始格和全局物品数量。
- `LevelDataIO` 通过 Odin Serializer 将 `LevelData` 转为 JSON。编辑器关卡保存在 `Application.streamingAssetsPath`，运行时按名称读取；默认文件名为 `mLD_<关卡名>.json`。
- Gameplay 通过 `LevelLoadBridge` 获取待加载关卡名和“编辑器测试”标记。场景切换由配置的 MMF Player 播放 Load Scene Feedback。

### 操作与反馈

- Gameplay 移动、Bark、撤销等动作由 Input System 的 `Player` Action Map 驱动；默认键位以 `InputSystem_Actions.inputactions` 中绑定为准。
- 移动、Bark、失败提示、木桶声音和多数物件动画使用 More Mountains Feel/MMFeedback；木桶模型翻转使用 DOTween。
- Gameplay 支持等待地图加载后初始化玩家和聚焦相机。编辑器和 Gameplay 相机支持鼠标右键拖动旋转；编辑器另有中键平移和滚轮缩放。
- 正常关卡完成会通过进度/关卡管理器处理解锁与场景跳转；编辑器测试关卡只显示完成提示。
- 工程中还包含公共 `UIPopup` 组件，可配置文本区和两个按钮用于简单确认弹窗。

## 实现结构

| 目录/脚本 | 职责 |
| --- | --- |
| `Assets/_Scripts/Map/MapData.cs` | 地块、墙、物品实例、占格映射、放置与通行规则、频道和机关状态数据 |
| `Assets/_Scripts/Map/MapPlane.cs` | 将地图数据生成到场景，管理地块/墙/物品对象、频道外观和压力机关刷新 |
| `Assets/_Scripts/Map/LevelData.cs` | 单地图关卡容器、起点与物品数量限制 |
| `Assets/_Scripts/Map/Json/` | 关卡及地图数据的 JSON 转换与文件读写 |
| `Assets/_Scripts/Movement/` | 普通推挤、连续推、木桶滚动和滑行移动的通行规划 |
| `Assets/_Scripts/Commands/` | 移动、摆放、删除、Bark 等可执行/撤销命令 |
| `Assets/_Scripts/Obj/` | 狗、鬼魂、可动物件、木桶、频道外观和压力溶解表现 |
| `Assets/_Scripts/Mgr/level/` | 关卡编辑器、摆放状态、保存/加载/测试流程 |
| `Assets/_Scripts/Mgr/GameMgr.cs` | Gameplay 初始化、输入缓冲、移动/Bark/撤销与回合后机关刷新 |
| `Assets/Resources/ChannelPalette.asset` | 南瓜、压力板和编辑器频道按钮共用的频道颜色 |

### 一次移动的主要流程

1. `GameMgr` 接收并缓冲输入，调用当前移动策略检查地图边界、共享边墙、地块和实体占格。
2. 推物时，`SimpleMovementStrategy` 收集可连推链或生成木桶路径；无法满足完整链条时整次推动失败。
3. 通过检查后创建 `MoveObjectCommand` 或组合命令。命令先更新 `MapData` 中的主格、占格和格子映射，再播放视觉反馈。
4. 完整移动结束后刷新压力板状态；压力板按频道隐藏/显示南瓜。撤销使用命令历史回放之前的地图状态。
5. 鬼魂移动和墓碑配对状态更新后，由 `EndingMgr` 检查关卡目标；测试场景显示提示，正式关卡进入进度与场景流程。

## 物品配置方式

物品定义由 `ObjDataList_SO` 管理。每个 `ObjData` 至少配置 `objID`、名称、`ObjectType`、prefab、占据格列表和 `maxCount`。同类多外观物品使用 `RandomObjData.prefabVariants`。物品能否移动、阻挡角色/鬼魂/目标、触发压力板或链推由 `ObjectType` 标记控制；频道实例 ID 存在 `ObjDataInstance.channelID`。

地块由 `TileDataList_SO` 管理，墙外观由 `WallDataList_SO` 管理。放置和移动判定读取数据资源，而不是依赖 Unity Collider，因此修改 prefab 外形时仍需保持对应的数据标记和占格设置正确。

## 目前的实现边界

- 地图是单一世界；旧附身/双世界相关脚本和资源仍在项目内，但不属于当前主要关卡读写流程。
- 关卡文件读写面向当前单地图格式，没有提供旧双地图存档迁移保证。
- 编辑器摆放高度目前固定为地面层；数据保留 3D 占格和旋转表达。
- 鬼魂受魂灯照亮范围影响；压力板目前通过压力板自身频道控制同频道物件。不同类型机关效果以当前 `MapPlane.RefreshPressureMechanisms` 实现为准。
- 编辑器未在创建空关卡时自动写文件；测试入口会调用保存流程以保证 Gameplay 读取到当前关卡。

## 常用检查

1. 在 Unity 中打开 `Assets/Scenes/Re/LevelEditorTest.unity`。
2. 新建或加载关卡，摆放地块、墙和物品；检查频道物件颜色及频道配对。
3. 点击保存，再点击测试关卡验证加载、移动、机关、Bark、撤销与完成提示。
4. 修改 Object/Tile/Wall 数据后，重新进入 Gameplay 验证数据判定与 prefab 展示一致。

当前仓库没有针对这些 Unity 场景逻辑配置自动化测试；最终验证需要在 Unity Editor 的 Play Mode 中进行。
