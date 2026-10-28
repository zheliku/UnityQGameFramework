# WebRequest

发起 HTTP 请求并读取响应。

```csharp
QGF.Event.Subscribe(WebRequestSuccessEventArgs.EventId, OnSuccess);
int serialId = QGF.WebRequest.AddWebRequest(url, this);
// 离开业务时取消未完成请求并取消订阅。
QGF.WebRequest.RemoveWebRequest(serialId);
```

事件类型使用 UnityQGameFramework.Runtime；按 SerialId 或 UserData 区分请求，GetWebResponseBytes 取得响应内容。GET、POST 与取消示例自带本地服务器。请求队列的取消与响应派发可能交叉，退出后不再更新旧界面。下载到文件用 Download，游戏长连接用 Network。

[运行示例与完整脚本](../../Samples~/01_Modules/18_WebRequest/)。

源码：[组件](../../Runtime/Unity/WebRequest/WebRequestComponent.cs)。
