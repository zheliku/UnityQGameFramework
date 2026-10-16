//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;
using UnityEngine;

namespace StarForce
{
    public class HPBarItemObject : ObjectBase
    {
        public static HPBarItemObject Create(object target)
        {
            HPBarItemObject hpBarItemObject = ReferencePool.Acquire<HPBarItemObject>();
            hpBarItemObject.Initialize(target);
            return hpBarItemObject;
        }

        protected override void Release(bool isShutdown)
        {
            HPBarItem hpBarItem = (HPBarItem)Target;
            if (hpBarItem == null)
            {
                return;
            }

            Object.Destroy(hpBarItem.gameObject);
        }
    }
}
