//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 小行星的生命值、速度和碰撞数据。
    /// </summary>
    [Serializable]
    public class AsteroidData : TargetableObjectData
    {
        [SerializeField]
        private int m_MaxHP = 0;

        [SerializeField]
        private int m_Attack = 0;

        [SerializeField]
        private float m_Speed = 0f;

        [SerializeField]
        private float m_AngularSpeed = 0f;

        [SerializeField]
        private int m_DeadEffectId = 0;

        [SerializeField]
        private int m_DeadSoundId = 0;

        /// <summary>
        /// 小行星的生命值、速度和碰撞数据。
        /// </summary>
        /// <param name="entityId">实体实例编号。</param>
        /// <param name="typeId">配置表中的实体类型编号。</param>
        public AsteroidData(int entityId, int typeId)
            : base(entityId, typeId, CampType.Neutral)
        {
            var dtAsteroid = GameEntry.DataTable.GetDataTable<TbAsteroid>();
            DRAsteroid drAsteroid = dtAsteroid.GetDataRow(TypeId);
            if (drAsteroid == null)
            {
                return;
            }

            HP = m_MaxHP = drAsteroid.MaxHP;
            m_Attack = drAsteroid.Attack;
            m_Speed = drAsteroid.Speed;
            m_AngularSpeed = drAsteroid.AngularSpeed;
            m_DeadEffectId = drAsteroid.DeadEffectId;
            m_DeadSoundId = drAsteroid.DeadSoundId;
        }

        /// <summary>
        /// 获取最大生命值。
        /// </summary>
        public override int MaxHP
        {
            get
            {
                return m_MaxHP;
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

        /// <summary>
        /// 获取角速度。
        /// </summary>
        public float AngularSpeed
        {
            get
            {
                return m_AngularSpeed;
            }
        }

        /// <summary>
        /// 获取死亡特效配置编号。
        /// </summary>
        public int DeadEffectId
        {
            get
            {
                return m_DeadEffectId;
            }
        }

        /// <summary>
        /// 获取死亡声音配置编号。
        /// </summary>
        public int DeadSoundId
        {
            get
            {
                return m_DeadSoundId;
            }
        }
    }
}
