//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 子弹的归属、阵营、攻击和速度数据。
    /// </summary>
    [Serializable]
    public class BulletData : EntityData
    {
        [SerializeField]
        private int m_OwnerId = 0;

        [SerializeField]
        private CampType m_OwnerCamp = CampType.Unknown;

        [SerializeField]
        private int m_Attack = 0;

        [SerializeField]
        private float m_Speed = 0f;

        /// <summary>
        /// 子弹的归属、阵营、攻击和速度数据。
        /// </summary>
        /// <param name="entityId">实体实例编号。</param>
        /// <param name="typeId">配置表中的实体类型编号。</param>
        /// <param name="ownerId">所属实体编号。</param>
        /// <param name="ownerCamp">所属实体的阵营。</param>
        /// <param name="attack">攻击力。</param>
        /// <param name="speed">移动速度。</param>
        public BulletData(int entityId, int typeId, int ownerId, CampType ownerCamp, int attack, float speed)
            : base(entityId, typeId)
        {
            m_OwnerId = ownerId;
            m_OwnerCamp = ownerCamp;
            m_Attack = attack;
            m_Speed = speed;
        }

        /// <summary>
        /// 获取所属实体编号。
        /// </summary>
        public int OwnerId
        {
            get
            {
                return m_OwnerId;
            }
        }

        /// <summary>
        /// 获取所属实体的阵营。
        /// </summary>
        public CampType OwnerCamp
        {
            get
            {
                return m_OwnerCamp;
            }
        }

        /// <summary>
        /// 获取攻击力。
        /// </summary>
        public int Attack
        {
            get
            {
                return m_Attack;
            }
        }

        /// <summary>
        /// 获取移动速度。
        /// </summary>
        public float Speed
        {
            get
            {
                return m_Speed;
            }
        }
    }
}
