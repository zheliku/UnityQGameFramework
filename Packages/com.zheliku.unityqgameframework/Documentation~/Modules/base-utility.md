# Base 与 Utility：驱动框架和替换平台能力

BaseComponent 把框架接入 Unity 生命周期：启动时初始化 Core，每帧驱动模块更新，销毁时关闭 Core。常驻场景通常只放一套 QGF 根节点。

运行 [Base 示例](../../Samples~/01_Modules/01_Base/) 可对照游戏时间与真实时间，再查看 JSON、设置和常用数据工具。

## 时间、帧率和暂停

```csharp
// 方法内片段：在组件初始化完成后调用。
var app = QGF.Base;
app.FrameRate = 60;
app.RunInBackground = true;
app.NeverSleep = true;

app.PauseGame();  // 记住当前 GameSpeed，然后设为 0。
app.ResumeGame(); // 恢复暂停前的速度。
app.GameSpeed = 0.5f;
app.ResetNormalGameSpeed();
```

FrameRate 写入 Application.targetFrameRate，实际帧率仍受平台、垂直同步与负载影响。GameSpeed 写入 Time.timeScale；不要同时让另一个暂停系统独立修改 timeScale，否则组件记录值与 Unity 实际值可能不一致。

速度、暂停和应用参数示例在框架初始化后保存原值，控制脚本销毁时通过 Base 属性恢复。这样公开属性、暂停标记和 Unity 参数保持一致。尚未启动的脚本不执行恢复；框架已销毁时直接恢复 Unity 参数。

暂停不会停止 Base.Update。Core 同时收到 deltaTime 与 unscaledDeltaTime，下载、网络和使用真实时间的状态仍可能继续工作。参见 [运行链路](../00-runtime.md) 中的更新时间说明。

`EditorLanguage` 可指定编辑器运行时使用的测试语言；Player 始终根据用户设置和系统语言选择语言。

## 一个 JSON 与路径实验

将下面的流程设为入口。Base 初始化时安装 DefaultJsonHelper，进入流程后可调用 Utility.Json。

```csharp
using System;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;
using ProcedureOwner = UnityQGameFramework.Core.Fsm.IFsm<UnityQGameFramework.Core.Procedure.IProcedureManager>;
using QgfVersion = UnityQGameFramework.Core.Version;

namespace QgfDocs
{
    [Serializable]
    public sealed class PlayerSnapshot
    {
        public string Name;
        public int Level;
    }

    public sealed class UtilityGuideProcedure : ProcedureBase
    {
        protected override void OnEnter(ProcedureOwner owner)
        {
            base.OnEnter(owner);
            string json = Utility.Json.ToJson(new PlayerSnapshot { Name = "Pilot", Level = 3 });
            PlayerSnapshot restored = Utility.Json.ToObject<PlayerSnapshot>(json);
            Log.Info("{0}，等级 {1}", restored.Name, restored.Level);
            Log.Info(Utility.Path.GetRegularPath(@"Assets\MyGame\Configs\Game.toml"));
            Log.Info("框架版本 {0}，游戏版本 {1}", QgfVersion.FrameworkVersion, QgfVersion.GameVersion);
        }
    }
}
```

默认 JSON Helper 使用 Unity JsonUtility，所以上例用可序列化字段保存数据。JSON 示例可切换 `ToObject<PlayerSnapshot>(json)` 与 `ToObject(typeof(PlayerSnapshot), json)`：前者直接返回指定类型，后者返回 object，需要转换。输入为空或格式有误时，界面提示原因并保留当前角色；`{}` 是有效对象，缺少的字段取默认值。

字典、多态等数据可通过 IJsonHelper 接入其他 JSON 库。实现 ToJson、泛型和 Type 两种 ToObject，在 Base 的 Json Helper Type Name 中指定完整类型名；Base 启动时调用 SetJsonHelper 安装它。辅助器是全局配置，应在启动时确定。Config 与 Localization 直接使用 Tomlyn 解析 TOML。

## 工具类如何选择

Base 的 `06_TextAndPaths` 到 `09_Compression` 提供以下操作：

| 示例 | 可以观察什么 |
| --- | --- |
| TextAndPaths | 一至三个参数的格式化、本地路径与 URI、空目录清理 |
| ConvertBytes | 十种基础类型和 UTF-8 / UTF-16 字符串，新数组、已有数组、带偏移的写入与还原 |
| CheckAndXor | CRC32 的完整数组、范围、流重载；副本与原地 XOR、范围与 Quick 版本 |
| CompressData | 六组压缩/解压重载、字节数变化、原文还原、错误格式的返回值 |

