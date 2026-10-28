# 开始使用

## 安装并运行示例

在 Unity 2022.3、6.0 或 6.3 项目的 Package Manager 中，通过 Git URL 安装：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

示例使用 URP、uGUI 和 Input System。先将 **Player Settings > Active Input Handling** 设为 **Input System Package (New)** 或 **Both**，等待编译完成。

在包的 Samples 中导入 **00 · PointPointPoint**，打开 `GameMain/Scenes/PointPointPoint.unity` 并运行。点击目标得分，点击空白扣分，结算保存最高分。其他示例见[运行示例](06-module-examples.md)。

示例默认使用 YooAsset Editor Simulation，普通 Play 无需构建资源包，也无需点击 Fix 或 Save。修改收集规则后需要 Save；路径失效时才使用 Fix。构建应用见[资源工作流](Modules/resource-build.md)。

## 创建自己的启动流程

安装后，编辑器会为尚未选择 Profile 的新项目准备 `Assets/UnityQGameFramework/Config`。其中包含 `LubanProfile.asset`、`luban.conf` 和 `Source` 下的初始 Excel。不会自动下载编译器；已有全局 Profile 时沿用原选择。

1. 在 `QGF > Settings` 选择默认 Luban Profile，执行 `QGF > Generate Tables`。首次生成按需下载固定版本编译器，需要可用的 .NET 8 或兼容运行时。
2. 新建启动场景，将包内 `Runtime/Prefabs/UnityQGameFramework.prefab` 拖入场景。
3. 将生成的 `Config/Data/Catalog.asset` 赋给 DataTable 节点的 **Luban Catalog**。
4. 创建下面的流程脚本。编译后在 Procedure Inspector 勾选它，并设为 **Entrance Procedure**。

```csharp
using UnityQGameFramework.Config;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace MyGame
{
    /// <summary>
    /// 加载游戏数据表并查询默认道具。
    /// </summary>
    public sealed class GameStartup : ProcedureBase
    {
        /// <summary>
        /// 进入启动流程。
        /// </summary>
        /// <param name="owner">流程状态机。</param>
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            // 组件已初始化；从 Inspector 的 Catalog 同步加载整组表。
            if (!QGF.DataTable.LoadDataTables<Tables>())
            {
                return;
            }

            var item = QGF.DataTable.GetDataTable<TbItem>().GetDataRow(1);
            Log.Info(item == null ? "Item 1 is missing." : item.Name);
        }
    }
}
```

保存场景并运行。默认 Item 表包含一条 Id 为 1 的记录。`Tables` 是 Luban 生成的加载器，`TbItem` 是生成表类型；业务通过同一个 `QGF.DataTable` 入口访问它们。

## 文件放在哪里

| 位置 | 内容 |
| --- | --- |
| Packages/UnityQGameFramework | 框架、编辑器工具、第三方运行库、生成模板 |
| Assets/自己的游戏目录 | 场景、脚本、UI、模型、音效 |
| Assets/UnityQGameFramework/Config | 自己的 Excel、生成 Profile、C#、bytes 和 Catalog |
| Assets/Samples/UnityQGameFramework | Package Manager 导入的可编辑示例 |
| Library/UnityQGameFramework | 本机编译器缓存与生成临时文件 |

自己的文件提交到 Git；Library 不提交。移动配置目录时，在 Unity Project 窗口移动整个文件夹并保留 `.meta`，随后检查 Profile 的源表与输出路径。具体见 [Luban](02-data-tables.md)。

Git 安装的包缓存由 UPM 管理，修改框架请使用 fork 或完整仓库。克隆仓库后先安装 Git LFS 并执行 `git lfs pull`；开发工程直接运行 `Assets/Examples`，无需再次导入 Sample。

自建 asmdef 引用 `UnityQGameFramework.Core`、`UnityQGameFramework.Runtime`；使用生成表时还需 `UnityQGameFramework.Luban`，异步代码添加 `UniTask`。三个 Sample 各有一个程序集，可以独立导入、移动和删除。

更新 Git 包需要提交、推送后在 Package Manager 更新 Git revision；也可在 Git URL 末尾使用 `#tag` 锁定版本。卸载 Sample 时移除其导入目录；移除框架包不会删除你的游戏配置和脚本。
