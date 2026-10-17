//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.ObjectPool;
using System.Collections.Generic;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    public class HPBarComponent : FrameworkComponent
    {
        [SerializeField]
        private HPBarItem m_HPBarItemTemplate = null;

        [SerializeField]
        private Transform m_HPBarInstanceRoot = null;

        [SerializeField]
        private int m_InstancePoolCapacity = 16;

        private IObjectPool<HPBarItemObject> m_HPBarItemObjectPool = null;
        private readonly List<HPBarItem> m_ActiveHPBarItems = new List<HPBarItem>();
        private Canvas m_CachedCanvas = null;

        private void Start()
        {
            if (m_HPBarInstanceRoot == null || m_HPBarItemTemplate == null)
            {
                enabled = false;
                Log.Error("Assign the HP bar template and instance root.");
                return;
            }

            m_CachedCanvas = m_HPBarInstanceRoot.GetComponent<Canvas>();
            var objectPool = UnityQGameFramework.Runtime.GameEntry.GetComponent<ObjectPoolComponent>();
            if (m_CachedCanvas == null || objectPool == null)
            {
                enabled = false;
                Log.Error("HP bars require a Canvas on the instance root and an ObjectPool component.");
                return;
            }

            // Framework components register in Awake; the sample entry caches them in Start.
            m_HPBarItemObjectPool = objectPool.CreateSingleSpawnObjectPool<HPBarItemObject>("HPBarItem", m_InstancePoolCapacity);
        }

        private void Update()
        {
            for (int i = m_ActiveHPBarItems.Count - 1; i >= 0; i--)
            {
                HPBarItem hpBarItem = m_ActiveHPBarItems[i];
                if (hpBarItem.Refresh())
                {
                    continue;
                }

                HideHPBar(hpBarItem);
            }
        }

        public void ShowHPBar(Entity entity, float fromHPRatio, float toHPRatio)
        {
            if (!enabled || m_HPBarItemObjectPool == null) return;
            if (entity == null)
            {
                Log.Warning("Entity is invalid.");
                return;
            }

            HPBarItem hpBarItem = GetActiveHPBarItem(entity);
            if (hpBarItem == null)
            {
                hpBarItem = CreateHPBarItem(entity);
                m_ActiveHPBarItems.Add(hpBarItem);
            }

            hpBarItem.Init(entity, m_CachedCanvas, fromHPRatio, toHPRatio);
        }

        private void HideHPBar(HPBarItem hpBarItem)
        {
            hpBarItem.Reset();
            m_ActiveHPBarItems.Remove(hpBarItem);
            m_HPBarItemObjectPool.Unspawn(hpBarItem);
        }

        private HPBarItem GetActiveHPBarItem(Entity entity)
        {
            if (entity == null)
            {
                return null;
            }

            for (int i = 0; i < m_ActiveHPBarItems.Count; i++)
            {
                if (m_ActiveHPBarItems[i].Owner == entity)
                {
                    return m_ActiveHPBarItems[i];
                }
            }

            return null;
        }

        private HPBarItem CreateHPBarItem(Entity entity)
        {
            HPBarItem hpBarItem = null;
            HPBarItemObject hpBarItemObject = m_HPBarItemObjectPool.Spawn();
            if (hpBarItemObject != null)
            {
                hpBarItem = (HPBarItem)hpBarItemObject.Target;
            }
            else
            {
                hpBarItem = Instantiate(m_HPBarItemTemplate);
                Transform transform = hpBarItem.GetComponent<Transform>();
                transform.SetParent(m_HPBarInstanceRoot);
                transform.localScale = Vector3.one;
                m_HPBarItemObjectPool.Register(HPBarItemObject.Create(hpBarItem), true);
            }

            return hpBarItem;
        }
    }
}
