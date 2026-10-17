# 配置文件与模块的分工

UGF 的 Config 是游戏运行时的全局键值配置模块，并不是框架全部设置的总入口。QGF 保留这个职责，默认文件格式改为 TOML。成批角色、装备、关卡等记录交给 DataTable / Luban；玩家存档和音量选择交给 Setting；多语言文本交给 Localization。

## 游戏 Config：少量全局参数

StarForce 的 `GameMain/Configs/DefaultConfig.toml`：

```toml
[Game]
Id = "Star Force"

[Scene]
Menu = 1
Main = 2
```

`[Scene]` 下的 `Menu` 注册为键 `Scene.Menu`。它表示菜单场景的业务 Id；场景路径仍保存在 Luban 的 Scene 表中。流程先读这个 Id，再查 Scene 表的记录，最后让 SceneComponent 加载场景。`Game.Id` 是保留的游戏标识，当前示例没有业务代码读取它。这些值属于游戏参数，不是 Unity 编辑器偏好。

加载链路：`ProcedurePreload` → `ConfigComponent.ReadData` → `ResourceComponent` 获取 TextAsset → `DefaultConfigHelper` 调用 **Tomlyn** → ConfigManager 保存键值 → 成功／失败事件。所有配置准备完成后才进入后续流程。与数据表一样，编辑源文件后需要重新加载才能更新运行中的值。

```csharp
// Start 初始化完成后加载；异步结果沿用 LoadConfigSuccess / Failure 事件。
GameEntry.Config.ReadData(AssetUtility.GetConfigAsset("DefaultConfig"), this);

// 加载成功后查询；也可提供缺失键的默认值。
string gameId = GameEntry.Config.GetString("Game.Id");
int menuId = GameEntry.Config.GetInt("Scene.Menu");
bool enabled = GameEntry.Config.GetBool("Feature.Enabled", false);
float speed = GameEntry.Config.GetFloat("Game.Speed", 1f);
```

其他接口有 Count、HasConfig、AddConfig、RemoveConfig、RemoveAllConfigs，以及直接 ParseData。它们沿用原模块；并不依赖 DataTable。`GameEntry` / `AssetUtility` 是示例的业务入口，自己的项目可通过框架组件取得 ConfigComponent。

TOML 语法交给内置的 Tomlyn 0.19.0（.NET Standard 2.0 / TOML 1.0）；QGF 只负责将标量映射到既有 GetString / GetBool / GetInt / GetFloat。支持嵌套分节、引号字符串、布尔值、Int32 和有限 Single 范围数值。数组、日期与复杂记录不映射到标量 API，请放入 Luban。无效语法、重复键、扁平化后键冲突或数值越界会整份失败，不留下半份配置；多文件重复注册已有键同样失败。XML 编辑器设置和 Localization 的格式不受影响。

`.toml` 由包内 ScriptedImporter 导入为 TextAsset，可随场景或 AssetBundle 进入 Player；Player 不依赖编辑器导入器。DefaultConfigHelper 的 byte[] 路径也按 UTF-8 TOML 解析，不再读取旧 TXT 或 BinaryReader 格式。StarForce 原 TXT 的 GUID 已保留到 TOML，资源集合引用同步迁移。

## 各种“配置”文件分别做什么

| 文件 | 使用者与作用 | 何时读取 |
| --- | --- | --- |
| `DefaultConfig.toml` | ConfigComponent：游戏标识、启动场景 Id 等少量全局参数 | 游戏预加载／显式加载 |
| `Datas/*.xlsx` | Luban：角色、装备、场景等业务记录 | 编辑器生成时；Player 不读取 Excel |
| `__tables__ / __beans__ / __enums__.xlsx` | Luban：表索引与模式、复用结构、枚举定义 | 生成时 |
| `Source/luban.conf` | Luban 生成工程：输入、schema、targets、namespace、manager、分组 | 生成时；当前是 Luban 官方 JSON 格式 |
| `Editor/LubanProfile.asset` | QGF 生成设置：源表、代码、数据目录与 target | 编辑器菜单与 Inspector |
| `ProjectSettings/UnityQGameFrameworkSettings.asset` | QGF：全局 Profile 与新工作区默认目录 | 编辑器项目设置 |
| `Generated/*.cs`、`Data/*.bytes`、`Catalog.asset` | DataTableComponent：C# 结构 + 二进制记录；Catalog 提供文件引用及版本核对 | C# 在编译时，数据在游戏加载时 |
| `BuildSettings.xml` | 构建场景工具：默认场景列表与搜索目录 | 编辑器构建工具 |
| `ResourceEditor.xml` | 资源管理器：资源根目录、搜索类型、标签与排序 | 编辑器资源管理工具 |
| `ResourceCollection.xml` | 资源集合：Asset GUID、资源分组与打包归属 | 编辑器资源管理／打包 |
| `ResourceBuilder.xml` | 资源构建器：平台、压缩方式、版本、输出和构建回调 | 编辑器打包 |
| Localization 语言文件 | LocalizationComponent：按语言查询文本 | 游戏加载语言时 |

后四类 XML 来自 UGF 的编辑器工具，不是 ConfigComponent 的数据，也不是 Luban schema。本次只将游戏 Config 的默认格式改为 TOML；不能简单更名这些 XML 扩展名。目录、生成与运行时 Catalog 的区别见 [Luban 使用说明](luban.md)。
