# UnityQGameFramework

QGF 基于 UnityGameFramework，使用 Luban 制作数据表、Tomlyn 读取 TOML、YooAsset 管理资源、UniTask 处理异步。支持目标为 Unity 2022.3、6.0、6.3，示例使用 uGUI、Input System 和 URP。

## 安装

在 Package Manager 选择 **Install package from git URL**：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

默认内置 UniTask 和 YooAsset，请勿重复安装。游戏脚本、Excel 和资源放在 `Assets`；框架放在 `Packages`。

## 示例与文档

从 Package Manager 导入一个 Sample，打开启动场景并运行。宿主需启用 Input System；示例不会替你修改项目设置。编辑器模拟模式无需预先打包资源或点击 YooAsset 的 Fix。

| Sample | 内容 |
| --- | --- |
| 00 · PointPointPoint | 用流程、UI、事件和设置完成一个小游戏 |
| 01 · Modules | 按模块查看核心调用和运行结果 |
| 02 · StarForce | 飞机、武器、数据表、资源和界面的协作 |

[开始使用](Packages/com.zheliku.unityqgameframework/Documentation~/01-getting-started.md) · [模块用法](Packages/com.zheliku.unityqgameframework/Documentation~/Modules/README.md) · [Luban](Packages/com.zheliku.unityqgameframework/Documentation~/02-data-tables.md) · [资源构建](Packages/com.zheliku.unityqgameframework/Documentation~/Modules/resource-build.md) · [更新记录](Packages/com.zheliku.unityqgameframework/CHANGELOG.md)

基于 [UnityGameFramework](https://github.com/EllanJiang/UnityGameFramework) 和 [GameFramework](https://github.com/EllanJiang/GameFramework)，示例与架构设计参考 [QFramework](https://github.com/liangxiegame/QFramework)。目前尚未移植 QFramework 的 Architecture、Model、System。

源码采用 [MIT](Packages/com.zheliku.unityqgameframework/LICENSE.md)，第三方内容见 [许可声明](<Packages/com.zheliku.unityqgameframework/Third%20Party%20Notices.md>)。

## 参与开发

克隆仓库并执行 `git lfs pull`，在 Unity 中打开根目录。框架源码位于 `Packages/com.zheliku.unityqgameframework`，示例只在 `Assets/Examples` 编辑。

```sh
python Tools/Style/format_code.py
dotnet run --project Tools/Style/CodeStyleAudit.csproj -- .
python Tools/Packaging/sync_samples.py
python Tools/Packaging/check_package.py
python Tools/Documentation/check_docs.py
```

`python Tools/Packaging/prepare_upm.py` 生成 tgz；可用 `--external-dependency unitask` 或 `--external-dependency yooasset` 排除对应内置插件，再由消费项目安装官方依赖。

查看原版 UGF 与当前框架的逐文件差异：运行 `python Tools/Review/compare_ugf.py --refresh`，在 VS Code 打开 `Artifacts~/UgfComparison/UGF-QGF.code-workspace`。对应文件使用当前路径，移动、改名仍可直接比较；基线保留上游原文，主仓库索引不变。
