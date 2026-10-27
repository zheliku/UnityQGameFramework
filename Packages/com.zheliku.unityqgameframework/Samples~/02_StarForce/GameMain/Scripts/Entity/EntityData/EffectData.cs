//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 特效类型和持续时间数据。
    /// </summary>
    [Serializable]
    public class EffectData : EntityData
    {
        [SerializeField]
        private float m_KeepTime = 0f;

        /// <summary>
        /// 特效类型和持续时间数据。
        /// </summary>
        /// <param name="entityId">实体实例编号。</param>
        /// <param name="typeId">配置表中的实体类型编号。</param>
        public EffectData(int entityId, int typeId)
            : base(entityId, typeId)
        {
            m_KeepTime = 3f;
        }

        /// <summary>
        /// 获取持续时间，以秒为单位。
        /// </summary>
        public float KeepTime
        {
            get
            {
                return m_KeepTime;
            }
        }
    }
}
