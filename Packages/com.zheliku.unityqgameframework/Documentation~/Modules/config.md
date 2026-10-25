# Config：读取一份游戏参数

Config 适合保存服务器地址、启动参数这类少量全局键值，文件使用 TOML。大批量结构化游戏数据由 Luban 和 DataTable 负责；两者用途不同。

[Config 示例](../../Samples~/01_Modules/02_Config/) 展示 TOML 查询、异步读文件和运行时增删。

## 准备 TOML

创建 `Assets/MyGame/Configs/Game.toml`：

```toml
[Player]
MaxHp = 100
MoveSpeed = 4.5

[Game]
Title = "我的游戏"
FeatureEnabled = true
```

标准 TOML 的嵌套表会映射为点分隔的运行时键：`Player.MaxHp`、`Game.Title`。这不是在 TOML 里引入自定义语法。

将 TOML 加入 `ResourceBuildProfile` 的收集规则，启用包的 `EnableAddressable` 并把地址规则设为 `AddressByFileName`。代码传文件名 `Game`，不传 `Assets/...` 路径或 `.toml` 扩展名。

## 加载完成后再读取

创建 `ConfigGuideProcedure.cs`，在框架 Procedure 的 Available Procedures 中勾选它，并设为入口。编辑器使用对应 Profile 构建的模拟包。下面是完整示例：

```csharp
using UnityEngine;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Core.Fsm;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;

namespace QgfDocs
{
    public sealed class ConfigGuideProcedure : ProcedureBase
    {
        private ConfigComponent config;
        private EventComponent events;

        protected override void OnEnter(IFsm<IProcedureManager> owner)
        {
            base.OnEnter(owner);
            config = QGF.Config;
            events = QGF.Event;
            events.Subscribe(LoadConfigSuccessEventArgs.EventId, OnLoaded);
            events.Subscribe(LoadConfigFailureEventArgs.EventId, OnFailed);
            config.ReadData("Game", this);
        }

        private void OnLoaded(object sender, GameEventArgs e)
        {
            var args = (LoadConfigSuccessEventArgs)e;
            if (!ReferenceEquals(args.UserData, this)) return;
            Log.Info(config.GetString("Game.Title"));
            Log.Info(config.GetInt("Player.MaxHp"));
            Log.Info(config.GetFloat("Player.MoveSpeed"));
        }

        private void OnFailed(object sender, GameEventArgs e)
        {
            var args = (LoadConfigFailureEventArgs)e;
            if (ReferenceEquals(args.UserData, this)) Log.Error(args.ErrorMessage);
        }

        protected override void OnLeave(IFsm<IProcedureManager> owner, bool isShutdown)
        {
            events.Unsubscribe(LoadConfigSuccessEventArgs.EventId, OnLoaded);
            events.Unsubscribe(LoadConfigFailureEventArgs.EventId, OnFailed);
            base.OnLeave(owner, isShutdown);
        }
    }
}
```

成功后 Console 输出标题、100 和 4.5。Player 使用相同的 YooAsset 地址读取构建后的资源包。

## ParseData 与 ReadData 的区别

`ReadData(location, userData)` 通过资源系统异步读取。`ParseData(text)` 直接解析已经拿到的 TOML 字符串，返回 bool；适合内置文本或测试，不会替你加载文件。

`GetBool/GetInt/GetFloat/GetString` 有带默认值的重载。缺键时，不带默认值的版本会抛异常；需要可选参数时显式传默认值。默认值用于缺键，不应拿它掩盖必需配置没有加载的问题。

读取方法应与配置类型匹配。Config 保留了原有四种值的接口，不是任意 TOML 类型的动态查询器。

## 当前支持的值

- bool、Int32 范围内的整数、可用有限 Single 表示的浮点数、字符串。
- TOML 子表会展开为点分隔键。
- 数组、日期时间及记录列表不属于这个 Config 适配层的支持范围；结构化设计数据使用 Luban。
- 同一批次展开后不能有重复键，也不能覆盖已有 Config 键；解析失败不会留下这一批已经添加的部分键。

例如 `[Game] Title = "A"` 与顶层 `"Game.Title" = "B"` 会映射到同一个 Config 键，应避免同时定义。重新加载前按业务需要 RemoveConfig 或 RemoveAllConfigs，不要把重复读取当作覆盖更新。

## 如何与其他模块协作

Resource 取到 TextAsset 后，DefaultConfigHelper 调用 Core 的 TomlConfigParser。解析成功或失败经由数据提供器通知 ConfigComponent，再由 EventComponent 派发结果。资源资产由辅助器释放，调用方只使用已经发布的配置值。

ConfigComponent 将查询和修改转发给 Core 的 IConfigManager。它继承的 `IDataProvider<IConfigManager>` 提供 ReadData、ParseData 及读取事件；Localization 也使用这套数据提供接口，Config 不依赖 DataTable。

03 增删示例可切换两种添加方式。组件的 `AddConfig` 显式接收 bool、int、float、string 四个值；Core 还保留 `AddConfig(name, text)`，会分别尝试转换文本，失败的类型得到 false 或 0，字符串本身保持原样。数字转换使用当前区域格式，例如输入 `42` 可读到 int 42 和 float 42，输入 `true` 只会得到 bool true。

示例用 `FrameworkEntry.GetModule<IConfigManager>()` 访问这条 Core 入口，随后仍通过 `QGF.Config` 查询和移除，操作的是同一个管理器。这是原框架的模块获取方式。TOML 解析根据原始值类型传入明确的四值表示，不经过这条文本推断入口；普通项目配置继续写 TOML。

`IConfigHelper` 目前是空的标记接口，解析方法定义在 `IDataProviderHelper<T>`。自定义格式时继承 ConfigHelperBase，实现资产读取、字节读取、文本解析、字节解析和资产释放，再交给 ConfigComponent 初始化。

ReadDataSuccess / Failure / Update / DependencyAsset 是 Core 数据提供器的事件。ConfigComponent 将它们转为 LoadConfig 对应事件，Unity 业务通过 `QGF.Event` 接收；进度与依赖通知需要先打开 Config Inspector 中的相应开关。02_LoadAsset 展示成功、失败与 UserData，回调结束后只保留需要的字段，不保存事件参数本身。

Config 不会替你保存玩家设置。修改音量应使用 Sound 和 Setting；修改道具配置应回到 Excel 生成；不要将这三类数据混在一份文件里。

源码：[ConfigComponent](../../Runtime/Unity/Config/ConfigComponent.cs)、[默认辅助器](../../Runtime/Unity/Config/DefaultConfigHelper.cs)、[TOML 适配](../../Runtime/Core/Config/TomlConfigParser.cs)。
