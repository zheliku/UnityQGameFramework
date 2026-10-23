# 模块示例

Package Manager 中 `01_Base`–`21_Debugger` 各自是一个 Sample。按需导入模块，示例场景和它用到的 QGF prefab、UI 资源会一起复制。移动或卸载时操作整个 Sample 目录。

每个模块目录里的编号子目录对应一个场景。运行一个场景即可；调用说明写在同名脚本中。WebRequest、Download 和 Network 使用随场景启动的本机测试服务。

3D 和 UI 模块在宽屏左右排列，窄屏上下排列；操作区带滚动条。`Common/ExampleSceneLayout.cs` 负责画面布局和镜头范围。

接口说明见 [模块文档](Modules/README.md)。游戏流程示例见 [00_PointPointPoint](../Samples~/00_PointPointPoint/README.md)，完整项目见 [StarForce](05-starforce.md)。
