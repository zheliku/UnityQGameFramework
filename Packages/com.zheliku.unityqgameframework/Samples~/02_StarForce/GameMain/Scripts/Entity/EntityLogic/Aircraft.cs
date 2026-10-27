//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 战机类。
    /// </summary>
    public abstract class Aircraft : TargetableObject
    {
        [SerializeField]
        private AircraftData m_AircraftData = null;

        /// <summary>
        /// 当前推进器实体。
        /// </summary>
        [SerializeField]
        protected Thruster m_Thruster = null;

        /// <summary>
        /// 当前附加的武器实体。
        /// </summary>
        [SerializeField]
        protected List<Weapon> m_Weapons = new List<Weapon>();

        /// <summary>
        /// 当前附加的装甲实体。
        /// </summary>
        [SerializeField]
        protected List<Armor> m_Armors = new List<Armor>();

        /// <summary>
        /// 显示实体并接收业务数据。
        /// </summary>
        /// <param name="userData">显示时传入的飞机数据，类型为 AircraftData。</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_AircraftData = userData as AircraftData;
            if (m_AircraftData == null)
            {
                Log.Error("Aircraft data is invalid.");
                return;
            }

            Name = Utility.Text.Format("Aircraft ({0})", Id);

            // 部件先异步显示，再在各自 OnShow 中 AttachEntity，完成后通知本机的 OnAttached。
            GameEntry.Entity.ShowThruster(m_AircraftData.GetThrusterData());

            List<WeaponData> weaponDatas = m_AircraftData.GetAllWeaponDatas();
            for (int i = 0; i < weaponDatas.Count; i++)
            {
                GameEntry.Entity.ShowWeapon(weaponDatas[i]);
            }

            List<ArmorData> armorDatas = m_AircraftData.GetAllArmorDatas();
            for (int i = 0; i < armorDatas.Count; i++)
            {
                GameEntry.Entity.ShowArmor(armorDatas[i]);
            }
        }

        /// <summary>
        /// 隐藏实体并清理本次使用的状态。
        /// </summary>
        /// <param name="isShutdown">是否是关闭实体管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
        }

        /// <summary>
        /// 实体附加子实体。
        /// </summary>
        /// <param name="childEntity">附加的子实体。</param>
        /// <param name="parentTransform">被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnAttached(EntityLogic childEntity, Transform parentTransform, object userData)
        {
            base.OnAttached(childEntity, parentTransform, userData);

            if (childEntity is Thruster)
            {
                m_Thruster = (Thruster)childEntity;
                return;
            }

            if (childEntity is Weapon)
            {
                m_Weapons.Add((Weapon)childEntity);
                return;
            }

            if (childEntity is Armor)
            {
                m_Armors.Add((Armor)childEntity);
                return;
            }
        }

        /// <summary>
        /// 实体解除子实体。
        /// </summary>
        /// <param name="childEntity">解除的子实体。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnDetached(EntityLogic childEntity, object userData)
        {
            base.OnDetached(childEntity, userData);

            if (childEntity is Thruster)
            {
                m_Thruster = null;
                return;
            }

            if (childEntity is Weapon)
            {
                m_Weapons.Remove((Weapon)childEntity);
                return;
            }

            if (childEntity is Armor)
            {
                m_Armors.Remove((Armor)childEntity);
                return;
            }
        }

        /// <summary>
        /// 处理实体死亡效果与隐藏。
        /// </summary>
        /// <param name="attacker">造成伤害的实体。</param>
        protected override void OnDead(Entity attacker)
        {
            base.OnDead(attacker);

            GameEntry.Entity.ShowEffect(new EffectData(GameEntry.Entity.GenerateSerialId(), m_AircraftData.DeadEffectId)
            {
                Position = CachedTransform.localPosition,
            });
            GameEntry.Sound.PlaySound(m_AircraftData.DeadSoundId);
        }

        /// <summary>
        /// 获取本次碰撞结算数据。
        /// </summary>
        /// <returns>阵营、生命值、攻击和防御信息。</returns>
        public override ImpactData GetImpactData()
        {
            return new ImpactData(m_AircraftData.Camp, m_AircraftData.HP, 0, m_AircraftData.Defense);
        }
    }
}
