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
    /// 推进器的配置和归属数据。
    /// </summary>
    [Serializable]
    public class ThrusterData : AccessoryObjectData
    {
        [SerializeField]
        private float m_Speed = 0f;

        /// <summary>
        /// 推进器的配置和归属数据。
        /// </summary>
        /// <param name="entityId">实体实例编号。</param>
        /// <param name="typeId">配置表中的实体类型编号。</param>
        /// <param name="ownerId">所属实体编号。</param>
        /// <param name="ownerCamp">所属实体的阵营。</param>
        public ThrusterData(int entityId, int typeId, int ownerId, CampType ownerCamp)
            : base(entityId, typeId, ownerId, ownerCamp)
        {
            var dtThruster = GameEntry.DataTable.GetDataTable<TbThruster>();
            DRThruster drThruster = dtThruster.GetDataRow(TypeId);
            if (drThruster == null)
            {
                return;
            }

            m_Speed = drThruster.Speed;
        }

        /// <summary>
        /// 速度。
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
