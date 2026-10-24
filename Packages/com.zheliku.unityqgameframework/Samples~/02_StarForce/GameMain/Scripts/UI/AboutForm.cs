//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    public class AboutForm : UGuiForm
    {
        [SerializeField]
        private RectTransform m_Transform = null;

        [SerializeField]
        private float m_ScrollSpeed = 1f;

        private float m_InitPosition = 0f;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);

            CanvasScaler canvasScaler = GetComponentInParent<CanvasScaler>();
            if (canvasScaler == null)
            {
                Log.Warning("Can not find CanvasScaler component.");
                return;
            }

            m_InitPosition = -0.5f * canvasScaler.referenceResolution.x * Screen.height / Screen.width;
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            m_Transform.SetLocalPositionY(m_InitPosition);

            // 换个音乐
            GameEntry.Sound.PlayMusic(3);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);

            // 正常关闭时恢复菜单音乐；框架关闭过程中不再访问表和声音模块。
            if (!isShutdown) GameEntry.Sound.PlayMusic(1);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            m_Transform.AddLocalPositionY(m_ScrollSpeed * elapseSeconds);
            if (m_Transform.localPosition.y > m_Transform.sizeDelta.y - m_InitPosition)
            {
                m_Transform.SetLocalPositionY(m_InitPosition);
            }
        }
    }
}
