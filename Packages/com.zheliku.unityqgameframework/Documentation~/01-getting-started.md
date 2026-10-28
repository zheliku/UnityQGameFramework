# 安装与第一个流程

安装包后，先运行点点点，再创建自己的启动流程。框架代码在 Packages，游戏代码、Excel 和生成资源在 Assets。

以下操作以 Unity 6000.3.24f1 为例。包声明的最低版本为 Unity 2022.3。日常使用框架不需要下载 Luban 编译器；第一次生成表格时才需要 .NET 8 运行时或兼容版本。

## 1. 选择安装方式

### 在自己的游戏项目中使用

先安装 Git，使 Unity 能在系统 PATH 中找到 `git`。打开自己的 Unity 项目，在 `Window > Package Manager` 中点击添加按钮，选择 **Install package from git URL**，粘贴：

```text
https://github.com/zheliku/UnityQGameFramework.git?path=/Packages/com.zheliku.unityqgameframework
```

等待包解析与脚本编译完成。不要在导入中途复制另一份框架到 Assets，否则会出现重复类型。

Unity 将框架放入包缓存，Project 窗口中的位置是：

```text
Packages/UnityQGameFramework
```

首次安装同时创建自己的配置工作区：

```text
Assets/UnityQGameFramework/Config/
  LubanProfile.asset
  luban.conf
  Source/
    __tables__.xlsx
    __beans__.xlsx
    __enums__.xlsx
    Item.xlsx
  Generated/
  Data/
```

这时 `Generated` 和 `Data` 可以为空。自动初始化只准备文件和 Profile，不会下载工具或生成游戏代码。`__beans__`、`__enums__` 初始只有标题，这是留给复用结构与枚举的空模板。

如果项目已经指定过 Profile，安装或更新后沿用原选择；不会另建一个默认工作区。已有 Profile 丢失时，在 `QGF > Settings` 重新指定。已有多个 Profile 且没有全局选择时，也在这里明确选一个。

### 参与框架开发

想修改框架源码时，克隆整个仓库：

```powershell
git clone https://github.com/zheliku/UnityQGameFramework.git
```

仓库中的图片、音频等资源使用 Git LFS。先安装 Git LFS，再在仓库目录执行：

```powershell
git lfs install
git lfs pull
```

在 Unity Hub 中添加仓库根目录。框架以 embedded package 放在 `Packages/com.zheliku.unityqgameframework`，全部示例源码在 `Assets/Examples`。开发工程直接运行这些场景；再导入同一 Sample 会产生重复脚本和 GUID。

本地修改只有提交并推送后，其他项目才能通过 Git URL 更新到。Git URL 安装不会读取你另一个目录里尚未提交的改动。

## 2. Packages 和 Assets 各放什么

| 位置                                   | 由谁维护             | 放什么                                           |
| -------------------------------------- | -------------------- | ------------------------------------------------ |
| Packages/UnityQGameFramework           | 框架                 | Core、Unity 组件、编辑器工具、Luban 模板与运行时 |
| Assets/UnityQGameFramework/Config      | 游戏项目             | Excel、Profile、生成代码、二进制、手写扩展       |
| Assets/自己的游戏目录                  | 游戏项目             | 场景、流程、实体、UI、音效、业务脚本             |
| Assets/Samples/UnityQGameFramework/... | 导入示例后归游戏项目 | 可编辑的 Sample 副本                              |
| Library/UnityQGameFramework            | 本机缓存             | 下载的 Luban 编译器和生成临时文件                |

使用 Git URL 安装后，修改包缓存不是可靠的开发方式，重新解析包或清理 Library 会丢失这些修改。自己的 Excel 和 C# 写在 Assets。需要维护框架时，使用完整仓库或自己的 fork。

`Samples~` 是包中的示例源目录。Unity 忽略其中的脚本和资源；点击 Import 后，Package Manager 将它复制到 Assets，副本才会参与编译和运行。

## 3. 先运行点点点

在 Package Manager 的 Samples 中导入 **00 · PointPointPoint**，打开 `PointPointPoint.unity` 并运行。点击目标得分，点击空白会扣分；完成目标或倒计时结束后进入结算。

示例直接使用 Input System。宿主的 Player Settings > Active Input Handling 需选择 Input System Package (New) 或 Both，示例不会自动修改此设置。

源码目录是 `Assets/Examples/00_PointPointPoint`。启动、菜单、游玩和结算分别在 Procedure 中；界面通过 Event 通知本局流程。

## 4. 再运行 StarForce

导入 **02 · StarForce**，打开 `StarForce Launcher.unity` 并运行。点击开始进入战斗，按住鼠标左键移动飞机并开火。

源码目录是 `Assets/Examples/02_StarForce`，Package Manager 中的 Sample 名称为 `02_StarForce`。Launcher 负责初始化和预加载；Menu、Main 场景由资源模块加载。

