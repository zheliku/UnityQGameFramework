# 01 · 模块示例

每个场景只演示一项核心功能。运行场景，再阅读同名 C# 脚本。

模块 Sample 使用独立 ResourceSettings 和 YooAsset 原生 BundleCollectorSetting；导入时不会修改项目设置。Player 构建流程见点点点和 StarForce。

## 运行前准备

等待 Unity 编译完成后，打开表格中的场景并点击 Play。普通模块场景不需要手动构建资源包，也不需要在 YooAsset 收集器窗口点击 `Fix` 或 `Save`；QGF 会使用本 Sample 的原生配置自动生成 Editor Simulation。

`11_DataTable` 的生成 C#、Catalog 和二进制表已随示例提供，直接运行即可。修改 Excel 后，在 `QGF > Settings` 选择本 Sample 的 Luban Profile，再执行 `QGF > Generate Tables`。

`12_Resource/01_LoadPrefab`、`02_AddressRules`、`03_Package` 和 `04_Binary` 可直接运行。运行 `05_Update` 前先停止 Play，选中场景中的 `UpdateRemoteResources` 对象，在 Inspector 点击 `构建远程资源示例`；该按钮会构建本地 HTTP 示例需要的资源并写入 `Library/QgfExamples/YooAsset/<Unity版本>`。构建记录与当前 Unity 版本不一致时需要重新执行。

`QGF > Resource > Packages` 打开 YooAsset 原生窗口，选择或双击本 Sample 的 `UnityQGameFramework/Resource/YooAssetCollector.asset` 切换到对应面板；其中的寻址规则、分组和构建选项就是实际使用的配置。修改后用窗口自己的 `Save` 保存，`Fix` 只在 GUID 路径失效时使用。

## DataTable 从哪里开始

先运行 [01_LoadTables](11_DataTable/01_LoadTables/LoadLubanTablesExample.unity)，再看同名脚本。这个场景演示最常用的“启动时加载一次，后续只查询”。示例已经带有生成结果，不需要先安装编译器或重新生成。

### Profile、Catalog 和生成代码

| 文件或设置 | 用途 | 游戏运行时是否读取 |
| --- | --- | --- |
| [LubanProfile.asset](UnityQGameFramework/Config/LubanProfile.asset) | 引用 luban.conf、指定代码和数据输出目录、target | 否，只用于编辑器生成 |
| `QGF > Settings` 中的全局 Profile | 指定“生成表格”菜单使用哪份 Profile；只是对上述资产的选择 | 否 |
| [Catalog.asset](UnityQGameFramework/Config/Data/Catalog.asset) | 生成的文件名 → bytes 引用清单，并记录结构版本 | 是，由 DataTableComponent 加载 |
| `Generated/Tables.cs`、`TbItem.cs`、`DRItem.cs` | 生成的加载器、表类型和行类型 | 编译进游戏，负责解析和查询 |
| `Data/*.bytes` | Excel 导出的具体记录值 | 是，由生成代码反序列化 |

Catalog 不是缓存，不能当作临时文件删除；它与 bytes 一起构成运行时数据。全局 Profile 只决定生成目标，不会自动替换场景组件的 Catalog。因此你切换全局 Profile 后，当前场景仍可使用原来指定的示例数据。

编辑 Excel 时：在 `QGF > Settings` 选择上表的 Profile，执行 `QGF > Generate Tables`，等待 Unity 编译完成。要让场景改用另一份生成结果，再把那份 `Data/Catalog.asset` 拖到场景 QGF 对象的 DataTableComponent，或点 **使用全局 Catalog**。加载器 `Tables` 必须来自同一套生成代码；结构不匹配时会拒绝加载。

### 需要 Initialize 吗

不需要手动调用 Initialize。场景中启用的 QGF 组件会自动完成初始化；普通业务脚本在 `Start` 中即可调用：

```csharp
// 使用本 Sample 的生成类型：UnityQGameFramework.Examples.Modules.TableData。
private void Start()
{
    // 从组件上指定的 Catalog 同步读取 bytes；返回 true 时整组表已经可以查询。
    if (!QGF.DataTable.LoadDataTables<Tables>())
    {
        return;
    }

    TbItem items = QGF.DataTable.GetDataTable<TbItem>();
    DRItem item = items.GetDataRow(1);
    Log.Info("道具：{0}，价格：{1}", item.Name, item.Price);
}
```

同步方式要求场景中有 DataTable、Event 组件，并已指定 Catalog。通用 QGF prefab 不带游戏专属 Catalog，本示例在场景实例上指定。不要在 `Awake`/`OnEnable` 调用加载；若运行时才创建 QGF 对象，也要先让它完成 `Start`。