`10_Random` 把非负整数、带上界整数、指定范围整数、[0, 1) 小数和随机字节放在同一界面。轨道标记表示本次结果在范围中的位置，字节模式显示首字节的位置。整数范围含下界、不含上界，两端相同时返回边界值。`SetSeed` 重设框架共用的随机序列；相同种子与调用顺序可以重现结果。示例只在点击“应用全局种子”时重设，不在启动时自动设置。StarForce 的敌人位置和配置选择也使用 `Utility.Random`。

`11_TypeLookup` 使用 `Utility.Assembly.GetType` 查找完整类型名，显示所属程序集与基类。Base 创建 Helper、Procedure 创建流程时也用这条查找路径。GetType 先按 .NET 规则解析名称，再遍历已加载程序集；包含程序集名称时也按 .NET 规则解析。`GetAssemblies` 返回当前已加载的程序集；`GetTypes()` 和 List 重载枚举其中的类型，List 入口会清空容器。只有字符串引用的类型在 Player 中需要通过直接引用或 link.xml 保留。

`12_StructureBytes` 用一个 `int + float`、`Sequential / Pack=1` 的结构体演示三种写入和两种读取重载。结构体没有引用字段，布局固定为 8 字节；带包头时读写使用相同偏移。与 Converter 一样，跨平台格式还需约定字节序和布局。Marshal 的共用非托管缓存按 4096 字节块扩容；`EnsureCachedHGlobalSize` 预留容量，`CachedHGlobalSize` 查看当前容量，`FreeCachedHGlobal` 释放它。下一次转换会重新申请；返回的 byte[] 独立于该缓存。示例在主线程顺序调用，退出时由 Base 关闭框架并统一释放缓存。

`Text.Format` 还提供四至十六个参数的重载，按 `{0}`、`{1}` 的顺序继续传参即可；示例用前三种说明占位符。

`GetRegularPath` 只把反斜杠换成正斜杠。`GetRemotePath` 保留已有协议，否则添加 `file:///`；它不负责解析相对路径或转义 URL。目录示例在 `Application.temporaryCachePath` 创建自己的目录和文件，展示含文件目录被保留、空目录被移除的过程；退出时清理自己写入的文件。

Converter 的数值按本机字节序写入，读取时使用相同的类型与偏移。跨平台协议应约定字节序。字符串默认 UTF-8，也可显式传 Encoding；范围参数是字节数。`Char` 是一个 UTF-16 代码单元。`ScreenDpi` 由 Base 初始化，用于厘米、英寸和像素换算；默认值或设备报告值会影响换算精度。

CRC32 适合检查数据是否改变；`GetCrc32Bytes` 将校验值写为四个大端字节。流重载从当前 Position 读到末尾，调用方管理流的生命周期。XOR 是可逆的字节混淆，相同键与范围再执行一次即可还原。副本重载保留整个原数组，Self 重载改写传入的数组；Quick 仅处理前 220 字节。

默认 Compression Helper 使用 GZip。数组结果为 `null` 或输出流返回 `false` 表示处理失败；参数或 Helper 配置错误可能抛出 FrameworkException。范围重载按 offset/length 处理，流重载从当前位置读取。示例用 MemoryStream 接收输出；默认压缩 Helper 会回写头部，需要输出流支持定位。输出流用于继续读取时先将 Position 归零。Helper 由 Base 安装，示例不替换全局 Helper。

| 入口 | 负责什么 |
| --- | --- |
| Utility.Text | 通过 ITextHelper 格式化文本 |
| Utility.Json | 通过 IJsonHelper 做 JSON 转换 |
| Utility.Compression | 通过 ICompressionHelper 压缩与解压 |
| Utility.Path | 统一分隔符，处理远程路径和目录 |
| Utility.Converter | 基础类型、字节与单位转换 |
| Utility.Verifier | CRC32 与校验值的字节表示 |
| Utility.Encryption | 字节异或混淆 |
| Utility.Random | 生成随机数 |
| Utility.Assembly | 框架类型查找 |
| Utility.Marshal | 固定布局结构体转换与共用非托管缓存 |
| Core.Version | 框架版本与 IVersionHelper 提供的游戏版本 |
| Runtime.Log / Core.FrameworkLog | Unity 层日志入口与 Core 的日志抽象 |

Utility 由静态工具类组成，直接调用；QFramework 的 IUtility 注册层尚未迁移。Core.Version.GameVersion 描述游戏版本，UPM package.json.version 描述框架包版本。

Utility 本身是按用途分组的静态容器，没有单独的实例或生命周期。`Utility.Assembly`、`Random` 等才是具体入口。

### Text 与 Compression 的扩展接口

