# Network

建立 TCP 通道，自定义消息编解码和处理。

```csharp
var channel = QGF.Network.CreateNetworkChannel("Main", ServiceType.Tcp, protocol);
channel.Connect(address, port);
// 连接成功后发送 Packet；protocol 负责序列化和接收处理。
channel.Close();
QGF.Network.DestroyNetworkChannel("Main");
```

protocol 实现 INetworkChannelHelper，负责包头、消息序列化和处理函数。用 Runtime 网络事件判断连接和错误，并确认事件对应当前通道。示例自带本机 Echo 服务；后两个示例展示消息往返与心跳。关闭业务时取消订阅并销毁通道。

[运行示例与完整脚本](../../Samples~/01_Modules/20_Network/)。

源码：[组件](../../Runtime/Unity/Network/NetworkComponent.cs)。
