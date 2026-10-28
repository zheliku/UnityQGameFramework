# Download

将网络内容下载到磁盘文件。

```csharp
QGF.Event.Subscribe(DownloadSuccessEventArgs.EventId, OnSuccess);
int serialId = QGF.Download.AddDownload(localPath, url, this);
QGF.Download.RemoveDownload(serialId);
```

事件类型使用 Runtime，按 SerialId 关联进度、成功和失败；不要仅看到网络字节到达就读取未完成文件。示例自带本地 HTTP 服务，演示写盘、进度、取消与断点续传。YooAsset 包更新使用 Resource 的官方下载器。

[运行示例与完整脚本](../../Samples~/01_Modules/19_Download/)。

源码：[组件](../../Runtime/Unity/Download/DownloadComponent.cs)。
