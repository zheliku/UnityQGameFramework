//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class EntityManager : GameFrameworkModule, IEntityManager
    {
        /// <summary>
        /// 等待实体显示时保存的请求信息。
        /// </summary>
        private sealed class ShowEntityInfo : IReference
        {
            /// <summary>
            /// 初始化实体显示请求。
            /// </summary>
            public ShowEntityInfo()
            {
                SerialId = 0;
                EntityId = 0;
                EntityGroup = null;
                UserData = null;
            }

            /// <summary>
            /// 获取异步显示请求序号。
            /// </summary>
            public int SerialId
            {
                get; private set;
            }

            /// <summary>
            /// 获取实体实例编号。
            /// </summary>
            public int EntityId
            {
                get; private set;
            }

            /// <summary>
            /// 获取目标实体组。
            /// </summary>
            public EntityGroup EntityGroup
            {
                get; private set;
            }

            /// <summary>
            /// 获取调用方传入的业务数据。
            /// </summary>
            public object UserData
            {
                get; private set;
            }

            /// <summary>
            /// 从引用池取得并填写异步显示请求。
            /// </summary>
            /// <param name="serialId">此次异步显示请求的序号。</param>
            /// <param name="entityId">实体实例编号。</param>
            /// <param name="entityGroup">实体所属的组。</param>
            /// <param name="userData">显示实体时传入的数据。</param>
            /// <returns>实体显示请求信息。</returns>
            public static ShowEntityInfo Create(int serialId, int entityId, EntityGroup entityGroup, object userData)
            {
                ShowEntityInfo showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
                showEntityInfo.SerialId = serialId;
                showEntityInfo.EntityId = entityId;
                showEntityInfo.EntityGroup = entityGroup;
                showEntityInfo.UserData = userData;
                return showEntityInfo;
            }

            /// <summary>
            /// 重置请求记录，供引用池复用。
            /// </summary>
            public void Clear()
            {
                SerialId = 0;
                EntityId = 0;
                EntityGroup = null;
                UserData = null;
            }
        }
    }
}
