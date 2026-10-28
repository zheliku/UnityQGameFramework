# Localization

选择语言并读取、格式化 TOML 字典。

```csharp
QGF.Localization.Language = Language.English;
QGF.Localization.ParseData(dictionaryText);
string name = QGF.Localization.GetString("Game.Name");
string reward = QGF.Localization.GetString("Reward.Coins", playerName, coins);
```

[English.Game] 中的 Name 映射为 Game.Name；{0}、{1} 是格式参数。切换 Language 后需移除旧字典、加载或解析目标语言并刷新界面；Language 属性本身不会重新加载。ReadData 走资源异步流程，成功事件后显示。

```toml
[English.Game]
Name = "Star Force"

[Korean.Game]
Name = "게임 프레임워크"
```

使用标准 TOML 子表，语言在第一层；`"Game.Name"` 表示含点号的单个键，与子表不同。

[运行示例与完整脚本](../../Samples~/01_Modules/10_Localization/)。

源码：[组件](../../Runtime/Unity/Localization/LocalizationComponent.cs)。
