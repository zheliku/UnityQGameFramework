using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// Unity 游戏代码的常用组件入口。组件仍由场景创建并在 Awake 注册；
    /// 使用资源、配置等功能前，应等待组件 Start 完成。
    /// </summary>
    public static class QGF
    {
        /// <summary>
        /// 获取基础组件。
        /// </summary>
        public static BaseComponent Base
        {
            get
            {
                return Require<BaseComponent>();
            }
        }

        /// <summary>
        /// 获取配置组件。
        /// </summary>
        public static ConfigComponent Config
        {
            get
            {
                return Require<ConfigComponent>();
            }
        }

        /// <summary>
        /// 获取数据结点组件。
        /// </summary>
        public static DataNodeComponent DataNode
        {
            get
            {
                return Require<DataNodeComponent>();
            }
        }

        /// <summary>
        /// 获取数据表组件。
        /// </summary>
        public static DataTableComponent DataTable
        {
            get
            {
                return Require<DataTableComponent>();
            }
        }

        /// <summary>
        /// 获取调试器组件。
        /// </summary>
        public static DebuggerComponent Debugger
        {
            get
            {
                return Require<DebuggerComponent>();
            }
        }

        /// <summary>
        /// 获取下载组件。
        /// </summary>
        public static DownloadComponent Download
        {
            get
            {
                return Require<DownloadComponent>();
            }
        }

        /// <summary>
        /// 获取实体组件。
        /// </summary>
        public static EntityComponent Entity
        {
            get
            {
                return Require<EntityComponent>();
            }
        }

        /// <summary>
        /// 获取事件组件。
        /// </summary>
        public static EventComponent Event
        {
            get
            {
                return Require<EventComponent>();
            }
        }

        /// <summary>
        /// 获取文件系统组件。
        /// </summary>
        public static FileSystemComponent FileSystem
        {
            get
            {
                return Require<FileSystemComponent>();
            }
        }

        /// <summary>
        /// 获取有限状态机组件。
        /// </summary>
        public static FsmComponent Fsm
        {
            get
            {
                return Require<FsmComponent>();
            }
        }

        /// <summary>
        /// 获取本地化组件。
        /// </summary>
        public static LocalizationComponent Localization
        {
            get
            {
                return Require<LocalizationComponent>();
            }
        }

        /// <summary>
        /// 获取网络组件。
        /// </summary>
        public static NetworkComponent Network
        {
            get
            {
                return Require<NetworkComponent>();
            }
        }

        /// <summary>
        /// 获取对象池组件。
        /// </summary>
        public static ObjectPoolComponent ObjectPool
        {
            get
            {
                return Require<ObjectPoolComponent>();
            }
        }

        /// <summary>
        /// 获取流程组件。
        /// </summary>
        public static ProcedureComponent Procedure
        {
            get
            {
                return Require<ProcedureComponent>();
            }
        }

        /// <summary>
        /// 获取引用池组件。
        /// </summary>
        public static ReferencePoolComponent ReferencePool
        {
            get
            {
                return Require<ReferencePoolComponent>();
            }
        }

        /// <summary>
        /// 获取资源组件。
        /// </summary>
        public static ResourceComponent Resource
        {
            get
            {
                return Require<ResourceComponent>();
            }
        }

        /// <summary>
        /// 获取场景组件。
        /// </summary>
        public static SceneComponent Scene
        {
            get
            {
                return Require<SceneComponent>();
            }
        }

        /// <summary>
        /// 获取游戏设置组件。
        /// </summary>
        public static SettingComponent Setting
        {
            get
            {
                return Require<SettingComponent>();
            }
        }

        /// <summary>
        /// 获取声音组件。
        /// </summary>
        public static SoundComponent Sound
        {
            get
            {
                return Require<SoundComponent>();
            }
        }

        /// <summary>
        /// 获取界面组件。
        /// </summary>
        public static UIComponent UI
        {
            get
            {
                return Require<UIComponent>();
            }
        }

        /// <summary>
        /// 获取Web 请求组件。
        /// </summary>
        public static WebRequestComponent WebRequest
        {
            get
            {
                return Require<WebRequestComponent>();
            }
        }

        // GameEntry owns the cache and clears it when Base shuts down. Keeping a
        // second static component cache here would add a separate reset lifetime.
        /// <summary>
        /// 获取必须存在的框架组件。
        /// </summary>
        /// <typeparam name="T">框架组件类型。</typeparam>
        /// <returns>已注册的框架组件；未注册时抛出异常。</returns>
        private static T Require<T>()
            where T : GameFrameworkComponent
        {
            T component = GameEntry.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            throw new GameFrameworkException("QGF requires a registered " + typeof(T).Name + ". Add it to the framework scene and access it after initialization.");
        }
    }
}
