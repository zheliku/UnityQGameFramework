# FileSystem

在一个容器文件内管理多份逻辑文件。

```csharp
var files = QGF.FileSystem.CreateFileSystem(path, FileSystemAccess.ReadWrite, 4, 16);
files.WriteFile("player/name.txt", bytes);
byte[] restored = files.ReadFile("player/name.txt");
QGF.FileSystem.DestroyFileSystem(files, false);
```

path 是磁盘容器路径，player/name.txt 是容器内逻辑名。创建前准备父目录，已有容器使用 LoadFileSystem。DestroyFileSystem 第二个参数控制是否删除磁盘容器；只关闭时传 false。文件分段与调用方缓冲区见第二个示例。

[运行示例与完整脚本](../../Samples~/01_Modules/17_FileSystem/)。

源码：[组件](../../Runtime/Unity/FileSystem/FileSystemComponent.cs)。
