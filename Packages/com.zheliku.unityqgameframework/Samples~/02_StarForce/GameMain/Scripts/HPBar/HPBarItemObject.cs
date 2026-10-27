//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Core.ObjectPool;
using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 血条物体的对象池登记条目。
    /// </summary>
    public class HPBarItemObject : ObjectBase
    {
        // Target 是场景里的 HPBarItem；这个 ObjectBase 只保存它在对象池中的登记信息。
        /// <summary>
        /// 创建血条对象池登记条目。
        /// </summary>
        /// <param name="target">对象池实际管理的血条物体。</param>
        /// <returns>包装指定 Target 的池条目。</returns>
        public static HPBarItemObject Create(object target)
        {
            // 登记条目本身用 ReferencePool 复用，血条对象由 ObjectPool 管理。
            HPBarItemObject hpBarItemObject = ReferencePool.Acquire<HPBarItemObject>();
            hpBarItemObject.Initialize(target);
            return hpBarItemObject;
        }

        /// <summary>
        /// 最终淘汰或关闭池时释放 Target，随后框架会将本包装条目归还引用池。
        /// </summary>
        /// <param name="isShutdown">是否是关闭对象池时触发。</param>
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
