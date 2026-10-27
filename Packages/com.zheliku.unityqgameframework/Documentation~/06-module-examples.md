# 模块示例

Package Manager 提供三个导入项：

| Sample | 内容 |
| --- | --- |
| [00_PointPointPoint](../Samples~/00_PointPointPoint/README.md) | 从启动、菜单到游玩和结算 |
| [01_Modules](../Samples~/01_Modules/README.md) | 各模块的常用操作，每个场景突出一个调用与结果 |
| [02_StarForce](../Samples~/02_StarForce/README.md) | 多模块协作的射击游戏 |

每个场景单独运行。先操作场景，再阅读同名 C# 文件；核心调用旁有必要的注释。WebRequest、Download 和 Network 自带本机测试服务。

模块示例包含一份 `Common/Prefabs/QGF.prefab` 和公共 UI 资源，内部继续按模块编号。移动或卸载时操作整个导入目录。

模块资源示例引用自己的 ResourceSettings 和 YooAsset 原生 BundleCollectorSetting，并通过 YooAsset Editor Simulation 加载。普通示例使用可读地址，句柄示例也使用可读地址。需要 Player 资源构建流程时查看点点点与 StarForce。

仓库的 `Assets/Examples` 是维护入口，同样只有上述三个目录。`Tools/Packaging/sync_samples.py` 将它们原样复制到 `Samples~`，不筛选场景、不改写代码或 GUID。模块内部按功能编号，每个场景只保留必要的调用和说明。

接口说明见 [模块文档](Modules/README.md)。
