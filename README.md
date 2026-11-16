# LOL Tower 符文远征录

基于 Unity 的竖屏单人冒险原型，结合分支地图探索、英雄队伍构筑、全队道具与回合制自动战斗。

## 打开工程

使用 Unity Hub 添加仓库根目录，编辑器版本为 **Unity 2022.3.62f3c1**。首次打开需要等待 Package Manager 还原依赖与资源导入。

打开 `Assets/ElementalTactics/Scenes/StartScene.unity` 后进入 Play Mode。后续场景为主城、地图与关卡；构建场景列表见 `ProjectSettings/EditorBuildSettings.asset`。

`Packages/manifest.json` 包含 Unity MCP 的 Git 依赖，首次还原需要网络和 Git。

## 项目内容

- `Assets/`：场景、游戏代码、编辑器工具、配置及美术资源；资源的 `.meta` 文件一并提交。
- `Packages/`：包依赖清单与锁定版本。
- `ProjectSettings/`：Unity 工程设置。
- `docx/`：策划案、数值机制手册和素材截图。
- `Assets/ElementalTactics/README.md`：游戏原型说明。
- `Assets/ElementalTactics/FILE_STRUCTURE.md`：代码与资源目录说明。

当前策划文档为 `docx/策划案_v1_前六章完整版.docx`。数值手册与配置不是自动双向同步，修改后需通过工程的配置导入流程生效。

地图池包含圆形回廊、方形城塞、三角秘阵和双湾迷阵；每个基础楼层随机使用一种，同层所有入口至 Boss 的移动步数一致。每局初始3点冒险生命，战败立即扣1点，重新布阵和重试不额外扣命。

`Library`、`Temp`、`Logs`、`UserSettings`、IDE自动生成文件及 `GAME` 构建导出目录不进入版本库，Unity 会按需重新生成缓存。
