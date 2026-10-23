# 资源构建：从 Assets 调试走向 Player

编辑器资源模式直接读取 Assets，便于快速调试；Player 没有 AssetDatabase，需要加载构建后的资源和版本清单。二者使用同一套业务资产名，准备过程不同。加载接口见 [Resource 示例](resource.md)。

模块 Sample 中的 02_Package、03_Binary 可以先验证离线资源：执行 `QGF > Examples > Build Resource Fixtures`，运行场景后点击初始化。构建菜单、源资产和二进制示例集中在 `12_Resource`，每个 Unity 版本各自生成 Bundle 与清单。产物可从 Library 删除后重建。

点点点提供 `QGF > Examples > PointPointPoint > Build Windows Player`。它独立收集游戏 TOML 和界面预制体，构建 Package 资源与 Windows64 Player，再把资源复制到 Player 输出中的 `StreamingAssets/QgfExamples/PointPointPoint`。构建不改项目的 StreamingAssets 和场景列表。其 `PointGameLaunch` 等待包内清单初始化后，继续使用同一套资产名加载规则和界面。

StarForce 提供 `QGF > Examples > 22 StarForce > Build Windows Player`。菜单使用示例的 ResourceCollection，在项目根目录 `QgfBuild~/StarForce` 中构建 Windows64 Package，只把 Launcher 编入 Player。资源部署到 Player 的 `StreamingAssets/QgfExamples/StarForce`，Menu 和 Main 场景由资源模块加载。构建结束会恢复 Unity 构建过程补写的项目设置。`QgfBuild~` 是可删除的构建产物，可加入版本控制忽略列表；场景 Bundle 不能输出到 Library。

04_Update 展示 HTTP 更新和分组下载；05_ResourcePack 展示完整包校验与应用；06_VerifyCache 从已有缓存开始，检测损坏并重新下载。它们共用同一份构建结果，操作界面显示实际计数和读取内容。

09_OnDemand 使用 UpdatableWhilePlaying，对照 LoadAsset 触发下载和 LoadBinary 先更新资源组的流程，并演示下载失败后恢复原加载任务。

## 1. 先决定发行模式

| 模式 | 启动准备 | 适合的第一步 |
| --- | --- | --- |
| 编辑器资源模式 | Base 勾选 Editor Resource Mode | 调试业务，先把完整玩法跑通 |
| ResourceMode.Package | 构建包内资源并部署到 StreamingAssets；启动调用 InitResources | 无在线更新的离线包 |
| ResourceMode.Updatable | 获取版本、更新版本清单、检查并更新资源 | 完整下载后进入游戏 |
| ResourceMode.UpdatableWhilePlaying | 准备版本与资源状态，并允许按需更新 | 已设计好分组和加载失败处理的项目 |

ResourceMode 是 Resource 的运行模式；编辑器直读是 Base 选择的调试后端。不要在 EditorResourceMode 下测试 InitResources，然后把结果当作离线资源包验证。

## 2. 资源工具读取哪些配置

菜单都在 `QGF > Resources`：

| 菜单 | 作用 |
| --- | --- |
| Collection | 维护资源名称、资产归属、变体、加载方式、文件系统和资源分组 |
| Analyze | 分析引用关系，检查依赖、重复等情况 |
| Build | 为选定平台构建 AssetBundle、二进制资源和版本数据 |
| Pack | 将选定资源组织成资源包，用于后续 ApplyResources |
| Sync | 按配置同步资源相关内容，执行前检查源目录和目标目录 |

默认配置位于 `Assets/UnityQGameFramework/Configs`，注意它与 Luban 的 `Config` 工作区不同：

| 文件 | 用途 |
| --- | --- |
| ResourceEditor.toml | 源资产根目录、扫描路径和类型/标签过滤 |
| ResourceCollection.toml | 哪些 GUID 属于哪个资源、变体、分组 |
| ResourceBuilder.toml | 平台、压缩、输出目录、资源版本、输出内容和构建事件处理器 |
| BuildSettings.toml | QGF 场景菜单使用的默认场景和搜索路径 |

这些文件由编辑器工具读写，不是运行时 ConfigComponent 的游戏参数。LubanProfile 也不控制这些资源工具。

路径可以通过带 ConfigPath 属性的静态 string 字段或属性提供。StarForce 自己的 `GameMain/Scripts/Editor/FrameworkConfigs.cs` 就使用了这个机制，将配置指向示例目录。

独立构建也可以直接传入两份配置：`new ResourceBuilderController(builderPath, collectionPath)`。只传一个参数时保留原有的全局 Collection 查找规则。Resource 示例使用两参数入口，避免与其他示例的路径提供者混用。

**同一种 ConfigPath 属性只保留一个有效提供者。** 当前实现扫描程序集后返回第一个结果，没有多资源配置 Profile 的选择器。导入 StarForce 后，如果改做自己的资源构建，应修改或移除示例的路径提供者，再接入自己的配置，不能再定义一份同类型提供者并假定后者覆盖前者。

例如自己的项目可以在 Editor 目录放下面的类；不要与 StarForce 的同类属性同时启用：

```csharp
using System.IO;
using UnityEngine;
using UnityQGameFramework.Editor;
using UnityQGameFramework.Editor.ResourceTools;

namespace QgfDocs.Editor
{
    public static class ProjectResourcePaths
    {
        private static string Root => Path.Combine(Application.dataPath, "MyGame", "Configs");

        [ResourceCollectionConfigPath]
        public static string Collection => Path.Combine(Root, "ResourceCollection.toml");

        [ResourceEditorConfigPath]
        public static string Editor => Path.Combine(Root, "ResourceEditor.toml");

        [ResourceBuilderConfigPath]
        public static string Builder => Path.Combine(Root, "ResourceBuilder.toml");
    }
}
```

