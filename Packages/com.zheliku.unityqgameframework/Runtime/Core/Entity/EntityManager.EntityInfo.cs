//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Collections.Generic;

namespace UnityQGameFramework.Core.Entity
{
    internal sealed partial class EntityManager : FrameworkModule, IEntityManager
    {
        /// <summary>
        /// 实体信息。
        /// </summary>
        private sealed class EntityInfo : IReference
        {
            private List<IEntity> m_ChildEntities;

            /// <summary>
            /// 初始化可复用的实体关系记录。
            /// </summary>
            public EntityInfo()
            {
                Entity = null;
                Status = EntityStatus.Unknown;
                ParentEntity = null;
                m_ChildEntities = new List<IEntity>();
            }

            /// <summary>
            /// 获取当前实体。
            /// </summary>
            public IEntity Entity { get; private set; }

            /// <summary>
            /// 获取或设置实体生命周期状态。
            /// </summary>
            public EntityStatus Status { get; set; }

            /// <summary>
            /// 获取或设置父实体。
            /// </summary>
            public IEntity ParentEntity { get; set; }

            /// <summary>
            /// 获取当前子实体数量。
            /// </summary>
            public int ChildEntityCount
            {
                get
                {
                    return m_ChildEntities.Count;
                }
            }

            /// <summary>
            /// 从引用池取得记录并绑定实体。
            /// </summary>
            /// <param name="entity">要记录的实体。</param>
            /// <returns>持有指定实体的关系记录。</returns>
            public static EntityInfo Create(IEntity entity)
            {
                if (entity == null)
                {
                    throw new FrameworkException("Entity is invalid.");
                }

                EntityInfo entityInfo = ReferencePool.Acquire<EntityInfo>();
                entityInfo.Entity = entity;
                entityInfo.Status = EntityStatus.WillInit;
                return entityInfo;
            }

            /// <summary>
            /// 清除实体、父子关系和状态，供引用池复用。
            /// </summary>
            public void Clear()
            {
                Entity = null;
                Status = EntityStatus.Unknown;
                ParentEntity = null;
                m_ChildEntities.Clear();
            }

            /// <summary>
            /// 获取第一个子实体。
            /// </summary>
            /// <returns>第一个子实体；没有子实体时返回空。</returns>
            public IEntity GetChildEntity()
            {
                return m_ChildEntities.Count > 0 ? m_ChildEntities[0] : null;
            }

            /// <summary>
            /// 获取全部子实体。
            /// </summary>
            /// <returns>当前子实体的数组副本。</returns>
            public IEntity[] GetChildEntities()
            {
                return m_ChildEntities.ToArray();
            }

            /// <summary>
            /// 获取全部子实体。
            /// </summary>
            /// <param name="results">接收子实体的列表，填充前会清空；填充前清空，容量不足时仍会扩容。</param>
            public void GetChildEntitiesNonAlloc(List<IEntity> results)
            {
                if (results == null)
                {
                    throw new FrameworkException("Results is invalid.");
                }

                results.Clear();
                foreach (IEntity childEntity in m_ChildEntities)
                {
                    results.Add(childEntity);
                }
            }

            /// <summary>
            /// 登记子实体关系，拒绝重复登记。
            /// </summary>
            /// <param name="childEntity">要添加的子实体。</param>
            public void AddChildEntity(IEntity childEntity)
            {
                if (m_ChildEntities.Contains(childEntity))
                {
                    throw new FrameworkException("Can not add child entity which is already exist.");
                }

                m_ChildEntities.Add(childEntity);
            }

            /// <summary>
            /// 移除已登记的子实体关系。
            /// </summary>
            /// <param name="childEntity">要移除的子实体。</param>
            public void RemoveChildEntity(IEntity childEntity)
            {
                if (!m_ChildEntities.Remove(childEntity))
                {
                    throw new FrameworkException("Can not remove child entity which is not exist.");
                }
            }
        }
    }
}