Base 在启动时创建 `Utility.Text.ITextHelper` 与 `Utility.Compression.ICompressionHelper` 的实现，并调用 `SetTextHelper`、`SetCompressionHelper` 安装。可在 Base 的 Text / Compression Helper Type Name 中填写自定义类型的完整名称。

Text Helper 实现一至十六个参数的 `Format`，默认 `DefaultTextHelper` 使用 StringBuilder 格式化。业务调用 Utility.Text，Localization 也通过这条路径处理占位符；`10_Localization/01_ReadText` 已将 1–16 个参数放在同一示例中。

Compression Helper 实现四个方法：字节数组范围和输入流各一组 Compress / Decompress，均把结果写入输出流并返回 bool。Utility.Compression 再提供数组、范围、流及直接返回数组的重载。`09_Compression` 的六种模式经过这些入口调用默认 GZip 实现。更换算法时，资源构建端和运行端需要使用相同格式，已有压缩数据也要匹配。

业务与示例统一使用 `UnityQGameFramework.Runtime.Log`，常规信息用 `Log.Info`，警告用 `Log.Warning`，错误或捕获异常用 `Log.Error`。这套日志结构继承自 UGF：`Runtime.Log → Core.FrameworkLog → ILogHelper`，Base 初始化时安装辅助器，默认实现再输出到 Unity Console。Core 和编辑器构建菜单使用同一个 FrameworkLog；编辑器加载时会先安装默认辅助器，菜单不依赖场景中的 BaseComponent。

编辑器输出格式为 `[QGF] ● INFO | Player.Start Player.cs:42 | 消息`，时间使用 Unity Console 自带的显示，不再重复添加。等级标记适配深浅主题，四个等级使用不同的高亮色；调用位置和正文沿用 Console 的文字颜色，选中行时也能读清。选中日志后，可在详情中点击文件名跳到调用 `Log.Info` 等 QGF 日志方法的位置。源码位置依赖调试符号，没有符号时只显示调用方法。

格式化仅在 QGF 的 DefaultLogHelper 内执行，不替换 Unity 的全局日志处理器；原生 `Debug.Log` 和未接入 QGF 的第三方日志保持原样。调用位置通过当前调用栈取得，跳过 Runtime.Log、FrameworkLog 和默认辅助器，文件链接由 Unity Console 原生处理。

Player 输出纯文本，例如 `[QGF] 12:34:56.789 | INFO | 消息`，保留时间以便查看日志文件。

`Log.Error(exception)` 直接交给 Unity 记录原异常，保留类型、抛出位置和 InnerException。传 exception.Message 只会记录文本。Fatal 默认抛出 FrameworkException；记录捕获的异常使用 Error。

日志宏由 `QGF > Logging` 菜单管理。选择 `Info` 开启信息及以上级别，`All` 开启全部，`Off` 关闭全部；这些操作会修改当前项目的编译宏。Log 方法使用 Conditional，未启用对应宏时调用和参数求值都会在编译时移除；不要把必须执行的状态变更放进日志参数表达式里。框架安装与 Sample 导入不会替宿主开启日志。

## Helper 是怎样接上的

Core 定义能力接口，Unity 层提供默认实现，组件负责创建或选择实现并交给管理器。常见例子：

| 模块 | 自定义入口 |
| --- | --- |
| Resource | ResourceHelperBase、LoadResourceAgentHelperBase |
| UI / Entity | UIFormHelperBase、UIGroupHelperBase、EntityHelperBase、EntityGroupHelperBase |
| Sound | SoundHelperBase、SoundAgentHelperBase、SoundGroupHelperBase |
| Config / Localization / Setting / FileSystem | 各自的 HelperBase |
| Download / WebRequest | 各自的 AgentHelperBase |
| Network | 每个通道传入 INetworkChannelHelper |

Unity 组件支持 Helper 类型名或自定义 Helper 实例；类型名来自程序集中的完整名称。更名、移动 namespace 或增加 asmdef 后，既要检查编译引用，也要检查 Inspector 里保存的类型名是否仍然有效。

Core 的 EventPool、TaskPool、FrameworkLinkedList、FrameworkMultiDictionary 为模块提供队列、任务和集合能力。业务通过各模块的公开 API 使用这些功能。

纯 C# 宿主需要提供 Helper，循环调用 FrameworkEntry.Update，并在退出时调用 Shutdown。Unity 项目由 Base 完成这些步骤。运行约定见 [项目约定](../03-conventions.md)。

源码：[BaseComponent](../../Runtime/Unity/Base/BaseComponent.cs)、[FrameworkEntry](../../Runtime/Core/Base/FrameworkEntry.cs)、[DefaultJsonHelper](../../Runtime/Unity/Utility/DefaultJsonHelper.cs)、[Helper](../../Runtime/Unity/Utility/Helper.cs)。
