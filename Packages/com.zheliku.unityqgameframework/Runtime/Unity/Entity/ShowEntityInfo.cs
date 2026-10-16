//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;

namespace UnityQGameFramework.Runtime
{
    internal sealed class ShowEntityInfo : IReference
    {
        public ShowEntityInfo()
        {
            EntityLogicType = null;
            UserData = null;
        }

        public Type EntityLogicType { get; private set; }

        public object UserData { get; private set; }

        public static ShowEntityInfo Create(Type entityLogicType, object userData)
        {
            ShowEntityInfo showEntityInfo = ReferencePool.Acquire<ShowEntityInfo>();
            showEntityInfo.EntityLogicType = entityLogicType;
            showEntityInfo.UserData = userData;
            return showEntityInfo;
        }

        public void Clear()
        {
            EntityLogicType = null;
            UserData = null;
        }
    }
}