## 5. 创建自己的启动流程

下面回到自己的游戏工程，用默认 Item 表做一个最小例子。

### 生成默认表

打开 `QGF > Settings`，确认 Profile 指向：

```text
Assets/UnityQGameFramework/Config/LubanProfile.asset
```

执行 `QGF > Generate Tables`，快捷键为 Ctrl/Cmd + Shift + G。

首次生成会下载固定的 Luban 版本到 Library，并调用 `dotnet`。生成成功后，`Generated` 中会出现 `DRItem.cs`、`TbItem.cs`、`Tables.cs`，`Data` 中会出现 `tbitem.bytes` 和 `Catalog.asset`。等待 Unity 完成编译再继续。

### 编写流程

创建 `Assets/MyGame/Scripts/GameStartup.cs`，写入以下完整代码：

```csharp
using UnityEngine;
using UnityQGameFramework.Config;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace MyGame
{
    public sealed class GameStartup : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);

            var dataTable = QGF.DataTable;
            dataTable.LoadDataTables<Tables>();

            // Inspector 引用模式同步解析；加载失败时不会注册部分表。
            if (!dataTable.HasDataTable<TbItem>()) return;

            var item = dataTable.GetDataTable<TbItem>().GetDataRow(1);
            Log.Info(item == null ? "找不到道具 1" : item.Name);
        }
    }
}
```

`ProcedureBase` 表示一个游戏阶段。这个例子在启动流程中读取 Item 表，再查询一条记录。

`QGF.DataTable` 获取场景中的数据表组件。它与 `FrameworkEntry.GetModule<T>()` 的层次不同：后者获取纯 C# 模块，通常由 Unity 组件内部使用。写游戏时优先从组件入口调用。

### 放入框架预制体

创建空场景并保存为 `Assets/MyGame/Launcher.unity`。从包中找到：

```text
Runtime/Prefabs/UnityQGameFramework.prefab
```

将它拖入场景。展开预制体：

1. 选中 **Procedure** 节点，在 Available Procedures 中勾选 `MyGame.GameStartup`，将 Entrance Procedure 设为它。
2. 选中 **DataTable** 节点，把默认工作区 `Data/Catalog.asset` 拖入 **Luban Catalog**。
3. 保存场景并点击 Play。

Console 应输出“示例道具”。默认 Item 表只有一条 Id 为 1 的记录。

Procedure 在框架组件完成 Start 后进入启动流程，所以这里可以调用 DataTable。普通组件的 Awake 早于其他组件 Start；跨模块初始化放在入口流程中。

默认 `Assembly-CSharp` 会自动引用框架程序集。如果自己的代码使用 asmdef，添加引用：

```text
UnityQGameFramework.Core
UnityQGameFramework.Runtime
UnityQGameFramework.Luban
```

Profile 与生成菜单属于 Editor 程序集。Player 业务程序集只需引用运行时和生成代码。

## 6. 常用入口

| 菜单                               | 用途                                 |
| ---------------------------------- | ------------------------------------ |
| QGF > Settings                     | 指定全局 Profile，创建另一个配置目录 |
| QGF > Generate Tables              | 使用全局 Profile 生成                |
| QGF > Open Folder > Table Sources  | 打开全局 Profile 的 Excel 源目录     |
| QGF > Open Folder > Generated Code | 打开生成代码目录                     |
| QGF > Open Folder > Table Data     | 打开 bytes 与 Catalog 目录           |
| QGF > Open Folder > Save Data      | 打开当前应用的存档目录               |
| QGF > Documentation                | 打开包内示例索引                     |

DataTable Inspector 也可以选择 Profile，再点击 **设为全局**。选择 Profile 后，Project 窗口中的选中状态不会改变全局生成目标。

Profile 控制生成到哪里，Luban Catalog 控制组件运行时读哪一份数据，两者独立。选择另一个 Profile 不会自动改掉已有场景的数据引用。需要切换时，先生成，再在组件中点击 **使用全局 Catalog**。

## 7. 更新和卸载

### 更新

Git 包解析后的提交会记录在 `Packages/packages-lock.json`。团队应提交 manifest 和 lock 文件，使用相同的框架版本。要锁定发布标签或提交，在 URL 末尾附加 `#标签或提交`。

