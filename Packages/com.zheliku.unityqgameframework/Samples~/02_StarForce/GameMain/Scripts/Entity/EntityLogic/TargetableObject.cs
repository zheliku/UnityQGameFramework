//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 可作为目标的实体类。
    /// </summary>
    public abstract class TargetableObject : Entity
    {
        [SerializeField]
        private TargetableObjectData m_TargetableObjectData = null;

        /// <summary>
        /// 获取生命值是否已耗尽。
        /// </summary>
        public bool IsDead
        {
            get
            {
                return m_TargetableObjectData.HP <= 0;
            }
        }

        /// <summary>
        /// 获取本次碰撞结算数据。
        /// </summary>
        /// <returns>阵营、生命值、攻击和防御信息。</returns>
        public abstract ImpactData GetImpactData();

        /// <summary>
        /// 扣除生命值并在耗尽时触发死亡。
        /// </summary>
        /// <param name="attacker">造成伤害的实体。</param>
        /// <param name="damageHP">本次扣除的生命值。</param>
        public void ApplyDamage(Entity attacker, int damageHP)
        {
            float fromHPRatio = m_TargetableObjectData.HPRatio;
            m_TargetableObjectData.HP -= damageHP;
            float toHPRatio = m_TargetableObjectData.HPRatio;
            if (fromHPRatio > toHPRatio)
            {
                GameEntry.HPBar.ShowHPBar(this, fromHPRatio, toHPRatio);
            }

            if (m_TargetableObjectData.HP <= 0)
            {
                OnDead(attacker);
            }
        }

        /// <summary>
        /// 初始化实例。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            gameObject.SetLayerRecursively(Constant.Layer.DefaultLayerId);
        }

        /// <summary>
        /// 显示实体并接收业务数据。
        /// </summary>
        /// <param name="userData">显示时传入的可攻击实体数据，类型为 TargetableObjectData。</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_TargetableObjectData = userData as TargetableObjectData;
            if (m_TargetableObjectData == null)
            {
                Log.Error("Targetable object data is invalid.");
                return;
            }
        }

        /// <summary>
        /// 处理实体死亡效果与隐藏。
        /// </summary>
        /// <param name="attacker">造成伤害的实体。</param>
        protected virtual void OnDead(Entity attacker)
        {
            GameEntry.Entity.HideEntity(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            Entity entity = other.gameObject.GetComponent<Entity>();
            if (entity == null)
            {
                return;
            }

            if (entity is TargetableObject && entity.Id >= Id)
            {
                // 碰撞事件由 Id 小的一方处理，避免重复处理
                return;
            }

            AIUtility.PerformCollision(this, entity);
        }
    }
}