TOML 中资产根目录等路径按配置文件所在位置解析；移动目录时保留 .meta 与 GUID，检查相对路径，再打开工具确认资产归属。各键格式还可参见 [配置文件说明](../02-data-tables.md)。

## 3. 先构建一个离线包

建议先在单独测试工程或自己的资源目录做以下步骤：

1. 在 Collection 确认要加载的场景、实体/UI 预制体、音频、Config TOML、语言字典与 Luban Catalog/bytes 已进入集合。脚本编译进 Player，不作为普通资源表文本处理。
2. 在 Analyze 检查依赖。注意一个资产被多个资源引用时的归属，避免不必要的重复和过大的公共依赖包。
3. 在 Build 选择与 Player 一致的平台、有效输出目录、压缩设置和资源版本，勾选 Package 输出。没有额外构建步骤时将 Build Event Handler 留空。
4. 执行构建，检查结果、日志和版本清单。输出目录下的 Package、Full、Packed 是不同发布内容，不要混在同一个目录任意覆盖。
5. 把**对应平台的完整 Package 输出**部署到项目 StreamingAssets，保留其相对层级与清单。不要只复制一个 AssetBundle。
6. 启动时关闭编辑器资源模式，选择 Package 模式，等待 InitResources 完成，再预加载和进入场景。最后构建 Player 实测。

StarForce 的构建事件处理器默认为空。示例菜单在 Player 构建完成后部署资源，只替换输出中的 StarForce 子目录。若通过通用 Build 窗口构建，需自行将完整 Package 输出部署到启动代码指定的资源目录。

## 4. 等待资源初始化

下面是独立 Package 模式测试流程，加入 Procedure 列表并设为入口。配置完整时打印“资源初始化完成”；正式项目在这个完成点再进入预加载流程。

```csharp
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;
using ProcedureOwner = UnityQGameFramework.Core.Fsm.IFsm<UnityQGameFramework.Core.Procedure.IProcedureManager>;

namespace QgfDocs
{
    public sealed class PackageBootProcedure : ProcedureBase
    {
        private object activation;

        protected override void OnEnter(ProcedureOwner owner)
        {
            base.OnEnter(owner);
            if (QGF.Base.EditorResourceMode)
            {
                Log.Warning("请关闭编辑器资源模式，并准备 Package 资源后运行本例。");
                return;
            }
            object token = activation = new object();
            QGF.Resource.InitResources(() =>
            {
                if (!ReferenceEquals(activation, token)) return;
                Log.Info("资源初始化完成，可以开始预加载。");
            });
        }

        protected override void OnLeave(ProcedureOwner owner, bool isShutdown)
        {
            activation = null;
            base.OnLeave(owner, isShutdown);
        }
    }
}
```

InitResources 要求 Package 模式，不能重复并发调用，也不能用固定延时替代完成回调。失败日志出现时检查真实资源路径和清单；没有回调不应直接进入游戏。

## 5. 可更新模式的协作顺序

StarForce 将更新拆成多个 Procedure，值得按这个顺序读源码：

```text
CheckVersion：WebRequest 获取游戏/资源版本说明
    ↓
UpdateVersion：必要时 UpdateVersionList，下载资源版本清单
    ↓
VerifyResources：校验本地资源
    ↓
CheckResources：对比只读与可写区，得出需要更新的数量和大小
    ↓
UpdateResources：按需要下载全部或某个资源组
    ↓
Preload：配置、字典、字体、表
```

不同分支可能跳过不需要的步骤；上面的箭头表示准备关系，不是所有 API 都必须每次无条件执行。版本列表的长度、hash、压缩长度与压缩 hash 来自可信的构建/发布描述，不能填几个常量让它“通过”。

Resource 调用 Download 执行文件任务，借助 FileSystem 组织容器，并使用 ObjectPool 与 ReferencePool 管理加载对象。UpdatePrefixUri 指向服务器资源根地址；版本描述服务、平台目录、资源清单和实际资源必须属于同一次发布。玩家更新界面订阅 ResourceUpdateStart/Changed/Success/Failure 与 ResourceUpdateAllComplete 等事件，按本次更新流程处理重试和取消。

资源组可以按 Base、关卡、语言组织，但按组更新不自动保证其他组已经齐全。进入某关卡前明确它需要哪些组，以及依赖是否可用。ApplyResources 则是应用已取得的资源包，不等于调用 UpdateResources 的别名。

## 6. 发布前检查

编辑器直读成功之后，还要检查：关闭直读的编辑器运行、目标平台 Player、全新持久化目录、已有旧版本升级、断网/失败重试与重复进入流程。不要在玩家机器上访问 Source、Excel、Editor 文件或开发电脑的绝对路径。

资源管线负责构建、部署和更新运行时资源。StarForce 提供离线 Package 构建入口；在线更新还需配置项目自己的版本服务、资源分组和下载地址。

源码：[ResourceBuilderController](../../Editor/ResourceBuilder/ResourceBuilderController.cs)、[ResourceCollection](../../Editor/ResourceCollection/ResourceCollection.cs)、[ConfigPath 查找](../../Editor/Misc/Type.cs)、[ResourceComponent](../../Runtime/Unity/Resource/ResourceComponent.cs)。示例源码位于导入的 StarForce/GameMain/Scripts/Procedure 与 Scripts/Editor。