Git 安装使用 `packages-lock.json` 记录的提交，不按 `package.json` 版本自动更新。更新分支依赖时，在 Package Manager 的 **Install package from git URL** 中重新提交 URL。固定版本的 URL 需要改成目标标签、提交或 `#main`。详见 [Unity 的 Git 依赖说明](https://docs.unity3d.com/6000.3/Documentation/Manual/upm-git.html#git-locks)。

每次对外发布时，同步更新包根目录 `package.json` 的 `version`、`Core.Version.FrameworkVersion` 和 `CHANGELOG.md`，并提交、推送这些文件。Package Manager 通过包根目录的 Changelog 提供版本记录；发布检查会验证三者一致。

更新后检查 Console，必要时重新生成表格。生成模板变化时，代码和数据必须成套更新。

已导入的 Sample 是 Assets 中的副本，不会随包更新自动覆盖。重新导入前，保存自己的修改并删除旧版本目录。同一项目不要保留两个版本的同一 Sample，否则会重复编译其中的脚本。

### 只移除示例

在 Project 窗口删除导入的示例目录即可。示例不修改宿主项目的输入、渲染、图层或构建场景设置，无需恢复这些设置。如果将示例 Profile 设为全局生成目标，删除后在 `QGF > Settings` 重新选择自己的 Profile。

### 移除框架

先保存自己的 Excel、生成物和扩展代码，删除或迁移引用 QGF 的示例及业务脚本，再通过 Package Manager 的 **Remove** 移除框架。Assets 中的配置目录由项目管理，不会随包卸载自动删除；确认不再使用后自行移除。`ProjectSettings/UnityQGameFrameworkSettings.asset` 只保存全局 Luban Profile 和默认目录，不再使用框架时可以删除。

## 8. 可选编辑器扩展

打开 `QGF > Extensions` 查看用途和 Git URL。复制地址后，在 Package Manager 的 **Install package from git URL** 中安装：

| 工具 | 用途 | 此 Git 包的最低 Unity 版本 |
| --- | --- | --- |
| [Alchemy](https://github.com/annulusgames/Alchemy) | Inspector、Hierarchy 和 EditorWindow 增强 | 2021.2 |
| [Rainbow Folders 2](https://github.com/Borod4r/Rainbow-Folders-2) | Project 文件夹颜色、图标及规则 | 6000.3 |

面板只提供说明和复制地址，不检查安装状态，也不发起包安装请求。版本、更新和移除由开发者通过 Package Manager 管理。卸载 QGF 不会删除独立安装的工具。

框架包的 `package.json` 不声明这些依赖，QGF 的运行时代码、Inspector 和示例也不引用它们的类型。完整开发仓库的 `Packages/manifest.json` 已包含这两个工具，属于开发环境选择，其他项目通过 Git URL 安装 QGF 时不会继承。

提交 `manifest.json` 和 `packages-lock.json` 后，团队会使用相同工具版本。固定上游版本时，在 Git URL 末尾指定 `#完整提交哈希`。

Alchemy 的序列化扩展还需要 Unity.Serialization；只使用 Inspector 增强不由 QGF 自动安装额外的序列化库。自己的业务程序集如果开始引用 Alchemy 类型，仍需保留对应引用。后续若为 QGF Inspector 增加 Alchemy 专用适配，应放入独立的可选 Editor 适配程序集。

[Alchemy 使用 MIT 许可](https://github.com/annulusgames/Alchemy/blob/main/LICENSE)。[Rainbow Folders 2 的上游说明](https://github.com/Borod4r/Rainbow-Folders-2#license)区分了代码和美术资源：代码为 Apache-2.0，图标、图片等为 CC BY-NC 4.0。QGF 不内嵌或重新授权这些插件；商业项目使用 Rainbow Folders 的美术资源前，应核对相应授权。该插件还依赖 Unity 内部 Editor API，升级 Unity 时需要检查兼容性。

## 9. 常见问题

**Git 包已安装，但没有初始 Excel。** 等待 Unity 完成编译，检查 Console 是否有阻止初始化的错误。确认 `QGF > Settings` 中没有沿用另一个已有 Profile。未改动的 Item 初始工作区缺少三份 schema 时，初始化会补齐缺少文件，保留已有 Excel 和 `.meta`。自定义表定义无法从模板还原，请恢复版本控制中的原件，或在新目录创建完整工作区。

**生成提示找不到 dotnet。** 在终端执行 `dotnet --list-runtimes`，安装 .NET 8 或兼容运行时后重启 Unity，让编辑器获得新的 PATH。

**网络无法下载 Luban。** QGF 会把固定版本的官方 Luban 编译器缓存到 `Library/UnityQGameFramework/Tools/Luban`。检查网络或系统 `tar` 命令后重新执行生成；不需要在项目设置中指定本机编译器路径。

**出现两个 DRItem 或 Tables。** 检查是否把同一套生成代码复制到了另一个 Assets 目录。同一命名空间下的相同类型只能保留一份；第二套独立配置要使用不同命名空间。

**示例找不到 Menu 场景或资源。** 确认完整导入 Sample，并从 Launcher 启动。保留文件夹 `.meta`；资源路径通过 Launcher 目录或 YooAsset 地址定位，不依赖固定的 Assets/Samples 路径。

继续阅读 [用 Luban 制作与读取配置表](02-data-tables.md)，从 Item 表开始增加字段和查询。
