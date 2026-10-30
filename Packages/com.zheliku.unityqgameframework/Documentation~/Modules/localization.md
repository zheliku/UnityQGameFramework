# Localization

`QGF.Localization` 通过 YooAsset 读取 TOML，使用 SmartFormat 格式化文本。语言配置直接保存在 LocalizationComponent，不需要额外的配置 SO。

## 切换语言

在 LocalizationComponent 的 Languages 点击 Language 按钮，可搜索语言名或标准代码；Fallback Language 和 Final Fallback Language 从当前配置中选择。字典和资源使用默认 YooAsset 包的地址。运行时也可从此 Inspector 选择语言并点击 Switch Language。

配置保存在组件中，也可在启动流程设置一次：

```csharp
QGF.Localization.SetLanguages(
    new LocalizationLanguage(Language.English, "Localization_English"),
    new LocalizationLanguage(Language.EnglishUnitedStates, "Localization_English"),
    new LocalizationLanguage(Language.ChineseSimplified, "Localization_Chinese", Language.English));
await QGF.Localization.SetLanguageAsync(Language.ChineseSimplified, cancellationToken);
string name = QGF.Localization.GetString("Game.Name");
```

地址来自默认资源包的 YooAsset Collector。加载会自动初始化资源包；等待框架组件完成 Start 后再切换。字典、字体和图片全部准备并校验成功后，组件才替换语言并立即发送 `LanguageChangedEventArgs`。取消、解析错误和加载失败会保留旧语言；新请求会取消尚未提交的旧请求。方法抛出的异常由业务处理。

回退顺序为显式 Fallback Code、已配置的区域父语言、组件 Fallback Language Code；循环配置会被拒绝。目标语言覆盖同名键与资源。一个 TOML 内的精确区域优先于父语言。代码与 TOML 共用标准语言身份，不再使用 English / ChineseSimplified 等旧分区名。最终回退语言必须在 Languages 中配置，留空则关闭全局回退。

```toml
[en.Game]
Name = "Star Force"

[en-US.Game]
Name = "Star Force (US)"

[zh-Hans.Game]
Name = "星际力量"

[en.Reward]
Coins = "{Player}: {Count} {Count:plural:coin|coins}"
Number = "{0:N2}"
```

`[en.Game]` 中的 Name 对应 `Game.Name`。第一层是语言分区，内部使用标准 TOML 子表；带引号的 `"Game.Name"` 是含点号的单个键，解析后也映射到同一字典键，同一分区内不能重复映射。

## 查询与格式化

```csharp
string positional = QGF.Localization.GetString("Reward.Number", 1234.5);
string named = QGF.Localization.GetString("Reward.Coins",
    new Dictionary<string, object> { { "Player", "Ada" }, { "Count", 2 } });
```

位置参数使用 `{0}`、`{1}`；命名参数传入一个字典，不依赖业务类型反射。数字与复数使用当前 Language.Code 对应的区域规则；回退文本也按当前语言格式化。无参数 GetString 返回原文；GetRawString 在缺失时返回 null，GetString 返回 `<NoKey>key`，格式参数错误返回 `<Error>key: ...`。SmartFormat 不负责字体缺字、RTL 排版或阿拉伯文字形。

## 自动刷新界面

- uGUI Text 添加 `LocalizeText` 并设置 Key，可用 SetArguments 更新动态参数。
- uGUI Image 添加 `LocalizeImage`，Key 对应语言资源的业务键。
- TMP Text 添加可选程序集中的 `LocalizeTmpText`；支持 Unity 2022.3 的 TextMeshPro 包与 Unity 6 的 uGUI 2。

Languages 的 Assets 配置业务键、YooAsset 地址和加载类型：图片选 Sprite，uGUI 字体选 Font，TMP 字体等主资产选 Object。LocalizeText / LocalizeTmpText 的 Font Key 对应此处的业务键。未配置资源时使用预制体原始资源。

