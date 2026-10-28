# Config

读取少量只读游戏参数。TOML 由 Tomlyn 解析。

```csharp
QGF.Config.ParseData(configText);
int health = QGF.Config.GetInt("Player.MaxHp");
float speed = QGF.Config.GetFloat("Player.Speed");
string title = QGF.Config.GetString("Game.Title");
```

[Player] 下的 MaxHp 映射为 Player.MaxHp；支持 bool、string、Int32、Single 与嵌套章节。数组和成批结构放 Luban，玩家修改后保存的设置用 Setting。ReadData(address, userData) 通过资源模块异步读取，成功事件后查询；ParseData 直接解析已有文本。加载新配置前按业务需要移除旧键，避免重复键。

```toml
[Game]
Title = "My Game"

[Player]
MaxHp = 100
Speed = 5.0
```

[运行示例与完整脚本](../../Samples~/01_Modules/02_Config/)。

源码：[组件](../../Runtime/Unity/Config/ConfigComponent.cs)。
