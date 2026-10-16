//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    internal sealed class AttachEntityInfo : IReference
    {
        public AttachEntityInfo()
        {
            ParentTransform = null;
            UserData = null;
        }

        public Transform ParentTransform { get; private set; }

        public object UserData { get; private set; }

        public static AttachEntityInfo Create(Transform parentTransform, object userData)
        {
            AttachEntityInfo attachEntityInfo = ReferencePool.Acquire<AttachEntityInfo>();
            attachEntityInfo.ParentTransform = parentTransform;
            attachEntityInfo.UserData = userData;
            return attachEntityInfo;
        }

        public void Clear()
        {
            ParentTransform = null;
            UserData = null;
        }
    }
}
