//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;

namespace UnityQGameFramework.Core.ObjectPool
{
    /// <summary>
    /// 对象池登记条目。保存 Target 及借还、释放策略，本身不是被管理的业务对象。
    /// </summary>
    public abstract class ObjectBase : IReference
    {
        /// <summary>
        /// 初始化对象基类的新实例。
        /// </summary>
        public ObjectBase()
        {
            Name = null;
            Target = null;
            Locked = false;
            Priority = 0;
            LastUseTime = default(DateTime);
        }

        /// <summary>
        /// 获取对象名称。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 获取登记的业务对象，例如 GameObject、AudioClip 或普通 C# 实例。
        /// 业务负责创建 Target，条目回调负责借用、归还和释放时的行为。
        /// </summary>
        public object Target { get; private set; }

        /// <summary>
        /// 获取或设置对象是否被加锁。
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>
        /// 获取或设置对象的优先级。
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 获取自定义释放检查标记。
        /// </summary>
        public virtual bool CustomCanReleaseFlag
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 获取对象上次使用时间。
        /// </summary>
        public DateTime LastUseTime { get; internal set; }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="target">对象。</param>
        protected void Initialize(object target)
        {
            Initialize(null, target, false, 0);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        protected void Initialize(string name, object target)
        {
            Initialize(name, target, false, 0);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        /// <param name="locked">对象是否被加锁。</param>
        protected void Initialize(string name, object target, bool locked)
        {
            Initialize(name, target, locked, 0);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        /// <param name="priority">对象的优先级。</param>
        protected void Initialize(string name, object target, int priority)
        {
            Initialize(name, target, false, priority);
        }

        /// <summary>
        /// 初始化对象基类。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <param name="target">对象。</param>
        /// <param name="locked">对象是否被加锁。</param>
        /// <param name="priority">对象的优先级。</param>
        protected void Initialize(string name, object target, bool locked, int priority)
        {
            if (target == null)
            {
                throw new FrameworkException(Utility.Text.Format("Target '{0}' is invalid.", name));
            }

            Name = name ?? string.Empty;
            Target = target;
            Locked = locked;
            Priority = priority;
            LastUseTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 包装条目归还引用池时清空字段。不会自动释放 Target，也不会在每次 Unspawn 时调用。
        /// </summary>
        public virtual void Clear()
        {
            Name = null;
            Target = null;
            Locked = false;
            Priority = 0;
            LastUseTime = default(DateTime);
        }

        /// <summary>
        /// 每次借出时调用，可恢复 Target 的状态；Register(spawned: true) 也会调用。
        /// </summary>
        protected internal virtual void OnSpawn()
        {
        }

        /// <summary>
        /// 每次归还时调用，可重置或隐藏 Target；共享借用时不代表所有使用者都已归还。
        /// </summary>
        protected internal virtual void OnUnspawn()
        {
        }

        /// <summary>
        /// 最终淘汰或关闭池时释放 Target，随后框架会将本包装条目归还引用池。
        /// </summary>
        /// <param name="isShutdown">是否是关闭对象池时触发。</param>
        protected internal abstract void Release(bool isShutdown);
    }
}
