//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------


namespace UnityQGameFramework.Core
{
    internal sealed partial class UIManager : GameFrameworkModule, IUIManager
    {
        /// <summary>
        /// 界面实例对象。
        /// </summary>
        private sealed class UIFormInstanceObject : ObjectBase
        {
            private object m_UIFormAsset;
            private IUIFormHelper m_UIFormHelper;

            /// <summary>
            /// 初始化界面实例的池内登记条目。
            /// </summary>
            public UIFormInstanceObject()
            {
                m_UIFormAsset = null;
                m_UIFormHelper = null;
            }

            /// <summary>
            /// 从引用池获取界面实例登记条目。
            /// </summary>
            /// <param name="name">界面资源名称。</param>
            /// <param name="uiFormAsset">界面资源。</param>
            /// <param name="uiFormInstance">被池管理的实际界面实例。</param>
            /// <param name="uiFormHelper">界面辅助器。</param>
            /// <returns>界面实例的池内登记条目。</returns>
            public static UIFormInstanceObject Create(string name, object uiFormAsset, object uiFormInstance, IUIFormHelper uiFormHelper)
            {
                if (uiFormAsset == null)
                {
                    throw new GameFrameworkException("UI form asset is invalid.");
                }

                if (uiFormHelper == null)
                {
                    throw new GameFrameworkException("UI form helper is invalid.");
                }

                UIFormInstanceObject uiFormInstanceObject = ReferencePool.Acquire<UIFormInstanceObject>();
                uiFormInstanceObject.Initialize(name, uiFormInstance);
                uiFormInstanceObject.m_UIFormAsset = uiFormAsset;
                uiFormInstanceObject.m_UIFormHelper = uiFormHelper;
                return uiFormInstanceObject;
            }

            /// <summary>
            /// 清理界面实例登记数据。
            /// </summary>
            public override void Clear()
            {
                base.Clear();
                m_UIFormAsset = null;
                m_UIFormHelper = null;
            }

            /// <summary>
            /// 释放界面实例及其资源。
            /// </summary>
            /// <param name="isShutdown">是否正在关闭框架。</param>
            protected internal override void Release(bool isShutdown)
            {
                m_UIFormHelper.ReleaseUIForm(m_UIFormAsset, Target);
            }
        }
    }
}
