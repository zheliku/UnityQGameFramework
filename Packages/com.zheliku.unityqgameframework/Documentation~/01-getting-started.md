# 安装与第一个流程

这一篇先把框架运行起来，再创建一个自己的启动流程。完成后，你会知道代码、Excel 和生成资源分别放在哪里，也能区分“修改框架”和“用框架做游戏”。

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
  Source/
    luban.conf
    Datas/
      __tables__.xlsx
      __beans__.xlsx
      __enums__.xlsx
      Item.xlsx
  Generated/
  Data/
  Extensions/
  Editor/LubanProfile.asset
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

在 Unity Hub 中添加仓库根目录。这个工程已经把框架作为 embedded package 放在 `Packages/com.zheliku.unityqgameframework`，示例源码放在 `Assets/Examples/StarForce`。不要再通过 Package Manager 导入相同的 StarForce，会与现有示例产生重复脚本和 GUID。

本地修改只有提交并推送后，其他项目才能通过 Git URL 更新到。Git URL 安装不会读取你另一个目录里尚未提交的改动。

## 2. Packages 和 Assets 各放什么

| 位置                                   | 由谁维护             | 放什么                                           |
| -------------------------------------- | -------------------- | ------------------------------------------------ |
| Packages/UnityQGameFramework           | 框架                 | Core、Unity 组件、编辑器工具、Luban 模板与运行时 |
| Assets/UnityQGameFramework/Config      | 游戏项目             | Excel、Profile、生成代码、二进制、手写扩展       |
| Assets/自己的游戏目录                  | 游戏项目             | 场景、流程、实体、UI、音效、业务脚本             |
| Assets/Samples/UnityQGameFramework/... | 导入示例后归游戏项目 | 可编辑的 StarForce 副本                          |
| Library/UnityQGameFramework            | 本机缓存             | 下载的 Luban 编译器和生成临时文件                |

使用 Git URL 安装后，修改包缓存不是可靠的开发方式，重新解析包或清理 Library 会丢失这些修改。自己的 Excel 和 C# 写在 Assets。需要维护框架时，使用完整仓库或自己的 fork。

`Samples~` 是包中的示例源目录。Unity 忽略其中的脚本和资源；点击 Import 后，Package Manager 将它复制到 Assets，副本才会参与编译和运行。

## 3. 先运行 StarForce

在 Package Manager 中选中 **UnityQGameFramework**，展开 **Samples**，点击 StarForce 的 **Import**。

导入后，在 Project 窗口打开：

```text
Assets/Samples/UnityQGameFramework/<包版本>/StarForce/StarForce Launcher.unity
```

如果是完整仓库，打开：

```text
Assets/Examples/StarForce/StarForce Launcher.unity
```

点击 Play，等待菜单出现，再点击开始。在 Game 视图中按住鼠标左键，飞机会向鼠标位置移动并持续开火。示例保留原版鼠标玩法。

从 Launcher 启动，而非直接打开 `GameMain/Scenes/Main.unity` 运行。Launcher 包含框架组件和预加载流程，后者负责读取配置、打开菜单和切换战斗场景。

示例自带已生成的表数据，因此运行示例不需要 .NET 或 Luban 编译器。框架会通过包依赖安装 Unity UI；示例的输入桥接同时支持旧 Input Manager 和 Input System。

示例使用随目录提供的 `StarForce/Surface`、`StarForce/Additive` Shader，可用于 Built-in 和 URP。它不指定宿主的渲染管线，不安装 URP，也不改 Active Input Handling、自定义 Layer 或 Build Profiles。编辑器资源模式直接按路径加载 Menu、Main 场景，编辑器试玩不需要先把它们加入构建列表。

这里验证的是编辑器运行。制作独立 Player 时，仍需自己选择启动场景、配置构建场景或 AssetBundle 资源，并完成发行资源构建；导入 Sample 不会代替项目做这些决定。

## 4. 创建自己的启动流程

下面回到自己的游戏工程，用默认 Item 表做一个最小例子。

### 生成默认表

打开 `QGF > Settings`，确认 Profile 指向：

```text
Assets/UnityQGameFramework/Config/Editor/LubanProfile.asset
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

            var dataTable = GameEntry.GetComponent<DataTableComponent>();
            dataTable.LoadDataTables(loader => new Tables(loader));

            // Inspector 引用模式同步解析；加载失败时不会注册部分表。
            if (!dataTable.HasDataTable<DRItem>()) return;

            var item = dataTable.GetDataTable<DRItem>().GetDataRow(1);
            Debug.Log(item == null ? "找不到道具 1" : item.Name);
        }
    }
}
```

`ProcedureBase` 表示一个游戏流程，类似启动、预加载、登录、战斗这些阶段。这个例子把加载与读取写在同一个启动流程里，方便看到完整调用链。

`GameEntry.GetComponent<DataTableComponent>()` 获取场景中的数据表组件。它与 `FrameworkEntry.GetModule<T>()` 的层次不同：后者获取纯 C# 模块，通常由 Unity 组件内部使用。写游戏时优先从组件入口调用。

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

