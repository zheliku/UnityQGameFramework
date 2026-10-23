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
        // Target 是场景里的 HPBarItem；这个 ObjectBase 只保存它在对象池中的登记信息。
        public static HPBarItemObject Create(object target)
        {
            // 登记条目本身用 ReferencePool 复用，血条对象由 ObjectPool 管理。
            HPBarItemObject hpBarItemObject = ReferencePool.Acquire<HPBarItemObject>();
            hpBarItemObject.Initialize(target);
            return hpBarItemObject;
        }

        protected override void Release(bool isShutdown)
        {
            // Unspawn 只归还借用；对象池真正释放条目时才销毁血条。
            HPBarItem hpBarItem = (HPBarItem)Target;
            if (hpBarItem == null)
            {
                return;
            }

            Object.Destroy(hpBarItem.gameObject);
        }
    }
}
