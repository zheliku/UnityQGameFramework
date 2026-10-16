# UnityQGameFramework

面向 Unity 6 的游戏框架：以 UnityGameFramework / GameFramework 为基础，逐步融合 QFramework 的 Architecture、Model、System、Utility 与 BindableProperty，并以 Luban 建立配置表工作流。

当前为 **0.1.0-exp.1 开发版本**。框架与 StarForce 示例已通过 Unity **6000.3.24f1** 编译，已修复资源打包废弃选项和 Debugger 静态初始化错误。完整运行、Player 构建、架构融合与 Luban 接入仍待验证或实现；包清单中的 `6000.0` 是目标最低版本，不代表已通过该版本测试。

## 仓库布局

仓库根直接是可打开的 Unity 工程。框架以一个 embedded UPM package 开发，宿主工程与发布包共享同一份源码。

```text
UnityQGameFramework/
├── Assets/
│   ├── Examples/StarForce/              可直接调试的示例源码
│   └── ...                             保留现有测试宿主内容
├── Packages/
│   ├── manifest.json                    宿主依赖
│   ├── packages-lock.json               Unity 生成的依赖锁定
│   └── com.zheliku.unityqgameframework/  框架包，唯一发布源码
│       ├── package.json
│       ├── Runtime/
│       │   ├── GameFramework/           纯 C# 核心源码，替代原 GameFramework.dll
│       │   ├── UnityGameFramework/      Unity 运行时组件与辅助器
│       │   ├── Plugins/                 上游 SharpZipLib 二进制依赖
│       │   ├── Prefabs/                 上游 GameFramework.prefab
│       │   └── link.xml
│       ├── Editor/                      上游编辑器工具
│       ├── Documentation~/              布局规划、来源记录、许可证和上游说明
│       ├── README.md
│       ├── CHANGELOG.md
│       ├── LICENSE.md
│       └── Third Party Notices.md
├── ProjectSettings/                     宿主设置
└── Tools/Packaging/prepare_upm.py        生成包含 Samples~ 的发布包
```

下一阶段的目录、程序集依赖、Luban 数据边界及发布方式见 [目录与发布规划](Packages/com.zheliku.unityqgameframework/Documentation~/repository-layout.md)。

## 本地查看

1. 用 Unity Hub 打开仓库根目录 `UnityQGameFramework`。
2. 等待 Package Manager 导入 `UnityQGameFramework` embedded package。
3. 查看 Console；当前框架与示例编译错误为 0，仍有旧 API 和上游模型导入警告。

框架预制体位于 `Packages/com.zheliku.unityqgameframework/Runtime/Prefabs/GameFramework.prefab`。官方示例入口为 `Assets/Examples/StarForce/StarForce Launcher.unity`；初始接入状态、后续适配及发布方式见 [示例说明](Assets/Examples/StarForce/README.md)。

本工程已安装官方 Unity CLI 与 `com.unity.pipeline`。当前核验的最新版 CLI 为 `1.0.0-beta.13`，可使用：

```powershell
unity status --format json
unity recompile --project-path . --format json
unity command console --project-path . --level error --tail 20 --format json
```

## Git URL 安装

下面的安装地址在本轮修改推送到 GitHub 后生效。在另一个 Unity 6 工程中打开 **Window → Package Management → Package Manager → + → Install package from git URL**，输入：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

开发本仓库时直接使用 embedded package，无需在本工程的 `manifest.json` 中再次添加自己的 Git 依赖。外部工程也可以在其 `Packages/manifest.json` 的 `dependencies` 中添加：

```json
"com.zheliku.unityqgameframework": "https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework"
```

正式发布后推荐在 URL 末尾加 `#版本标签` 锁定版本；目前尚未创建发布标签。

## 发布方式

- **UPM Git URL**：安装 `Packages/com.zheliku.unityqgameframework` 子目录；宿主的 URP、Alchemy、Rainbow Folders 等依赖不属于框架包。
- **unitypackage**：后续发布工具把同一包内容暂存到独立导出工程的 `Assets/UnityQGameFramework/`，保留全部 GUID 后导出；业务场景、宿主设置与缓存不进入插件。
- **GitHub**：源码、来源记录与许可证一并维护；发行产物放 GitHub Releases。本轮未提交、推送或发布。

StarForce 日常开发只编辑 `Assets/Examples/StarForce`。运行 `python Tools/Packaging/prepare_upm.py` 会在被忽略的 `Artifacts~/` 生成含 `Samples~/StarForce` 的 `.tgz` 及发布树，并登记样例导入入口。开发分支的 Git URL 目前提供框架；包含示例的发行标签需要基于该生成发布树创建，路径保持一致。当前未创建标签、远程发布或生成 unitypackage。

## 上游与参考

- [UnityGameFramework](https://github.com/EllanJiang/UnityGameFramework)：Unity 组件与编辑器工具；已导入。
- [GameFramework](https://github.com/EllanJiang/GameFramework)：纯 C# 核心；已导入源码。
- [QFramework](https://github.com/liangxiegame/QFramework)：架构与可绑定属性设计参考；本轮未导入。
- [GodotGameFramework](https://github.com/nuoyanruoshui/GodotGameFramework)：Luban 与工具工作流参考；本轮未导入。
- 本地 `P:\Godot-Project\GameZMXY`：只读参考已有实现；不复制游戏内容、生成数据或项目专属规范。

完整提交号、导入映射与第三方依赖见包内 [来源记录](Packages/com.zheliku.unityqgameframework/Documentation~/upstream.md) 和 [第三方声明](Packages/com.zheliku.unityqgameframework/Third%20Party%20Notices.md)。
