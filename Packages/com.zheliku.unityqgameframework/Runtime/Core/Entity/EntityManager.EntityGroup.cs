//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.ObjectPool;
using System.Collections.Generic;

namespace UnityQGameFramework.Core.Entity
{
    internal sealed partial class EntityManager : FrameworkModule, IEntityManager
    {
        /// <summary>
        /// 实体组。
        /// </summary>
        private sealed class EntityGroup : IEntityGroup
        {
            private readonly IObjectPool<EntityInstanceObject> m_InstancePool;
            private readonly FrameworkLinkedList<IEntity> m_Entities;
            private LinkedListNode<IEntity> m_CachedNode;

            /// <summary>
            /// 初始化实体组的新实例。
            /// </summary>
            /// <param name="name">实体组名称。</param>
            /// <param name="instanceAutoReleaseInterval">实体实例对象池自动释放可释放对象的间隔秒数。</param>
            /// <param name="instanceCapacity">实体实例对象池容量。</param>
            /// <param name="instanceExpireTime">实体实例对象池对象过期秒数。</param>
            /// <param name="instancePriority">实体实例对象池的优先级。</param>
            /// <param name="entityGroupHelper">实体组辅助器。</param>
            /// <param name="objectPoolManager">对象池管理器。</param>
            public EntityGroup(string name, float instanceAutoReleaseInterval, int instanceCapacity, float instanceExpireTime, int instancePriority, IEntityGroupHelper entityGroupHelper, IObjectPoolManager objectPoolManager)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new FrameworkException("Entity group name is invalid.");
                }

                if (entityGroupHelper == null)
                {
                    throw new FrameworkException("Entity group helper is invalid.");
                }

                Name = name;
                Helper = entityGroupHelper;
                m_InstancePool = objectPoolManager.CreateSingleSpawnObjectPool<EntityInstanceObject>(Utility.Text.Format("Entity Instance Pool ({0})", name), instanceCapacity, instanceExpireTime, instancePriority);
                m_InstancePool.AutoReleaseInterval = instanceAutoReleaseInterval;
                m_Entities = new FrameworkLinkedList<IEntity>();
                m_CachedNode = null;
            }

            /// <summary>
            /// 获取实体组名称。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 获取实体组中实体数量。
            /// </summary>
            public int EntityCount
            {
                get
                {
                    return m_Entities.Count;
                }
            }

            /// <summary>
            /// 获取或设置实体组实例对象池自动释放可释放对象的间隔秒数。
            /// </summary>
            public float InstanceAutoReleaseInterval
            {
                get
                {
                    return m_InstancePool.AutoReleaseInterval;
                }
                set
                {
                    m_InstancePool.AutoReleaseInterval = value;
                }
            }

            /// <summary>
            /// 获取或设置实体组实例对象池的容量。
            /// </summary>
            public int InstanceCapacity
            {
                get
                {
                    return m_InstancePool.Capacity;
                }
                set
                {
                    m_InstancePool.Capacity = value;
                }
            }

            /// <summary>
            /// 获取或设置实体组实例对象池对象过期秒数。
            /// </summary>
            public float InstanceExpireTime
            {
                get
                {
                    return m_InstancePool.ExpireTime;
                }
                set
                {
                    m_InstancePool.ExpireTime = value;
                }
            }

            /// <summary>
            /// 获取或设置实体组实例对象池的优先级。
            /// </summary>
            public int InstancePriority
            {
                get
                {
                    return m_InstancePool.Priority;
                }
                set
                {
                    m_InstancePool.Priority = value;
                }
            }

            /// <summary>
            /// 获取实体组辅助器。
            /// </summary>
            public IEntityGroupHelper Helper { get; }

