//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Entity
{
    internal sealed partial class EntityManager : FrameworkModule, IEntityManager
    {
        private sealed class ShowEntityInfo : IReference
        {
            public ShowEntityInfo()
            {
                SerialId = 0;
                EntityId = 0;
                EntityGroup = null;
                UserData = null;
            }

            public int SerialId { get; private set; }

            public int EntityId { get; private set; }

            public EntityGroup EntityGroup { get; private set; }

            public object UserData { get; private set; }

            public static ShowEntityInfo Create(int serialId, int entityId, EntityGroup entityGroup, object userData)
            {
                ShowEntityInfo showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
                showEntityInfo.SerialId = serialId;
                showEntityInfo.EntityId = entityId;
                showEntityInfo.EntityGroup = entityGroup;
                showEntityInfo.UserData = userData;
                return showEntityInfo;
            }

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
