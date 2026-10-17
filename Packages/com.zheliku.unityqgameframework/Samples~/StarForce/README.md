# StarForce

StarForce 展示 QGF 的流程、实体、UI、事件、对象池、音效与 Luban 数据表。

打开本目录的 `StarForce Launcher.unity`，点击 Play，进入菜单后点击开始。在 Game 视图按住鼠标左键移动并开火。请从 Launcher 启动，它负责初始化框架与预加载。

示例不修改宿主的 Layer、输入后端、渲染管线或构建场景列表。材质统一使用 URP 内置 Shader，请在已启用 URP 的工程中导入。UI 输入同时支持旧 Input Manager 和 Input System。编辑器试玩不需要资源打包，也不需要先生成表格。

要编辑数据，使用本目录 `UnityQGameFramework/Config/Editor/LubanProfile.asset`，在 `QGF > Settings` 中选为全局 Profile，执行 `QGF > Generate Tables`。表源在 `Config/Source/Datas`，手写扩展放 `Config/Extensions`。游戏参数、发行信息、资源工具配置与多语言字典统一使用 TOML。已有 Launcher 的 Catalog 引用保持不变。

从 Package Manager 导入的示例可直接编辑。删除整个 StarForce 文件夹即可移除示例；也可使用 `QGF > Uninstall > StarForce`，它先备份到项目根目录 `QgfBackup~`。

完整用法见框架包 `Documentation~` 的两篇教程。Player 发行仍需配置启动场景与资源构建，不由 Sample 导入代为修改项目。

源码与资源来源、第三方库见 [THIRD-PARTY.md](THIRD-PARTY.md)，原 MIT 许可见 [LICENSE.md](LICENSE.md)。
