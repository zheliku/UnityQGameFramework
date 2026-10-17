# Unity Q Game Framework

面向 Unity 2022.3 LTS 及 Unity 6 的模块化游戏框架，基础维护版 `0.1.0-exp.2`。

- `Runtime/Core`：纯 C# 模块、接口、引用池、任务池和工具，不引用 UnityEngine。
- `Runtime/Unity`：Unity 组件、默认辅助器与运行时调试器。
- `Runtime/Prefabs/UnityQGameFramework.prefab`：框架入口，原有资源 GUID 保留。
- `Editor`：类型发现、Inspector、配置、资源管理与打包工具。
- `Runtime/Luban`：隔离命名空间的官方最小二进制运行时。
- `Runtime/Plugins`：压缩与 TOML 依赖，许可见第三方声明。

程序集分别为 `UnityQGameFramework.Core`、`UnityQGameFramework.Runtime`、`UnityQGameFramework.Luban`、`UnityQGameFramework.Editor`。自定义游戏流程和辅助器可放入自己的 asmdef；运行时程序集需引用 Core/Runtime，Luban 生成代码还需引用 Luban，编辑器工具需引用 Editor。

安装地址：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

框架不强制安装 URP、Input System、Unity CLI 或开发者的编辑器插件。StarForce 是可选的 URP 示例：本仓库的开发源码放在 `Assets/Examples/StarForce`，发行工具生成 `Samples~/StarForce` 后由消费者导入到 Assets。

DataTable 已替换为 Luban 后端，保留常用 QGF 查询接口，移除旧 TXT 表生成／解析链路。使用 `Unity Q Game Framework > Generate Data Tables` 一键生成，表源和业务生成代码放在 Assets；见 [Luban 使用说明](Documentation~/luban.md)。QFramework 架构融合尚未开始。

安装、首次生成、发布检查及实际验证范围见 [生产前检查记录](Documentation~/production-readiness.md)。Git URL 直接安装包源码，无需在 Git 中另存 `.tgz`；本地修改只有提交并推送后才会出现在远端安装中。

请阅读 [基础 API](Documentation~/api.md)、[架构](Documentation~/architecture.md)、[兼容性](Documentation~/compatibility.md)、[迁移](Documentation~/migration.md)、[目录与发布](Documentation~/repository-layout.md) 和 [第三方声明](Third%20Party%20Notices.md)。来源追溯单独保留于 [upstream.md](Documentation~/upstream.md)。
