# 代码文件分类

`Runtime` 下按游戏功能分组，`Editor` 下按编辑器职责分组。分类只改变文件路径，不改变类名、命名空间和程序集边界。

| 目录 | 内容 |
| --- | --- |
| `Runtime/Core/` | 场景导航、局内共享状态 |
| `Runtime/Combat/` | 战斗规则、战力估值、回合控制、战斗 HUD |
| `Runtime/Formation/` | 六格布阵、角色拖动、站位状态 |
| `Runtime/Map/` | 路线生成、节点交互、地图图标 |
| `Runtime/Characters/` | 已拥有角色列表 |
| `Runtime/Inventory/` | 背包状态、背包界面、道具加成 |
| `Runtime/Progression/` | 冒险等级、经验、稀有度抽取 |
| `Runtime/Rewards/` | 胜利奖励生成与领取 |
| `Runtime/Shop/` | 商店库存、购买、刷新与界面 |
| `Runtime/Endings/` | 结局与额外楼层逻辑；目前仅源码准备版本存在 |
| `Runtime/Data/` | 配置记录类型、数据访问入口 |
| `Runtime/Data/Databases/` | 各类 ScriptableObject 数据库定义 |
| `Runtime/UI/Common/` | 公共 UGUI 构建、卡片、职业图标、安全区布局 |
| `Runtime/UI/Art/` | 开始界面与冒险背景的程序绘制 |
| `Runtime/UI/Encounters/` | 事件、商店与战利品弹层入口 |
| `Editor/Import/` | Excel 配置导入器 |
| `Editor/Scenes/` | 场景生成工具 |
| `Editor/Validation/` | 战斗规则验证工具 |

`Data/` 保留工作簿与配置说明；`Resources/`、`Fonts/` 保留资源原路径。实际 Unity 工程的 `Scenes/` 与 `Resources/Data/` 也维持原位置，确保场景入口和 Resources.Load 路径不变。

当前 Unity 工程不存在 `EndingRun`、`EndingDatabaseSO`，本次分类没有把它们部署进去。新增代码应放入对应功能目录；Unity 内移动脚本必须同时保留 `.meta` GUID。