Procedure 在框架其他组件的 Start 之后进入启动流程，所以这里可以调用 DataTable。不要把相同代码移到另一个组件的 Awake 中；那时 Event、Resource 等组件可能还没有完成初始化。

默认 `Assembly-CSharp` 会自动引用框架程序集。如果自己的代码使用 asmdef，添加引用：

```text
UnityQGameFramework.Core
UnityQGameFramework.Runtime
UnityQGameFramework.Luban
```

不要让业务程序集引用框架 Editor 程序集。Profile 与生成菜单属于编辑器，Player 只使用生成代码和 Catalog 数据。

## 5. 几个常用入口

| 菜单                               | 用途                                 |
| ---------------------------------- | ------------------------------------ |
| QGF > Settings                     | 指定全局 Profile，创建另一个配置目录 |
| QGF > Generate Tables              | 使用全局 Profile 生成                |
| QGF > Open Folder > Table Sources  | 打开全局 Profile 的 Excel 源目录     |
| QGF > Open Folder > Generated Code | 打开生成代码目录                     |
| QGF > Open Folder > Table Data     | 打开 bytes 与 Catalog 目录           |
| QGF > Open Folder > Save Data      | 打开当前应用的存档目录               |
| QGF > Documentation                | 打开包内教程索引                     |

DataTable Inspector 也可以选择 Profile，再点击 **设为全局**。这和项目设置是同一个选择；只选中 Project 中的某个 Profile 不会改变全局生成目标。

Profile 控制生成到哪里，Luban Catalog 控制组件运行时读哪一份数据，两者独立。选择另一个 Profile 不会自动改掉已有场景的数据引用。需要切换时，先生成，再在组件中点击 **使用全局 Catalog**。

## 6. 更新和卸载

### 更新

Git 包解析后的提交会记录在 `Packages/packages-lock.json`。团队应提交 manifest 和 lock 文件，使用相同的框架版本。要锁定发布标签或提交，在 URL 末尾附加 `#标签或提交`。

更新包时通过 Package Manager 更新 Git 依赖。更新后检查 Console，必要时重新生成表格。生成模板变化时，代码和数据必须成套更新。

已经导入 Assets 的 Sample 是副本，不会随包更新自动覆盖。先保存自己的改动，再决定是否重新导入。自己的游戏最好放在独立目录，避免修改与示例更新混在一起。

### 只移除示例

可以删除导入的 StarForce 文件夹，也可以执行 `QGF > Uninstall > StarForce`。菜单会先将目录和 `.meta` 备份到项目根目录的 `QgfBackup~`，再移除示例。它不需要恢复渲染管线、Layer 或输入设置，因为示例没有修改这些设置。

### 移除框架

`QGF > Uninstall > Framework` 会备份并移除登记的配置工作区和导入的 StarForce，然后请求 Package Manager 移除框架。通过 Package Manager 直接 Remove 也会触发这份目录清理。

备份不参与 Unity 编译，确认自己的 Excel 和扩展代码已保存后可自行删除。`QgfBackup~` 已在本仓库的 `.gitignore` 中忽略；使用自己项目时，也可加入相同规则。

清理范围是框架登记的工作区和示例。自己写在其他目录中的业务脚本仍由自己管理；如果它们引用了 QGF，卸载框架前需要同步移除或迁移这些引用。单独移动到工作区之外的自定义文件夹也应自行处理。完整仓库中的 embedded package 由 Git 管理，卸载框架菜单不会删除这份框架源码。

资源工具首次保存到默认 `Assets/UnityQGameFramework/Configs` 时，也会登记这个配置目录；卸载框架时同样备份并移除。它与 Luban 的 `Config` 工作区用途不同，后者保存 Excel 与生成物。

## 7. 遇到问题时先检查这里

**Git 包已安装，但没有初始 Excel。** 等待 Unity 完成编译，检查 Console 是否有阻止初始化的错误。确认 `QGF > Settings` 中没有沿用另一个已有 Profile。未改动的 Item 初始工作区缺少三份 schema 时，初始化会补齐缺少文件，保留已有 Excel 和 `.meta`。自定义表定义无法从模板还原，请恢复版本控制中的原件，或在新目录创建完整工作区。

**生成提示找不到 dotnet。** 在终端执行 `dotnet --list-runtimes`，安装 .NET 8 或兼容运行时后重启 Unity，让编辑器获得新的 PATH。

**网络无法下载 Luban。** 自行下载官方 Luban 5.1.0，在 `QGF > Settings > 高级 > 本地编译器` 选择解压后的 `Luban.dll`。留空恢复使用 Library 下载缓存。本机路径不提交给团队。

**出现两个 DRItem 或 Tables。** 检查是否把同一套生成代码复制到了另一个 Assets 目录。同一命名空间下的相同类型只能保留一份；第二套独立配置要使用不同命名空间。

**示例找不到 Menu 场景或资源。** 确认完整导入 Sample，并从 Launcher 启动。保留文件夹 `.meta`；资源路径通过目录 GUID 定位，不依赖固定的 Assets/Samples 路径。

接下来阅读 [用 Luban 制作与读取配置表](02-data-tables.md)，从 Item 表开始增加字段和查询。
