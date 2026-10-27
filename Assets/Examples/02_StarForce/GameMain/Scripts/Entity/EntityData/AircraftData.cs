//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.DataTable;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 飞机及其推进器、武器和装甲数据。
    /// </summary>
    [Serializable]
    public abstract class AircraftData : TargetableObjectData
    {
        [SerializeField]
        private ThrusterData m_ThrusterData = null;

        [SerializeField]
        private List<WeaponData> m_WeaponDatas = new List<WeaponData>();

        [SerializeField]
        private List<ArmorData> m_ArmorDatas = new List<ArmorData>();

        [SerializeField]
        private int m_MaxHP = 0;

        [SerializeField]
        private int m_Defense = 0;

        [SerializeField]
        private int m_DeadEffectId = 0;

        [SerializeField]
        private int m_DeadSoundId = 0;

        /// <summary>
        /// 飞机及其推进器、武器和装甲数据。
        /// </summary>
        /// <param name="entityId">实体实例编号。</param>
        /// <param name="typeId">配置表中的实体类型编号。</param>
        /// <param name="camp">实体阵营。</param>
        public AircraftData(int entityId, int typeId, CampType camp)
            : base(entityId, typeId, camp)
        {
            var dtAircraft = GameEntry.DataTable.GetDataTable<TbAircraft>();
            DRAircraft drAircraft = dtAircraft.GetDataRow(TypeId);
            if (drAircraft == null)
            {
                return;
            }

            m_ThrusterData = new ThrusterData(GameEntry.Entity.GenerateSerialId(), drAircraft.ThrusterId, Id, Camp);

            for (int index = 0, weaponId = 0; (weaponId = drAircraft.GetWeaponIdAt(index)) > 0; index++)
            {
                AttachWeaponData(new WeaponData(GameEntry.Entity.GenerateSerialId(), weaponId, Id, Camp));
            }

            for (int index = 0, armorId = 0; (armorId = drAircraft.GetArmorIdAt(index)) > 0; index++)
            {
                AttachArmorData(new ArmorData(GameEntry.Entity.GenerateSerialId(), armorId, Id, Camp));
            }

            m_DeadEffectId = drAircraft.DeadEffectId;
            m_DeadSoundId = drAircraft.DeadSoundId;

            HP = m_MaxHP;
        }

        /// <summary>
        /// 最大生命。
        /// </summary>
        public override int MaxHP
        {
            get
            {
                return m_MaxHP;
            }
        }

        /// <summary>
        /// 防御。
        /// </summary>
        public int Defense
        {
            get
            {
                return m_Defense;
            }
        }

        /// <summary>
        /// 速度。
        /// </summary>
        public float Speed
        {
            get
            {
                return m_ThrusterData.Speed;
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

        /// <summary>
        /// 获取当前推进器数据。
        /// </summary>
        /// <returns>当前推进器数据。</returns>
        public ThrusterData GetThrusterData()
        {
            return m_ThrusterData;
        }

        /// <summary>
        /// 获取全部已附加武器数据。
        /// </summary>
        /// <returns>内部武器数据列表；通过附加和移除方法维护。</returns>
        public List<WeaponData> GetAllWeaponDatas()
        {
            return m_WeaponDatas;
        }

        /// <summary>
        /// 添加武器数据。
        /// </summary>
        /// <param name="weaponData">要附加的武器数据。</param>
        public void AttachWeaponData(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return;
            }

            if (m_WeaponDatas.Contains(weaponData))
            {
                return;
            }

            m_WeaponDatas.Add(weaponData);
        }

        /// <summary>
        /// 移除武器数据。
        /// </summary>
        /// <param name="weaponData">要移除的武器数据。</param>
        public void DetachWeaponData(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return;
            }

            m_WeaponDatas.Remove(weaponData);
        }

        /// <summary>
        /// 获取全部已附加装甲数据。
        /// </summary>
        /// <returns>内部装甲数据列表；通过附加和移除方法维护，以更新属性。</returns>
        public List<ArmorData> GetAllArmorDatas()
        {
            return m_ArmorDatas;
        }

        /// <summary>
        /// 添加装甲数据。
        /// </summary>
        /// <param name="armorData">要附加的装甲数据。</param>
        public void AttachArmorData(ArmorData armorData)
        {
            if (armorData == null)
            {
                return;
            }

            if (m_ArmorDatas.Contains(armorData))
            {
                return;
            }

            m_ArmorDatas.Add(armorData);
            RefreshData();
        }

        /// <summary>
        /// 移除装甲数据。
        /// </summary>
        /// <param name="armorData">要移除的装甲数据。</param>
        public void DetachArmorData(ArmorData armorData)
        {
            if (armorData == null)
            {
                return;
            }

            m_ArmorDatas.Remove(armorData);
            RefreshData();
        }

        private void RefreshData()
        {
            m_MaxHP = 0;
            m_Defense = 0;
            for (int i = 0; i < m_ArmorDatas.Count; i++)
            {
                m_MaxHP += m_ArmorDatas[i].MaxHP;
                m_Defense += m_ArmorDatas[i].Defense;
            }

            if (HP > m_MaxHP)
            {
                HP = m_MaxHP;
            }
        }
    }
}