            /// <summary>
            /// 实体组轮询。
            /// </summary>
            /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
            /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                LinkedListNode<IEntity> current = m_Entities.First;
                while (current != null)
                {
                    m_CachedNode = current.Next;
                    current.Value.OnUpdate(elapseSeconds, realElapseSeconds);
                    current = m_CachedNode;
                    m_CachedNode = null;
                }
            }

            /// <summary>
            /// 实体组中是否存在实体。
            /// </summary>
            /// <param name="entityId">实体序列编号。</param>
            /// <returns>实体组中是否存在实体。</returns>
            public bool HasEntity(int entityId)
            {
                foreach (IEntity entity in m_Entities)
                {
                    if (entity.Id == entityId)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// 实体组中是否存在实体。
            /// </summary>
            /// <param name="entityAssetName">实体资源名称。</param>
            /// <returns>实体组中是否存在实体。</returns>
            public bool HasEntity(string entityAssetName)
            {
                if (string.IsNullOrEmpty(entityAssetName))
                {
                    throw new FrameworkException("Entity asset name is invalid.");
                }

                foreach (IEntity entity in m_Entities)
                {
                    if (entity.EntityAssetName == entityAssetName)
                    {
                        return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// 从实体组中获取实体。
            /// </summary>
            /// <param name="entityId">实体序列编号。</param>
            /// <returns>要获取的实体。</returns>
            public IEntity GetEntity(int entityId)
            {
                foreach (IEntity entity in m_Entities)
                {
                    if (entity.Id == entityId)
                    {
                        return entity;
                    }
                }

                return null;
            }

            /// <summary>
            /// 从实体组中获取实体。
            /// </summary>
            /// <param name="entityAssetName">实体资源名称。</param>
            /// <returns>要获取的实体。</returns>
            public IEntity GetEntity(string entityAssetName)
            {
                if (string.IsNullOrEmpty(entityAssetName))
                {
                    throw new FrameworkException("Entity asset name is invalid.");
                }

                foreach (IEntity entity in m_Entities)
                {
                    if (entity.EntityAssetName == entityAssetName)
                    {
                        return entity;
                    }
                }

                return null;
            }

            /// <summary>
            /// 从实体组中获取实体。
            /// </summary>
            /// <param name="entityAssetName">实体资源名称。</param>
            /// <returns>要获取的实体。</returns>
            public IEntity[] GetEntities(string entityAssetName)
            {
                if (string.IsNullOrEmpty(entityAssetName))
                {
                    throw new FrameworkException("Entity asset name is invalid.");
                }

                List<IEntity> results = new List<IEntity>();
                foreach (IEntity entity in m_Entities)
                {
                    if (entity.EntityAssetName == entityAssetName)
                    {
                        results.Add(entity);
                    }
                }

                return results.ToArray();
            }

            /// <summary>
            /// 从实体组中获取实体。
            /// </summary>
            /// <param name="entityAssetName">实体资源名称。</param>
            /// <param name="results">要获取的实体；填充前清空，容量不足时仍会扩容。</param>
            public void GetEntitiesNonAlloc(string entityAssetName, List<IEntity> results)
            {
                if (string.IsNullOrEmpty(entityAssetName))
                {
                    throw new FrameworkException("Entity asset name is invalid.");
                }

                if (results == null)
                {
                    throw new FrameworkException("Results is invalid.");
                }

                results.Clear();
                foreach (IEntity entity in m_Entities)
                {
                    if (entity.EntityAssetName == entityAssetName)
                    {
                        results.Add(entity);
                    }
                }
            }

            /// <summary>
            /// 从实体组中获取所有实体。
            /// </summary>
            /// <returns>实体组中的所有实体。</returns>
            public IEntity[] GetAllEntities()
            {
                List<IEntity> results = new List<IEntity>();
                foreach (IEntity entity in m_Entities)
                {
                    results.Add(entity);
                }

                return results.ToArray();
            }

            /// <summary>
            /// 从实体组中获取所有实体。
            /// </summary>
            /// <param name="results">实体组中的所有实体；填充前清空，容量不足时仍会扩容。</param>
            public void GetAllEntitiesNonAlloc(List<IEntity> results)
            {
                if (results == null)
                {
                    throw new FrameworkException("Results is invalid.");
                }

                results.Clear();
                foreach (IEntity entity in m_Entities)
                {
                    results.Add(entity);
                }
            }

            /// <summary>
            /// 往实体组增加实体。
            /// </summary>
            /// <param name="entity">要增加的实体。</param>
            public void AddEntity(IEntity entity)
            {
                m_Entities.AddLast(entity);
            }

            /// <summary>
            /// 从实体组移除实体。
            /// </summary>
            /// <param name="entity">要移除的实体。</param>
            public void RemoveEntity(IEntity entity)
            {
                if (m_CachedNode != null && m_CachedNode.Value == entity)
                {
                    m_CachedNode = m_CachedNode.Next;
                }

                if (!m_Entities.Remove(entity))
                {
                    throw new FrameworkException(Utility.Text.Format("Entity group '{0}' not exists specified entity '[{1}]{2}'.", Name, entity.Id, entity.EntityAssetName));
                }
            }

            /// <summary>
            /// 向组内对象池登记实体实例。
            /// </summary>
            /// <param name="obj">要登记的实体实例条目。</param>
            /// <param name="spawned">登记时是否已经借出；为真时池会执行 OnSpawn。</param>
            public void RegisterEntityInstanceObject(EntityInstanceObject obj, bool spawned)
            {
                m_InstancePool.Register(obj, spawned);
            }

            /// <summary>
            /// 从组内对象池借用指定资产的实体实例。
            /// </summary>
            /// <param name="name">实体资产名称。</param>
            /// <returns>借出的实例条目；没有空闲实例时返回空。</returns>
            public EntityInstanceObject SpawnEntityInstanceObject(string name)
            {
                return m_InstancePool.Spawn(name);
            }

            /// <summary>
            /// 将实体实际使用的实例归还组内对象池。
            /// </summary>
            /// <param name="entity">要归还实例的实体。</param>
            public void UnspawnEntity(IEntity entity)
            {
                m_InstancePool.Unspawn(entity.Handle);
            }

            /// <summary>
            /// 设置实体实例的锁定状态。
            /// </summary>
            /// <param name="entityInstance">对象池实际管理的实体实例。</param>
            /// <param name="locked">是否锁定，锁定后不参与普通释放。</param>
            public void SetEntityInstanceLocked(object entityInstance, bool locked)
            {
                if (entityInstance == null)
                {
                    throw new FrameworkException("Entity instance is invalid.");
                }

                m_InstancePool.SetLocked(entityInstance, locked);
            }

            /// <summary>
            /// 设置实体实例在池中的回收优先级。
            /// </summary>
            /// <param name="entityInstance">对象池实际管理的实体实例。</param>
            /// <param name="priority">回收优先级。</param>
            public void SetEntityInstancePriority(object entityInstance, int priority)
            {
                if (entityInstance == null)
                {
                    throw new FrameworkException("Entity instance is invalid.");
                }

                m_InstancePool.SetPriority(entityInstance, priority);
            }
        }
    }
}
