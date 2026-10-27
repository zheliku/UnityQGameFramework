//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 武器类。
    /// </summary>
    public class Weapon : Entity
    {
        private const string AttachPoint = "Weapon Point";

        [SerializeField]
        private WeaponData m_WeaponData = null;

        private float m_NextAttackTime = 0f;

        /// <summary>
        /// 初始化实例。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
        }

        /// <summary>
        /// 显示实体并接收业务数据。
        /// </summary>
        /// <param name="userData">显示时传入的武器数据，类型为 WeaponData。</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_WeaponData = userData as WeaponData;
            if (m_WeaponData == null)
            {
                Log.Error("Weapon data is invalid.");
                return;
            }

            // OwnerId 是场上的飞机实例 ID；AttachPoint 是该实例下的挂点路径。
            GameEntry.Entity.AttachEntity(Entity, m_WeaponData.OwnerId, AttachPoint);
        }

        /// <summary>
        /// 实体附加子实体。
        /// </summary>
        /// <param name="parentEntity">被附加的父实体。</param>
        /// <param name="parentTransform">被附加父实体的位置。</param>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnAttachTo(EntityLogic parentEntity, Transform parentTransform, object userData)
        {
            base.OnAttachTo(parentEntity, parentTransform, userData);

            Name = Utility.Text.Format("Weapon of {0}", parentEntity.Name);
            CachedTransform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 满足冷却条件时发射子弹。
        /// </summary>
        public void TryAttack()
        {
            if (Time.time < m_NextAttackTime)
            {
                return;
            }

            m_NextAttackTime = Time.time + m_WeaponData.AttackInterval;
            GameEntry.Entity.ShowBullet(new BulletData(
                GameEntry.Entity.GenerateSerialId(),
                m_WeaponData.BulletId,
                m_WeaponData.OwnerId,
                m_WeaponData.OwnerCamp,
                m_WeaponData.Attack,
                m_WeaponData.BulletSpeed)
            {
                Position = CachedTransform.position,
            });
            GameEntry.Sound.PlaySound(m_WeaponData.BulletSoundId);
        }
    }
}
