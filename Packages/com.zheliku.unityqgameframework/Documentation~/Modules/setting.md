# Setting：记住音量和上次选择

Setting 保存音量、语言等玩家偏好。默认 Helper 将键值写入 `Application.persistentDataPath/GameFrameworkSetting.dat`。

[Setting 示例](../../Samples~/09_Setting/) 演示基本值、对象和键管理。三个场景共用一个 Helper，各自存到 `QgfExamples/Setting` 下；文件名在首次加载时确定。

| 示例 | 操作与观察 |
| --- | --- |
| 01_SaveCounter | 编辑姓名、静音和音量，保存后修改输入，再重新读取；移除音量后恢复默认值 |
| 02_Object | 编辑飞行员姓名与等级，比较两个 SetObject、四个 GetObject 入口 |
| 03_Keys | 写入或覆盖键，比较数组与 List 查询，移除单个键或清空 |

## 第一次运行与第二次运行

创建 `SettingGuideProcedure.cs`，在框架预制体的 Procedure 中勾选并设为入口：

```csharp
using UnityEngine;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace QgfDocs
{
    public sealed class SettingGuideProcedure : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            var setting = QGF.Setting;
            int count = setting.GetInt("Guide.LaunchCount", 0) + 1;
            setting.SetInt("Guide.LaunchCount", count);
            setting.SetFloat("Guide.MusicVolume", 0.3f);
            setting.Save();
            Log.Info("示例启动次数：" + count);
        }
    }
}
```

第一次运行输出 1；停止后再次运行会输出 2。使用 `QGF > Open Folder > Save Data` 可以打开当前应用的持久化目录。

如果已经运行过这个例子，计数会接着增加。要重置它，只移除 `Guide.LaunchCount` 后 Save，不要为了测试删除全部玩家设置。

## 常用读写

| 类型         | 读取                                | 写入                  |
| ------------ | ----------------------------------- | --------------------- |
| bool         | GetBool(key, defaultValue)          | SetBool(key, value)   |
| int          | GetInt(key, defaultValue)           | SetInt(key, value)    |
| float        | GetFloat(key, defaultValue)         | SetFloat(key, value)  |
| string       | GetString(key, defaultValue)        | SetString(key, value) |
| 可序列化对象 | `GetObject<T>(key, defaultValue)` | SetObject(key, value) |

Set 修改内存中的设置，Save 请求立即写盘。框架正常关闭时，SettingManager 也会调用 Save；异常退出可能来不及保存。适合在确认设置、关闭设置界面等时机主动保存，无需每帧写盘。

01_SaveCounter 将输入框和当前设置分开显示。“保存设置”先写入内存，再 Save；“重新读取已保存设置”调用 `FrameworkEntry.GetModule<ISettingManager>().Load()`，随后刷新输入框。这个入口与 SettingComponent.Start 使用的是同一份 Core 管理器，重读不会再次增加运行次数。

默认对象读写先经 Utility.Json 转成字符串，再进入设置存储。默认 JSON 辅助器使用 Unity 的 JSON 能力，因此对象应满足相应的序列化限制；它不会因为使用 Setting 就获得任意多态或字典序列化能力。

有键时，GetObject 从已保存的 JSON 重建对象，修改返回对象后需要再次 SetObject。缺键且提供默认对象时，直接返回该默认对象本身，不复制，也不自动建立设置项。

## 应用音量的顺序

1. 读取 `MusicVolume`，缺少时使用设计好的默认值。
2. 给 Sound 的 Music 组设置音量。
3. 玩家调整后，更新声音组和 Setting 的内存值。
4. 确认或关闭设置时 Save。

Setting 不会自动发现声音组并同步它们。StarForce 的 `InitSoundSettings()` 与设置界面代码负责这个连接。

## 生命周期与替换存储

SettingComponent.Awake 创建并设置辅助器，Start 调用 Load。入口 Procedure 开始时可以读取已经加载的设置；普通脚本的 Awake 不应假设 Load 已完成。

需要云存档、另一种本地格式或自己的加密方案时，实现 SettingHelperBase 并在组件中指定。默认 dat 文件是框架自己的序列化格式；不要用 TOML 编辑器直接修改它，也不要因为文件扩展名是 dat 就认为内容具备防修改能力。

GetAllSettingNames、HasSetting、RemoveSetting 和 RemoveAllSettings 用于管理键。List 查询先清空传入列表；数组查询返回新数组。默认 Helper 的无默认值读取在缺键时记录警告并返回 false、0 或 null；首次运行应使用带默认值的重载。GetObject 的默认值也只在缺键时使用，无法替代对损坏数据的处理。

Config 读取 TOML 中的游戏参数，DataTable 读取 Luban 生成的结构化表，Setting 保存玩家修改后的偏好。Setting 可以覆盖已有键，Config.AddConfig 则拒绝重复键。

源码：[SettingComponent](../../Runtime/Unity/Setting/SettingComponent.cs)、[DefaultSettingHelper](../../Runtime/Unity/Setting/DefaultSettingHelper.cs)、[SettingManager](../../Runtime/Core/Setting/SettingManager.cs)。
