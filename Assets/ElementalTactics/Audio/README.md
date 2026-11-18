# 符文远征录 音频素材

共 5 首背景音乐、36 个音效，按当前游戏的奇幻冒险、六人编队、自动战斗和节点奖励流程整理。

双击 `试听.html`，可按类别试听、搜索用途并查看来源。所有素材采用 CC0 1.0 授权，允许商用；作者和授权记录见 `Licenses/README.md`。

## 项目中的播放方式

- 进入任意现有游戏场景时，自动创建跨场景音频管理器，无需手动挂组件。
- 标题、主城、商店播放主城配乐；第一、二层地图和布阵播放探索配乐，第三、四层播放暗影配乐。
- 开战后根据地图节点的首领标记，切换普通战斗或首领配乐；结算时恢复探索配乐，并播放胜负提示。
- 按钮、卡牌选择与布阵、进入节点、购买、事件、领奖、招募和升级均已接入对应音效。战斗按职业、属性和实际伤害、治疗、护盾等结果播放声音。
- 主城的“设置”中可分别调整音乐和音效，拖到 0% 即可静音，音量自动保存在本机。默认音乐 25%、音效 70%，界面短音另有减音。
- 配乐换曲采用约 1.2 秒淡入淡出；暂停战斗时暂停战斗音效、降低配乐，界面音仍可播放。游戏进入后台时暂停声音。
- 音频引用配置位于 `Resources/Audio/GameAudioCatalog.asset`，音量和播放逻辑位于 `Runtime/Audio/`。修改音色时，可在配置资产中替换对应音频引用。
- 编辑器菜单 `Tools → Elemental Tactics → Validate Audio Setup` 可检查音频引用、导入设置和音量滑块，并生成设置页预览。

## 素材说明

- 治疗、护盾和技力音效是通用施法声的建议用途；水、草属性可先共用通用法术音效。
- 配乐保留原文件，未处理成无缝循环；曲目末尾重复时可能保留原素材的停顿。
- 音效已转为 PCM16 WAV，并统一峰值，避免原素材忽大忽小；原始文件的来源和校验值均保留。
- Unity 导入设置：配乐使用 Streaming；短音效使用 Decompress On Load，作为二维声音播放。

## 背景音乐

| 文件 | 用途 | 时长 |
| --- | --- | --- |
| `Music/bgm_home.mp3` | 主城与标题：开始界面、主城、商店，轻松的竖琴与管乐 | 97.45 秒 |
| `Music/bgm_exploration.mp3` | 地图探索：林地、路线选择、普通事件，舒缓的奇幻器乐 | 84.64 秒 |
| `Music/bgm_shadow.ogg` | 暗影区域：暗影岛、隐藏楼层、神秘事件，偏低沉的环境配乐 | 280.00 秒 |
| `Music/bgm_battle.mp3` | 普通战斗：普通敌阵、精英战斗，节奏更紧凑 | 66.04 秒 |
| `Music/bgm_boss.mp3` | 首领战斗：楼层首领、最终战，较强的弦乐与铜管 | 95.85 秒 |

## 界面操作

| 文件 | 用途 | 时长 |
| --- | --- | --- |
| `SFX/UI/ui_click.wav` | 按钮点击：普通按钮 | 0.10 秒 |
| `SFX/UI/ui_back.wav` | 返回：返回上一页 | 0.06 秒 |
| `SFX/UI/ui_panel_open.wav` | 打开面板：角色、背包和设置弹窗 | 0.15 秒 |
| `SFX/UI/ui_panel_close.wav` | 关闭面板：关闭弹窗 | 0.15 秒 |
| `SFX/UI/ui_card_select.wav` | 选中卡牌：选中英雄或奖励 | 0.04 秒 |
| `SFX/UI/ui_card_drop.wav` | 放下卡牌：拖动布阵成功 | 0.11 秒 |
| `SFX/UI/ui_confirm.wav` | 确认操作：确认阵容、领取奖励 | 0.29 秒 |
| `SFX/UI/ui_error.wav` | 操作无效：金币不足、条件不满足 | 0.17 秒 |
| `SFX/UI/ui_toggle.wav` | 切换选项：战斗倍速、设置选项 | 0.50 秒 |

