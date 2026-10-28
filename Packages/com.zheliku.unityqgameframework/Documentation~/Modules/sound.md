# Sound

按声音组管理音乐和音效。

```csharp
QGF.Sound.AddSoundGroup("Prompt", false, false, 0.25f, 1);
int serialId = QGF.Sound.PlaySound("Tone", "Prompt");
QGF.Sound.StopSound(serialId);
```

PlaySound 返回播放编号；加载成功后由声音代理播放。声音组控制组音量、静音和代理数量，PlaySoundParams 控制单次音量、循环、空间混合等。声音组和单次音量共同作用；StopSound 结束播放，资源归还由声音模块处理。

[运行示例与完整脚本](../../Samples~/01_Modules/15_Sound/)。

源码：[组件](../../Runtime/Unity/Sound/SoundComponent.cs)。