组件持有语言资源句柄，在界面收到刷新通知后释放旧句柄。`GetAsset<T>(key)` 返回借用对象，调用方不可释放，且不可跨语言切换保存。绑定组件在禁用时取消订阅并还原字体或图片，重新启用时读取当前语言，适用于池化界面。示例中文字体是 Noto Sans CJK 的字符子集，只覆盖示例字符；实际项目使用完整或自制的项目字体。

内置库的职责：YooAsset 负责资源与句柄，UniTask 提供异步等待与取消，Tomlyn 解析 TOML，SmartFormat 格式化占位符、复数和数字。ZString 是 SmartFormat 的字符串构建依赖，Unsafe 是相关底层支持；业务只需调用 QGF，不必直接调用这两个库。Alchemy 和 Rainbow Folders 属于可选编辑器工具，与运行时本地化无依赖关系。

## 扩展语言

业务 API、语言配置、回退和事件统一使用 `Language`，不提供字符串切换重载。内置项可直接使用 `Language.English`、`Language.EnglishUnitedStates` 等；项目可以在自己的静态定义中添加语言，无需修改包源码：

```csharp
using UnityQGameFramework.Core;

public static class ProjectLanguages
{
    /// <summary>
    /// 墨西哥西班牙语。
    /// </summary>
    public static readonly Language SpanishMexico = Language.FromCode("es-MX");
}
```

`FromCode` 只用于语言定义及外部配置读取边界，创建时规范化并校验。之后把 `ProjectLanguages.SpanishMexico` 传给 SetLanguageAsync 或 LocalizationLanguage。相同代码表示相同语言，可以直接比较或用作字典键。没有隐式字符串转换。

Inspector 从系统支持的标准语言列表搜索选择；序列化文件与 TOML 必须保存代码字符串。读外部偏好可用 `Language.TryParse`，无效值返回 Unspecified。Core 不依赖 UnityEngine，Unity 的序列化转换留在 Runtime。

## 构建检查

配置 Languages 后，Inspector 的 Validate Dictionaries and Assets 会按默认 YooAsset 包的实际收集结果校验字典、资源类型和回退链。构建包含该组件的场景时执行同样检查；缺失翻译、根格式参数不一致、循环回退、未收集地址和无效界面绑定会阻止构建。回退补齐的翻译有效，复数分支数量允许不同；字体未映射时继续使用预制体字体。

检查复用运行时的 ParseDictionary 和回退顺序。自定义 LocalizationHelperBase 可覆写 ParseDictionary；该方法应只解析传入文本，不依赖运行中的框架组件。动态业务中使用的 key 也应出现在至少一种语言的字典中；检查无法推断运行时拼接出的 key 或参数类型。

## 原有调用

`Language` 已统一为可扩展的强类型值，保留 Language.English 等原成员名，不再是 C# enum；不能再使用 Enum.Parse 或依赖枚举数值。ParseData、ReadData 与位置参数重载继续可用。设置 Language 只选择解析与格式化语言，不会重新加载或刷新界面；手动使用旧流程时仍需自己管理字典与刷新。新项目统一使用 SetLanguageAsync，不要在切换期间混用清空字典与 ReadData。

[运行示例与完整脚本](../../Samples~/01_Modules/10_Localization/)：01_ReadText 展示查询和格式参数，02_SwitchLanguage 的按钮只请求切换，场景中的 LocalizeText / LocalizeImage 自动更新文字、字体、徽标与动态金币参数。隐藏预览后切换再显示，可观察重新启用时读取当前语言；其中缺失的中文提示使用英文回退。默认编辑器模拟模式无需提前构建；Player 按[资源工作流](resource-build.md)准备资源包。

源码：[组件](../../Runtime/Unity/Localization/LocalizationComponent.Language.cs)、[Core 管理器](../../Runtime/Core/Localization/LocalizationManager.cs)。
