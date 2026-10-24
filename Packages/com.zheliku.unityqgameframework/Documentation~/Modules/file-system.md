# FileSystem：把多个逻辑文件放进一个容器

FileSystem 在一个物理容器里保存多个逻辑文件，并通过索引和块分配管理空间。Resource 可用它组织资源，业务代码也能单独创建容器。

[FileSystem 示例](../../Samples~/01_Modules/17_FileSystem/) 展示完整读写和分段读取。每个场景都显示文件内容和操作结果。

少量玩家设置优先交给 [Setting](setting.md)。普通 JSON 存档直接使用 System.IO 也可以。只有需要统一管理许多逻辑文件时，才值得使用 FileSystem；容器不会自动加密、备份或同步云存档。

## 创建、关闭，再重新读取

场景需要 Base、FileSystem，使用默认文件系统 Helper。下面流程在 persistentDataPath/GuideFiles/guide.dat 中写入逻辑文件 player/name.txt，关闭容器，再重新打开并读取。Console 应输出 QGF Player。

保存为 `Assets/MyGame/Scripts/FileSystemGuideProcedure.cs`，加入流程列表并作为入口：

```csharp
using System.IO;
using System.Text;
using UnityEngine;
using UnityQGameFramework.Core.FileSystem;
using UnityQGameFramework.Core.Procedure;
using UnityQGameFramework.Runtime;
using ProcedureOwner = UnityQGameFramework.Core.Fsm.IFsm<UnityQGameFramework.Core.Procedure.IProcedureManager>;

namespace QgfDocs
{
    public sealed class FileSystemGuideProcedure : ProcedureBase
    {
        protected override void OnEnter(ProcedureOwner owner)
        {
            base.OnEnter(owner);
            var files = QGF.FileSystem;
            string directory = Path.Combine(Application.persistentDataPath, "GuideFiles");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "guide.dat");

            IFileSystem container = File.Exists(path)
                ? files.LoadFileSystem(path, FileSystemAccess.ReadWrite)
                : files.CreateFileSystem(path, FileSystemAccess.ReadWrite, 16, 64);
            try
            {
                if (!container.WriteFile("player/name.txt", Encoding.UTF8.GetBytes("QGF Player")))
                    throw new IOException("写入逻辑文件失败。");
            }
            finally
            {
                files.DestroyFileSystem(container, false);
            }

            container = files.LoadFileSystem(path, FileSystemAccess.Read);
            try
            {
                byte[] bytes = container.ReadFile("player/name.txt");
                if (bytes == null) throw new IOException("找不到逻辑文件。");
                Log.Info(Encoding.UTF8.GetString(bytes));
            }
            finally
            {
                files.DestroyFileSystem(container, false);
            }
        }
    }
}
```

guide.dat 是本示例创建的测试容器，不能指向其他系统管理的文件。第二次运行会打开原文件并更新相同逻辑键。

## 两层路径不要混用

`path` 是真实磁盘路径，必须位于当前平台可写的目录；`player/name.txt` 只是容器中的逻辑名称，磁盘上不会因此出现 player 文件夹。读写都通过同一个 IFileSystem 对象进行。

CreateFileSystem 的 maxFileCount 和 maxBlockCount 是容器元数据容量上限，要按预计逻辑文件数和分块情况规划，不能简单理解为总字节数。新建时不能只申请 Read 权限；加载已有容器可以使用 Read、Write 或 ReadWrite。

Write 只允许业务写入。默认底层流仍需读取头与索引来重开容器；IFileSystem 的 ReadFile、ReadFileSegment 和 SaveAsFile 会检查访问方式并拒绝只写句柄。

同一真实路径在管理器中只能打开一次。共享容器应由一个长期服务持有并借给业务，使用者不要各自 LoadFileSystem，也不要擅自关闭共享句柄。

| API | 作用 |
| --- | --- |
| HasFile / GetFileInfo / GetAllFileInfos | 查询容器内的文件 |
| WriteFile | 写入或覆盖一个逻辑文件，可接收 byte[]、Stream 或磁盘文件路径 |
| ReadFile / ReadFileSegment | 全量读取或读取指定片段；频繁读取可复用 byte[] |
| SaveAsFile | 将逻辑文件导出成真实磁盘文件 |
| RenameFile / DeleteFile | 修改或移除逻辑条目 |
| DestroyFileSystem(container, false) | 关闭并从管理器移除，保留磁盘容器 |
| DestroyFileSystem(container, true) | 关闭并删除整个磁盘容器 |

使用结束必须关闭，尤其是后面要备份、替换或删除文件时。WriteFile 等返回 bool 的方法要检查结果，不能只因没有异常就显示“保存成功”。跨多个逻辑文件的业务更新不自动具有数据库事务语义；关键存档仍需版本号、校验和恢复策略。

源码：[FileSystemComponent](../../Runtime/Unity/FileSystem/FileSystemComponent.cs)、[IFileSystem](../../Runtime/Core/FileSystem/IFileSystem.cs)、[FileSystemManager](../../Runtime/Core/FileSystem/FileSystemManager.cs)。
