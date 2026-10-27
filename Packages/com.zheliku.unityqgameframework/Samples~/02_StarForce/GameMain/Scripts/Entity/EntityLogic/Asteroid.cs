//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 小行星类。
    /// </summary>
    public class Asteroid : TargetableObject
    {
        [SerializeField]
        private AsteroidData m_AsteroidData = null;

        private Vector3 m_RotateSphere = Vector3.zero;

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
        /// <param name="userData">显示时传入的小行星数据，类型为 AsteroidData。</param>
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_AsteroidData = userData as AsteroidData;
            if (m_AsteroidData == null)
            {
                Log.Error("Asteroid data is invalid.");
                return;
            }

            m_RotateSphere = Random.insideUnitSphere;
        }

        /// <summary>
        /// 更新实体状态。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            CachedTransform.Translate(Vector3.back * m_AsteroidData.Speed * elapseSeconds, Space.World);
            CachedTransform.Rotate(m_RotateSphere * m_AsteroidData.AngularSpeed * elapseSeconds, Space.Self);
        }

        /// <summary>
        /// 处理实体死亡效果与隐藏。
        /// </summary>
        /// <param name="attacker">造成伤害的实体。</param>
        protected override void OnDead(Entity attacker)
        {
            base.OnDead(attacker);

            GameEntry.Entity.ShowEffect(new EffectData(GameEntry.Entity.GenerateSerialId(), m_AsteroidData.DeadEffectId)
            {
                Position = CachedTransform.localPosition,
            });
            GameEntry.Sound.PlaySound(m_AsteroidData.DeadSoundId);
        }

        /// <summary>
        /// 获取本次碰撞结算数据。
        /// </summary>
        /// <returns>阵营、生命值、攻击和防御信息。</returns>
        public override ImpactData GetImpactData()
        {
            return new ImpactData(m_AsteroidData.Camp, m_AsteroidData.HP, m_AsteroidData.Attack, 0);
        }
    }
}
