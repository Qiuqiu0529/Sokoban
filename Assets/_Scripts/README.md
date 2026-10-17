# 脚本目录索引

当前逻辑按职责归类；旧 Hub/附身版本代码集中在 `Legacy/`，保留供旧场景和测试场景引用。

| 目录 | 职责 |
| --- | --- |
| `Map/` | `LevelData`、`MapData`、地块/墙/物品数据与地图生成；`Map/Json/` 负责关卡 JSON 转换和读写 |
| `Mgr/` | Gameplay、地图、进度、相机和结算管理；`Mgr/level/` 是关卡编辑器与摆放流程 |
| `Movement/` | 角色、推箱、连锁推动、木桶滚动与鬼魂移动规则 |
| `Commands/` | 可执行和撤销的摆放、移动、Bark 等操作 |
| `Obj/` | 小狗、鬼魂、木桶、压力板/南瓜表现与频道外观 |
| `UI/` | 关卡、物品、地块、墙和弹窗 UI |
| `Common/` | 通用枚举、单例和共享工具 |
| `EventSystem/` | 项目内的事件/观察者实现 |
| `LevelProgress/` | 关卡解锁条件和玩家进度数据 |
| `Re/` | 当前菜单与场景流程脚本 |
| `Test/` | 开发测试与旧 Hub 测试脚本 |
| `Util/` | 相机等通用组件 |
| `Legacy/HubGameplay/` | 旧 Hub/附身玩法，包括旧的 Gameplay Elements 和 GhostScript |
| `Legacy/Managers/` | 旧玩家控制、附身、Realm 切换和 Undo 管理器 |

## 维护约定

- 移动挂在场景或预制体上的脚本时，必须连同 `.meta` 移动，以保留 `MonoBehaviour` GUID。
- 新的玩法逻辑优先放入对应职责目录，不再添加到 `_Scripts` 根目录。
- `Test/` 与 `Legacy/` 中的代码仍参与编译；它们的目录分类不代表可以删除。删除前应先检查旧场景和预制体的脚本引用。
