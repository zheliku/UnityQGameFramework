//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 滚动背景并提供显示、移动和出生边界。
    /// </summary>
    public class ScrollableBackground : MonoBehaviour
    {
        [SerializeField]
        private float m_ScrollSpeed = -0.25f;

        [SerializeField]
        private float m_TileSize = 30f;

        [SerializeField]
        private BoxCollider m_VisibleBoundary = null;

        [SerializeField]
        private BoxCollider m_PlayerMoveBoundary = null;

        [SerializeField]
        private BoxCollider m_EnemySpawnBoundary = null;

        private Transform m_CachedTransform = null;
        private Vector3 m_StartPosition = Vector3.zero;

        private void Start()
        {
            m_CachedTransform = transform;
            m_StartPosition = m_CachedTransform.position;
        }

        private void Update()
        {
            float newPosition = Mathf.Repeat(Time.time * m_ScrollSpeed, m_TileSize);
            m_CachedTransform.position = m_StartPosition + Vector3.forward * newPosition;
        }

        /// <summary>
        /// 获取实体可见边界。
        /// </summary>
        public BoxCollider VisibleBoundary
        {
            get
            {
                return m_VisibleBoundary;
            }
        }

        /// <summary>
        /// 获取玩家移动边界。
        /// </summary>
        public BoxCollider PlayerMoveBoundary
        {
            get
            {
                return m_PlayerMoveBoundary;
            }
        }

        /// <summary>
        /// 获取敌人出生边界。
        /// </summary>
        public BoxCollider EnemySpawnBoundary
        {
            get
            {
                return m_EnemySpawnBoundary;
            }
        }
    }
}
