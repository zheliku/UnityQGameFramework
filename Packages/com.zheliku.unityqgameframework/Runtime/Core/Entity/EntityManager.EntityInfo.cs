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

            public EntityInfo()
            {
                Entity = null;
                Status = EntityStatus.Unknown;
                ParentEntity = null;
                m_ChildEntities = new List<IEntity>();
            }

            public IEntity Entity { get; private set; }

            public EntityStatus Status { get; set; }

            public IEntity ParentEntity { get; set; }

            public int ChildEntityCount => m_ChildEntities.Count;

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

            public void Clear()
            {
                Entity = null;
                Status = EntityStatus.Unknown;
                ParentEntity = null;
                m_ChildEntities.Clear();
            }

            public IEntity GetChildEntity()
            {
                return m_ChildEntities.Count > 0 ? m_ChildEntities[0] : null;
            }

            public IEntity[] GetChildEntities()
            {
                return m_ChildEntities.ToArray();
            }

            public void GetChildEntities(List<IEntity> results)
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

            public void AddChildEntity(IEntity childEntity)
            {
                if (m_ChildEntities.Contains(childEntity))
                {
                    throw new FrameworkException("Can not add child entity which is already exist.");
                }

                m_ChildEntities.Add(childEntity);
            }

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
