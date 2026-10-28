//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------


namespace UnityQGameFramework.Core
{
    internal sealed partial class EntityManager : GameFrameworkModule, IEntityManager
    {
        /// <summary>
        /// 实体实例对象。
        /// </summary>
        private sealed class EntityInstanceObject : ObjectBase
        {
            private object m_EntityAsset;
            private IEntityHelper m_EntityHelper;

            /// <summary>
            /// 初始化实体实例的池条目。
            /// </summary>
            public EntityInstanceObject()
            {
                m_EntityAsset = null;
                m_EntityHelper = null;
            }

            /// <summary>
            /// 包装实体实例，并保存其预制体借用和释放辅助器。
            /// </summary>
            /// <param name="name">实体资产名称。</param>
            /// <param name="entityAsset">保持借用的实体预制体资产。</param>
            /// <param name="entityInstance">对象池实际管理的实体实例。</param>
            /// <param name="entityHelper">负责创建和释放实例的辅助器。</param>
            /// <returns>尚未登记到对象池的实体实例条目。</returns>
            public static EntityInstanceObject Create(string name, object entityAsset, object entityInstance, IEntityHelper entityHelper)
            {
                if (entityAsset == null)
                {
                    throw new GameFrameworkException("Entity asset is invalid.");
                }

                if (entityHelper == null)
                {
                    throw new GameFrameworkException("Entity helper is invalid.");
                }

                EntityInstanceObject entityInstanceObject = ReferencePool.Acquire<EntityInstanceObject>();
                entityInstanceObject.Initialize(name, entityInstance);
                entityInstanceObject.m_EntityAsset = entityAsset;
                entityInstanceObject.m_EntityHelper = entityHelper;
                return entityInstanceObject;
            }

            /// <summary>
            /// 清除条目引用，供引用池复用。
            /// </summary>
            public override void Clear()
            {
                base.Clear();
                m_EntityAsset = null;
                m_EntityHelper = null;
            }

            /// <summary>
            /// 通过辅助器销毁实体实例并归还预制体借用。
            /// </summary>
            /// <param name="isShutdown">是否正在关闭实体管理器。</param>
            protected internal override void Release(bool isShutdown)
            {
                m_EntityHelper.ReleaseEntity(m_EntityAsset, Target);
            }
        }
    }
}
