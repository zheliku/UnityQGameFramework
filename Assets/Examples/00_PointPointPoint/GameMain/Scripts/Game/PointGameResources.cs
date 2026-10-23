using System.IO;
using UnityEngine;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.PointPointPoint
{
    // Package 资源放在示例专属目录，与宿主的资源清单分开。
    public sealed class PointGameResources : MonoBehaviour
    {
        public const string Folder = "QgfExamples/PointPointPoint";

        private void Awake()
        {
            if (Application.isEditor && GetComponent<BaseComponent>().EditorResourceMode) return;
            // 路径必须在 ResourceComponent.Start 创建加载代理之前设置。
            var resources = FrameworkEntry.GetModule<IResourceManager>();
            resources.SetReadOnlyPath(Path.Combine(Application.streamingAssetsPath, Folder));
            resources.SetReadWritePath(Path.Combine(Application.persistentDataPath, Folder));
        }
    }
}