`GetDataTable<TbItem>()` 只查询已经登记的表，不会自动加载。实际项目应由启动流程先完成加载，再进入使用表的业务流程；不要依赖多个普通脚本 `Start` 的执行先后。每次查询不用再加载，`Update` 中也不要重复加载。

### 同步和异步示例有什么区别

- `01_LoadTables`：`LoadDataTables<Tables>()` 使用组件上的 Catalog，同步返回是否成功，成功后可紧接着查询。
- `02_LoadCatalog`：`LoadDataTables<Tables>("Tables_Catalog", this)` 按资源地址异步加载另一份 Catalog，不使用组件上的 Catalog。先订阅结果，收到成功事件后查询；也可用 `await LoadDataTablesAsync<Tables>(address, cancellationToken: token)`。此方式还需要配置 ResourceComponent 和 YooAsset 收集规则，本示例已配置好。
- `03`～`06`：在同一套已加载表对象上展示按键查询、条件筛选、具名登记、索引、表模式与跨表引用。

同一组件同时只加载一组表，切换前调用 `UnloadDataTables()`；这会移除登记，业务也应丢弃旧表和旧行引用。修改 Excel 并生成不会自动刷新内存中的记录，需重新进入运行或明确卸载、重载。

## 场景清单

| 场景 | 内容 |
| --- | --- |
| [GameSpeedExample](01_Base/01_GameSpeed/GameSpeedExample.unity) | 01  时间倍率 |
| [JsonUtilityExample](01_Base/02_Json/JsonUtilityExample.unity) | 02  JSON 对象 |
| [LoggingExample](01_Base/03_Logging/LoggingExample.unity) | 03  日志级别 |
| [TextAndPaths](01_Base/04_TextAndPaths/TextAndPaths.unity) | 04  文本与路径 |
| [ConvertBytes](01_Base/05_ByteConversion/ConvertBytes.unity) | 05  字节转换与偏移 |
| [DrawRandom](01_Base/06_Random/DrawRandom.unity) | 06  可复现随机数 |
| [ConvertStructure](01_Base/07_StructureBytes/ConvertStructure.unity) | 07  结构体与字节 |
| [ReadConfigValues](02_Config/01_ReadValues/ReadConfigValues.unity) | 01  解析与查询配置 |
| [LoadConfigAsset](02_Config/02_LoadAsset/LoadConfigAsset.unity) | 02  异步读取配置资产 |
| [NodePaths](03_DataNode/01_Path/NodePaths.unity) | 01  路径与相对节点 |
| [ManageNodeChildren](03_DataNode/02_Children/ManageNodeChildren.unity) | 02  管理子节点 |
| [SubscribeEvent](04_Event/01_Subscribe/SubscribeEvent.unity) | 01  订阅、发送与取消 |
| [EventDispatchOrder](04_Event/02_DispatchOrder/EventDispatchOrder.unity) | 02  立即派发与排队 |
| [TwoStates](05_Fsm/01_TwoStates/TwoStates.unity) | 01  等待与工作 |
| [FsmStateData](05_Fsm/02_StateData/FsmStateData.unity) | 02  状态机共享数据 |
| [NamedMachines](05_Fsm/03_NamedMachines/NamedMachines.unity) | 03  用名称管理多台状态机 |
| [ProcedureEntryExample](06_Procedure/01_Entry/ProcedureEntryExample.unity) | 01  从入口流程开始 |
| [ChangeProcedureExample](06_Procedure/02_ChangeState/ChangeProcedureExample.unity) | 02  菜单与游玩 |
| [ProcedureDataExample](06_Procedure/03_Data/ProcedureDataExample.unity) | 03  把关卡选择交给加载流程 |
| [ReuseReference](07_ReferencePool/01_Reuse/ReuseReference.unity) | 01  借出、归还、再借出 |
| [ReferenceOwnership](07_ReferencePool/02_Ownership/ReferenceOwnership.unity) | 02  谁负责归还变量 |
| [SpawnObject](08_ObjectPool/01_Spawn/SpawnObject.unity) | 01  借出与归还 |
| [SpawnOrCreateCube](08_ObjectPool/02_Factory/SpawnOrCreateCube.unity) | 02  池空时由工厂创建 |
| [ObjectPoolPolicy](08_ObjectPool/03_Policy/ObjectPoolPolicy.unity) | 03  锁定对象，控制回收 |
| [SaveCounter](09_Setting/01_SaveCounter/SaveCounter.unity) | 01  保存计数 |
| [SaveSettingObject](09_Setting/02_Object/SaveSettingObject.unity) | 02  保存与读取对象 |
| [ReadTranslation](10_Localization/01_ReadText/ReadTranslation.unity) | 01  查询与格式化翻译 |
| [SwitchLanguage](10_Localization/02_SwitchLanguage/SwitchLanguage.unity) | 02  切换语言 |
| [LoadLubanTablesExample](11_DataTable/01_LoadTables/LoadLubanTablesExample.unity) | 01  加载 Luban 表 |
| [LoadCatalogExample](11_DataTable/02_LoadCatalog/LoadCatalogExample.unity) | 02  异步加载 Luban 表 |
| [QueryRowsExample](11_DataTable/03_QueryRows/QueryRowsExample.unity) | 03  按 Id 查询 |
| [FilterRowsExample](11_DataTable/04_FilterRows/FilterRowsExample.unity) | 04  条件筛选与排序 |
| [TableRegistryExample](11_DataTable/05_TableRegistry/TableRegistryExample.unity) | 05  具名表与登记管理 |
| [TableModesExample](11_DataTable/06_TableModes/TableModesExample.unity) | 06  表模式、索引与引用 |
| [LoadCubePrefab](12_Resource/01_LoadPrefab/LoadCubePrefab.unity) | 01  资产与实例的生命周期 |
| [LoadAddressRules](12_Resource/02_AddressRules/LoadAddressRules.unity) | 02  三种寻址规则 |
| [LoadResourcePackage](12_Resource/03_Package/LoadResourcePackage.unity) | 03  资源包中的资产 |
| [ReadBinaryResource](12_Resource/04_Binary/ReadBinaryResource.unity) | 04  二进制文件片段 |
| [UpdateRemoteResources](12_Resource/05_Update/UpdateRemoteResources.unity) | 05  HTTP 资源更新 |
| [ShowCubeEntity](13_Entity/01_ShowHide/ShowCubeEntity.unity) | 01  显示与隐藏实体 |
| [ReuseCubeEntity](13_Entity/02_Reuse/ReuseCubeEntity.unity) | 02  实例复用与缓存保留 |
| [OpenWindowExample](14_UI/01_OpenWindow/OpenWindowExample.unity) | 01  打开与关闭窗口 |
| [ReopenWindowExample](14_UI/02_Reopen/ReopenWindowExample.unity) | 02  窗口复用与缓存 |
| [CoverWindowExample](14_UI/03_Cover/CoverWindowExample.unity) | 03  覆盖、暂停与置顶 |
| [OneShotSoundExample](15_Sound/01_OneShot/OneShotSoundExample.unity) | 01  播放提示音 |
| [SoundGroupVolumeExample](15_Sound/02_GroupVolume/SoundGroupVolumeExample.unity) | 02  声音组与单声道音量 |
| [SpatialSoundExample](15_Sound/03_Spatial/SpatialSoundExample.unity) | 03  空间声音 |
| [LoadRoomExample](16_Scene/01_LoadRoom/LoadRoomExample.unity) | 01  加载与卸载房间 |
| [SwitchRoomsExample](16_Scene/02_SwitchRooms/SwitchRoomsExample.unity) | 02  顺序切关 |
| [WriteReadFileExample](17_FileSystem/01_WriteRead/WriteReadFileExample.unity) | 01  写入与读取容器 |
| [ReadFileSegments](17_FileSystem/02_Segments/ReadFileSegments.unity) | 02  读取文件片段 |
| [GetWebRequestExample](18_WebRequest/01_Get/GetWebRequestExample.unity) | 01  GET 与响应正文 |
| [CancelWebRequestExample](18_WebRequest/02_Cancel/CancelWebRequestExample.unity) | 02  取消排队中的请求 |
| [PostWebRequest](18_WebRequest/03_Post/PostWebRequest.unity) | 03  POST 表单 |
| [DownloadFileExample](19_Download/01_File/DownloadFileExample.unity) | 01  下载到文件 |
| [DownloadProgressExample](19_Download/02_Progress/DownloadProgressExample.unity) | 02  字节进度与写盘 |
| [CancelDownloadExample](19_Download/03_Cancel/CancelDownloadExample.unity) | 03  取消与断点续传 |
| [ConnectNetworkExample](20_Network/01_Connect/ConnectNetworkExample.unity) | 01  连接 TCP 通道 |
| [EchoNetworkExample](20_Network/02_Echo/EchoNetworkExample.unity) | 02  消息回显 |
| [NetworkHeartbeat](20_Network/03_Heartbeat/NetworkHeartbeat.unity) | 03  心跳响应与丢失 |
| [InspectDebuggerExample](21_Debugger/01_Inspect/InspectDebuggerExample.unity) | 01  查看运行信息 |
| [DebuggerWindowLifecycle](21_Debugger/02_CustomWindow/DebuggerWindowLifecycle.unity) | 02  自定义调试窗口 |
