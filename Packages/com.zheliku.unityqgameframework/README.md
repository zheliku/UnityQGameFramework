# UnityQGameFramework

面向 Unity 6 的实验性游戏框架。当前版本 `0.1.0-exp.1` 已完成源码导入、包结构整理和首批 Unity 6 错误修复。框架与 StarForce 示例通过 Unity 6000.3.24f1 编译；完整运行、QFramework 架构融合与 Luban 接入仍待验证或实现。

- `Runtime/GameFramework`：GameFramework 核心源码，单独编译且不引用 UnityEngine。
- `Runtime/UnityGameFramework`：上游 Unity 组件，保留原类型与程序集名称。
- `Runtime/Plugins`：保留上游 SharpZipLib 0.86.0.518，见第三方声明。
- `Runtime/Prefabs/GameFramework.prefab`：上游框架预制体，保留 GUID。
- `Editor`：上游编辑器工具，仅在编辑器中编译。

本地仓库开发：用 Unity 6000.3.24f1 打开仓库根，Unity 自动识别此 embedded package。包清单的 Unity 6000.0 是目标最低版本，尚未在该版本验证兼容性。StarForce 的可编辑源码位于仓库的 `Assets/Examples/StarForce`。

本轮变更推送后，外部工程可用以下 UPM Git URL 安装：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

`Tools/Packaging/prepare_upm.py` 从同一份 Assets 示例源码生成带 `Samples~/StarForce` 的发行包及 `.tgz`，发行包的 Package Manager Samples 面板提供导入入口。开发分支 Git URL 目前提供框架；包含示例的发行标签尚未创建。当前尚未执行远程安装验证或生成 unitypackage。同一工程只安装本包或原版 UGF 其中一种，避免共享程序集名称冲突。

详见 [目录与发布规划](Documentation~/repository-layout.md)、[上游导入记录](Documentation~/upstream.md) 和 [第三方声明](Third%20Party%20Notices.md)。
