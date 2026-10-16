//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    /// <summary>
    /// 任务基类。
    /// </summary>
    internal abstract class TaskBase : IReference
    {
        /// <summary>
        /// 任务默认优先级。
        /// </summary>
        public const int DefaultPriority = 0;

        /// <summary>
        /// 初始化任务基类的新实例。
        /// </summary>
        public TaskBase()
        {
            SerialId = 0;
            Tag = null;
            Priority = DefaultPriority;
            Done = false;
            UserData = null;
        }

        /// <summary>
        /// 获取任务的序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 获取任务的标签。
        /// </summary>
        public string Tag { get; private set; }

        /// <summary>
        /// 获取任务的优先级。
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// 获取任务的用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 获取或设置任务是否完成。
        /// </summary>
        public bool Done { get; set; }

        /// <summary>
        /// 获取任务描述。
        /// </summary>
        public virtual string Description => null;

        /// <summary>
        /// 初始化任务基类。
        /// </summary>
        /// <param name="serialId">任务的序列编号。</param>
        /// <param name="tag">任务的标签。</param>
        /// <param name="priority">任务的优先级。</param>
        /// <param name="userData">任务的用户自定义数据。</param>
        internal void Initialize(int serialId, string tag, int priority, object userData)
        {
            SerialId = serialId;
            Tag = tag;
            Priority = priority;
            UserData = userData;
            Done = false;
        }

        /// <summary>
        /// 清理任务基类。
        /// </summary>
        public virtual void Clear()
        {
            SerialId = 0;
            Tag = null;
            Priority = DefaultPriority;
            UserData = null;
            Done = false;
        }
    }
}
