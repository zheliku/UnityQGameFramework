# QGF 代码约定

- 框架参考 UGF 的命名、接口、生命周期和源码组织；保留原版权声明。UGF 基础类型保留 `GameFramework` 前缀；Core 基础类型与模块接口使用 `UnityQGameFramework.Core`，模块及 Helper 通知使用 `UnityQGameFramework.Core.Events`，Runtime 游戏事件保留原名。业务代码不为这些事件添加类型别名。示例参考 QFramework，直接展示核心调用、可见结果和必要的生命周期，统一称为“示例”。
- 自有 C# 遵循 `.editorconfig`：四空格、Allman 大括号；成员和访问器不用表达式主体，简单属性可用自动属性。代码标识、界面和运行消息使用英文，注释使用中文。
- 类型和公开成员使用多行中文 XML 注释。框架内部方法、内部类型的公开成员和显式接口实现同样检查；按 `summary`、`typeparam`、`param`、`returns` 补齐实际签名。说明准确表达参数、返回值、空值和集合所有权；void 和构造函数不写 returns，相邻成员留空行。
- 注释只解释关键用法、所有权和异步时序，不复述代码。避免无意义封装、重复配置、冗余文档、空目录和单文件目录。
- 不修改 YooAsset、UniTask、Luban 等第三方官方源码，不覆写其配置资产 Inspector。适配放在 QGF 自有代码，优先使用官方接口，版本差异集中处理。
- 资源收集与构建使用 YooAsset 原生窗口。运行设置只保存在 `ResourceSettings`，组件仅引用它；每包只有一个 Play Mode，编辑器与 Player 共用。不另建配置 SO、环境覆盖或全局模拟开关。
- 示例的必要编辑操作放在组件 Inspector，不新建示例窗口或打包包装；验证工具放在 `Tools`。示例在 `Assets/Examples` 修改，再同步到包内 `Samples~`。
- QGF Log 只格式化自己的调用，不接管 Unity 全局日志，源码链接指向业务调用位置。
- 修改后运行 `python Tools/Style/format_code.py`，再运行 `dotnet run --project Tools/Style/CodeStyleAudit.csproj -- .`。格式化覆盖全项目自有代码，排除第三方和生成代码；人工复核文档含义，不能只看标签齐全。发布前同步并检查 Samples、文档链接和包内容。
