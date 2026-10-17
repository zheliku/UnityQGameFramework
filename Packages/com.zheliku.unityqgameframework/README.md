# UnityQGameFramework

面向 Unity 2022.3 与 Unity 6 的游戏框架，提供纯 C# 模块、Unity 组件、编辑器资源工具和 StarForce 示例。配置表使用 Luban，游戏全局参数使用 TOML。

## 安装

在 Unity Package Manager 中选择 **Install package from git URL**：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

安装完成后，自己的配置工作区在 `Assets/UnityQGameFramework/Config`。在已启用 URP 的工程中，可从 Package Manager 的 Samples 面板导入 StarForce，打开其中的 `StarForce Launcher.unity` 即可在编辑器试玩。

`QGF > Extensions` 提供 Alchemy 与 Rainbow Folders 2 的可选安装入口，按需加入当前项目。框架本身不依赖这两个工具。

## 教程

- [安装与第一个流程](Documentation~/01-getting-started.md)
- [用 Luban 制作与读取配置表](Documentation~/02-data-tables.md)
- [版本记录](CHANGELOG.md)

框架开发仓库的示例源码位于 `Assets/Examples/StarForce`，包内 `Samples~/StarForce` 是它的发布副本。修改示例后运行 `python Tools/Packaging/sync_samples.py`；`--check` 检查副本是否一致。`python Tools/Packaging/prepare_upm.py` 将同一份包内容打成 `.tgz`，输出到 `Artifacts~`。

当前基于 UGF 维护并接入 Luban，QFramework 的 Architecture、Model、System 尚未迁入。框架采用 MIT 许可，原始版权与依赖许可见 [第三方声明](Third%20Party%20Notices.md)。
