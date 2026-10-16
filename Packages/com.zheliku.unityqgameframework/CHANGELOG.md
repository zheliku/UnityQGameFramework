# Changelog

## Unreleased

- 替换 Unity 6 弃用 API：刷新率改用 `refreshRateRatio.value`，对象查找改用 `FindFirstObjectByType` / `FindObjectsByType`，脚本宏定义读写改用 `NamedBuildTarget` API；保留原有查找行为与宏定义工具接口。
- 安卓文件流改用 `AndroidJNI.NewSByteArray` / `FromSByteArray`，使用 `Buffer.BlockCopy` 保留原始字节内容，并移除对应的弃用警告屏蔽。
- 修复 Unity 6 对 `BuildAssetBundleOptions.DeterministicAssetBundle` 的编译错误，改用默认选项。
- 调试器复制功能改用 `GUIUtility.systemCopyBuffer`，避免静态创建 TextEditor 导致初始化异常。
- 声音代理只在存在 AudioClip 时设置播放时间，修复 Unity 6 在初始化与回收声音时的空资源寻址警告。
- 导入官方 StarForce `d4ce803353f89852c4d58df65b2482a3b08eb11f` 到宿主 `Assets/Examples/StarForce`，调整资源与编辑器工具路径。
- 新增发行暂存工具，从开发示例生成 `Samples~/StarForce`、样例元数据与 UPM `.tgz`，不维护第二份手工编辑的示例源码。
- StarForce 保留原版鼠标玩法，仅增加 Input System 鼠标 API 兼容分支，并忽略已回收实体的碰撞回调；Layer 配置由宿主提供，不自动修改项目设置。
- 将 StarForce 的 21 个材质转换为 URP Lit、Unlit 和 Particles/Unlit，保留贴图及 GUID。
- 通过官方 Unity CLI 验证 Unity 6000.3.24f1 框架与示例编译；完整游戏回归与 Player 构建尚未验证。

## 0.1.0-exp.1 — 2026-10-16

- 导入 UnityGameFramework `e7eb3ef11393df0ac504204c480cdb839752b05f` 的运行时、编辑器工具与预制体。
- 导入 GameFramework `d0c010b05167c58e92350449d04864a91ca13fd2` 核心源码，取代上游 GameFramework.dll。
- 建立 embedded UPM package、核心/运行时/编辑器程序集边界，以及 Git URL 与 unitypackage 发布规划。
- 保留上游 C#、预制体和已有 GUID，记录第三方来源及许可证。
- Unity 6 编译与运行兼容性、QFramework 架构和 Luban 接入留待后续阶段。
