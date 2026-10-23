//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 游戏入口。
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        /// <summary>
        /// 获取游戏基础组件。
        /// </summary>
        public static BaseComponent Base { get; private set; }

        /// <summary>
        /// 获取配置组件。
        /// </summary>
        public static ConfigComponent Config { get; private set; }

        /// <summary>
        /// 获取数据结点组件。
        /// </summary>
        public static DataNodeComponent DataNode { get; private set; }

        /// <summary>
        /// 获取数据表组件。
        /// </summary>
        public static DataTableComponent DataTable { get; private set; }

        /// <summary>
        /// 获取调试组件。
        /// </summary>
        public static DebuggerComponent Debugger { get; private set; }

        /// <summary>
        /// 获取下载组件。
        /// </summary>
        public static DownloadComponent Download { get; private set; }

        /// <summary>
        /// 获取实体组件。
        /// </summary>
        public static EntityComponent Entity { get; private set; }

        /// <summary>
        /// 获取事件组件。
        /// </summary>
        public static EventComponent Event { get; private set; }

        /// <summary>
        /// 获取文件系统组件。
        /// </summary>
        public static FileSystemComponent FileSystem { get; private set; }

        /// <summary>
        /// 获取有限状态机组件。
        /// </summary>
        public static FsmComponent Fsm { get; private set; }

        /// <summary>
        /// 获取本地化组件。
        /// </summary>
        public static LocalizationComponent Localization { get; private set; }

        /// <summary>
        /// 获取网络组件。
        /// </summary>
        public static NetworkComponent Network { get; private set; }

        /// <summary>
        /// 获取对象池组件。
        /// </summary>
        public static ObjectPoolComponent ObjectPool { get; private set; }

        /// <summary>
        /// 获取流程组件。
        /// </summary>
        public static ProcedureComponent Procedure { get; private set; }

        /// <summary>
        /// 获取资源组件。
        /// </summary>
        public static ResourceComponent Resource { get; private set; }

        /// <summary>
        /// 获取场景组件。
        /// </summary>
        public static SceneComponent Scene { get; private set; }

        /// <summary>
        /// 获取配置组件。
        /// </summary>
        public static SettingComponent Setting { get; private set; }

        /// <summary>
        /// 获取声音组件。
        /// </summary>
        public static SoundComponent Sound { get; private set; }

        /// <summary>
        /// 获取界面组件。
        /// </summary>
        public static UIComponent UI { get; private set; }

        /// <summary>
        /// 获取网络组件。
        /// </summary>
        public static WebRequestComponent WebRequest { get; private set; }

        private static void InitBuiltinComponents()
        {
            Base = UnityQGameFramework.Runtime.GameEntry.GetComponent<BaseComponent>();
            Config = UnityQGameFramework.Runtime.GameEntry.GetComponent<ConfigComponent>();
            DataNode = UnityQGameFramework.Runtime.GameEntry.GetComponent<DataNodeComponent>();
            DataTable = UnityQGameFramework.Runtime.GameEntry.GetComponent<DataTableComponent>();
            Debugger = UnityQGameFramework.Runtime.GameEntry.GetComponent<DebuggerComponent>();
            Download = UnityQGameFramework.Runtime.GameEntry.GetComponent<DownloadComponent>();
            Entity = UnityQGameFramework.Runtime.GameEntry.GetComponent<EntityComponent>();
            Event = UnityQGameFramework.Runtime.GameEntry.GetComponent<EventComponent>();
            FileSystem = UnityQGameFramework.Runtime.GameEntry.GetComponent<FileSystemComponent>();
            Fsm = UnityQGameFramework.Runtime.GameEntry.GetComponent<FsmComponent>();
            Localization = UnityQGameFramework.Runtime.GameEntry.GetComponent<LocalizationComponent>();
            Network = UnityQGameFramework.Runtime.GameEntry.GetComponent<NetworkComponent>();
            ObjectPool = UnityQGameFramework.Runtime.GameEntry.GetComponent<ObjectPoolComponent>();
            Procedure = UnityQGameFramework.Runtime.GameEntry.GetComponent<ProcedureComponent>();
            Resource = UnityQGameFramework.Runtime.GameEntry.GetComponent<ResourceComponent>();
            Scene = UnityQGameFramework.Runtime.GameEntry.GetComponent<SceneComponent>();
            Setting = UnityQGameFramework.Runtime.GameEntry.GetComponent<SettingComponent>();
            Sound = UnityQGameFramework.Runtime.GameEntry.GetComponent<SoundComponent>();
            UI = UnityQGameFramework.Runtime.GameEntry.GetComponent<UIComponent>();
            WebRequest = UnityQGameFramework.Runtime.GameEntry.GetComponent<WebRequestComponent>();
        }
    }
}