## 地图与奖励

| 文件 | 用途 | 时长 |
| --- | --- | --- |
| `SFX/Adventure/map_step.wav` | 路线前进：进入下一节点 | 0.25 秒 |
| `SFX/Adventure/node_unlock.wav` | 节点解锁：解锁下层或新路线 | 0.26 秒 |
| `SFX/Adventure/shop_purchase.wav` | 购买成功：商店扣款并获得商品 | 0.85 秒 |
| `SFX/Adventure/reward_coins.wav` | 获得金币：金币奖励 | 0.62 秒 |
| `SFX/Adventure/reward_item.wav` | 获得道具：普通道具奖励 | 0.41 秒 |
| `SFX/Adventure/reward_rare.wav` | 稀有奖励：稀有、史诗道具或星界印记 | 0.20 秒 |
| `SFX/Adventure/inventory_open.wav` | 翻开背包：背包或道具详情 | 0.15 秒 |
| `SFX/Adventure/codex_page.wav` | 图鉴翻页：图鉴和剧情文本切换 | 0.77 秒 |

## 战斗与技能

| 文件 | 用途 | 时长 |
| --- | --- | --- |
| `SFX/Combat/attack_blade_01.wav` | 近战攻击一：战士普通攻击 | 0.29 秒 |
| `SFX/Combat/attack_blade_02.wav` | 近战攻击二：近战攻击的另一种声音 | 0.31 秒 |
| `SFX/Combat/sword_draw.wav` | 拔剑开战：开始战斗或战士蓄势 | 0.40 秒 |
| `SFX/Combat/wind_slash.wav` | 挥斩：亚索等角色的挥刀、风斩 | 0.57 秒 |
| `SFX/Combat/hit_metal.wav` | 金属受击：盾牌格挡、重甲受击 | 0.58 秒 |
| `SFX/Combat/enemy_hurt.wav` | 怪物受击：敌方怪物受伤 | 0.62 秒 |
| `SFX/Combat/enemy_die.wav` | 怪物倒下：敌方怪物阵亡 | 1.06 秒 |
| `SFX/Combat/boss_roar.wav` | 首领咆哮：沃里克等怪物首领登场 | 0.64 秒 |
| `SFX/Combat/arcane_cast.wav` | 施放法术：法师出手、能量释放 | 0.63 秒 |
| `SFX/Combat/arcane_hit.wav` | 法术命中：法术攻击命中 | 0.55 秒 |
| `SFX/Combat/fire_cast.wav` | 火焰施法：火属性技能开始 | 1.06 秒 |
| `SFX/Combat/fire_hit.wav` | 火焰命中：火属性技能击中目标 | 1.25 秒 |
| `SFX/Combat/heal_magic.wav` | 治疗法术：盾卫治疗、行动回复的候选音效 | 1.38 秒 |
| `SFX/Combat/shield_magic.wav` | 护盾法术：开战护盾、技能护盾的候选音效 | 1.82 秒 |
| `SFX/Combat/energy_magic.wav` | 技力法术：技力蓄满、星界能量的候选音效 | 1.11 秒 |

## 结算与成长

| 文件 | 用途 | 时长 |
| --- | --- | --- |
| `SFX/Results/battle_victory.wav` | 战斗胜利：战斗获胜，上行拨弦短音 | 0.80 秒 |
| `SFX/Results/battle_defeat.wav` | 战斗失败：失败、冒险生命耗尽，下行拨弦短音 | 0.79 秒 |
| `SFX/Results/adventure_level_up.wav` | 冒险升级：升级、通关楼层 | 0.96 秒 |
| `SFX/Results/hero_recruit.wav` | 招募英雄：招募成功、结局解锁 | 0.46 秒 |
