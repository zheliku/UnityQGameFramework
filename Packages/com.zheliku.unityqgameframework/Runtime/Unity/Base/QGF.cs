using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// Unity 游戏代码的常用组件入口。组件仍由场景创建并在 Awake 注册；
    /// 使用资源、配置等功能前，应等待组件 Start 完成。
    /// </summary>
    public static class QGF
    {
        public static BaseComponent Base => Require<BaseComponent>();
        public static ConfigComponent Config => Require<ConfigComponent>();
        public static DataNodeComponent DataNode => Require<DataNodeComponent>();
        public static DataTableComponent DataTable => Require<DataTableComponent>();
        public static DebuggerComponent Debugger => Require<DebuggerComponent>();
        public static DownloadComponent Download => Require<DownloadComponent>();
        public static EntityComponent Entity => Require<EntityComponent>();
        public static EventComponent Event => Require<EventComponent>();
        public static FileSystemComponent FileSystem => Require<FileSystemComponent>();
        public static FsmComponent Fsm => Require<FsmComponent>();
        public static LocalizationComponent Localization => Require<LocalizationComponent>();
        public static NetworkComponent Network => Require<NetworkComponent>();
        public static ObjectPoolComponent ObjectPool => Require<ObjectPoolComponent>();
        public static ProcedureComponent Procedure => Require<ProcedureComponent>();
        public static ReferencePoolComponent ReferencePool => Require<ReferencePoolComponent>();
        public static ResourceComponent Resource => Require<ResourceComponent>();
        public static SceneComponent Scene => Require<SceneComponent>();
        public static SettingComponent Setting => Require<SettingComponent>();
        public static SoundComponent Sound => Require<SoundComponent>();
        public static UIComponent UI => Require<UIComponent>();
        public static WebRequestComponent WebRequest => Require<WebRequestComponent>();

        // GameEntry owns the cache and clears it when Base shuts down. Keeping a
        // second static component cache here would add a separate reset lifetime.
        private static T Require<T>() where T : FrameworkComponent
        {
            T component = GameEntry.GetComponent<T>();
            if (component != null) return component;
            throw new FrameworkException("QGF requires a registered " + typeof(T).Name
                + ". Add it to the framework scene and access it after initialization.");
        }
    }
}
