# 模块示例

打开下表任一场景并运行，操作按钮观察结果，再阅读同名 C#。全部模块共用 Common/Prefabs/QGF.prefab 和公共 UI，不需要逐场景配置框架。

## 运行前准备

- 宿主使用 URP，Active Input Handling 选择 Input System Package (New) 或 Both；Sample 不修改项目设置。
- 等待编译完成，普通场景直接 Play。默认 YooAsset Editor Simulation 无需预先打包，也无需 Fix 或 Save；修改收集规则后才 Save，路径失效时才 Fix。
- `12_Resource/05_Update` 需先停止 Play，在 UpdateRemoteResources 脚本 Inspector 点击 **Build Remote Resources Example**，准备本地 HTTP 更新资源；更换 Unity 版本后重新构建。

资源配置在 `UnityQGameFramework/Resource`。通过 ResourceComponent 的 Open Collector / Open Builder 打开官方窗口；02_AddressRules 对比三个寻址规则，05_Update 展示 Host 下载。

## DataTable

先运行 [01_LoadTables](11_DataTable/01_LoadTables/LoadLubanTablesExample.cs)：组件自动初始化，`LoadDataTables<Tables>()` 同步加载指定 Catalog，随后用 `GetDataTable<TbItem>()` 查询。生成类型位于 `UnityQGameFramework.Examples.Modules.TableData`。

02_LoadCatalog 按资源地址异步加载，成功后查询；03～06 分别展示按键、筛选、具名登记、索引与表模式。切换整组表前调用 UnloadDataTables，业务丢弃旧引用。

Excel、Profile、生成代码与 bytes 已提供。改表时在 DataTable Inspector 选择 `UnityQGameFramework/Config/LubanProfile.asset` 后 Generate；Set Global 只切换编辑器生成目标，组件 Catalog 才决定运行时数据。完整说明见包内 `Documentation~/02-data-tables.md`。

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
