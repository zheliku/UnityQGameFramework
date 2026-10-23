//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;

namespace UnityQGameFramework.Core.ObjectPool
{
    /// <summary>
    /// 对象池接口。
    /// </summary>
    /// <typeparam name="T">ObjectBase 登记条目类型；条目的 Target 才是被管理的业务对象。</typeparam>
    public interface IObjectPool<T> where T : ObjectBase
    {
        /// <summary>
        /// 获取对象池名称。
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 获取对象池完整名称。
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// 获取登记条目类型 T，不是 Target 的运行时类型。
        /// </summary>
        Type ObjectType { get; }

        /// <summary>
        /// 获取登记条目总数，包含借出与空闲条目。
        /// </summary>
        int Count { get; }

        /// <summary>
        /// 获取对象池中能被释放的对象的数量。
        /// </summary>
        int CanReleaseCount { get; }

        /// <summary>
        /// 获取是否允许对象被多次获取。
        /// </summary>
        bool AllowMultiSpawn { get; }

        /// <summary>
        /// 获取或设置对象池自动释放可释放对象的间隔秒数。
        /// </summary>
        float AutoReleaseInterval { get; set; }

        /// <summary>
        /// 获取或设置释放策略的容量阈值。不会限制创建或借出数量，只淘汰符合条件的空闲目标。
        /// </summary>
        int Capacity { get; set; }

        /// <summary>
        /// 获取或设置对象池对象过期秒数。
        /// </summary>
        float ExpireTime { get; set; }

        /// <summary>
        /// 获取或设置对象池的优先级。
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// 登记已有的条目及 Target，不会创建业务对象。spawned 为 true 时执行一次 OnSpawn。
        /// </summary>
        /// <param name="obj">对象。</param>
        /// <param name="spawned">对象是否已被获取。</param>
        void Register(T obj, bool spawned);

        /// <summary>
        /// 检查是否存在可借出的空名称条目。
        /// </summary>
        /// <returns>要检查的对象是否存在。</returns>
        bool CanSpawn();

        /// <summary>
        /// 检查指定名称下是否存在可借出的条目。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <returns>要检查的对象是否存在。</returns>
        bool CanSpawn(string name);

        /// <summary>
        /// 借出空名称条目并触发 OnSpawn；没有可借对象时返回 null，不会自动创建。
        /// </summary>
        /// <returns>要获取的对象。</returns>
        T Spawn();

        /// <summary>
        /// 借出指定名称的条目并触发 OnSpawn；没有可借对象时返回 null。
        /// </summary>
        /// <param name="name">对象名称。</param>
        /// <returns>要获取的对象。</returns>
        T Spawn(string name);

        /// <summary>
        /// 归还一次借用并触发 OnUnspawn。超过容量时可能进一步淘汰可释放的空闲目标。
        /// </summary>
        /// <param name="obj">要回收的对象。</param>
        void Unspawn(T obj);

        /// <summary>
        /// 通过 Target 归还一次借用；它与传入登记条目的重载操作同一份池记录。
        /// </summary>
        /// <param name="target">要回收的对象。</param>
        void Unspawn(object target);

        /// <summary>
        /// 设置对象是否被加锁。
        /// </summary>
        /// <param name="obj">要设置被加锁的对象。</param>
        /// <param name="locked">是否被加锁。</param>
        void SetLocked(T obj, bool locked);

        /// <summary>
        /// 设置对象是否被加锁。
        /// </summary>
        /// <param name="target">要设置被加锁的对象。</param>
        /// <param name="locked">是否被加锁。</param>
        void SetLocked(object target, bool locked);

        /// <summary>
        /// 设置对象的优先级。
        /// </summary>
        /// <param name="obj">要设置优先级的对象。</param>
        /// <param name="priority">优先级。</param>
        void SetPriority(T obj, int priority);

        /// <summary>
        /// 设置对象的优先级。
        /// </summary>
        /// <param name="target">要设置优先级的对象。</param>
        /// <param name="priority">优先级。</param>
        void SetPriority(object target, int priority);

        /// <summary>
        /// 释放对象。
        /// </summary>
        /// <param name="obj">要释放的对象。</param>
        /// <returns>释放对象是否成功。</returns>
        bool ReleaseObject(T obj);

        /// <summary>
        /// 释放对象。
        /// </summary>
        /// <param name="target">要释放的对象。</param>
        /// <returns>释放对象是否成功。</returns>
        bool ReleaseObject(object target);

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        void Release();

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        /// <param name="toReleaseCount">尝试释放对象数量。</param>
        void Release(int toReleaseCount);

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
        void Release(ReleaseObjectFilterCallback<T> releaseObjectFilterCallback);

        /// <summary>
        /// 释放对象池中的可释放对象。
        /// </summary>
        /// <param name="toReleaseCount">尝试释放对象数量。</param>
        /// <param name="releaseObjectFilterCallback">释放对象筛选函数。</param>
        void Release(int toReleaseCount, ReleaseObjectFilterCallback<T> releaseObjectFilterCallback);

        /// <summary>
        /// 释放对象池中的所有未使用对象。
        /// </summary>
        void ReleaseAllUnused();
    }
}
