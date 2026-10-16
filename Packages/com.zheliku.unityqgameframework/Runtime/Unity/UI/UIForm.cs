//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core.UI;
using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 界面。
    /// </summary>
    public sealed class UIForm : MonoBehaviour, IUIForm
    {
        /// <summary>
        /// 获取界面序列编号。
        /// </summary>
        public int SerialId { get; private set; }

        /// <summary>
        /// 获取界面资源名称。
        /// </summary>
        public string UIFormAssetName { get; private set; }

        /// <summary>
        /// 获取界面实例。
        /// </summary>
        public object Handle => gameObject;

        /// <summary>
        /// 获取界面所属的界面组。
        /// </summary>
        public IUIGroup UIGroup { get; private set; }

        /// <summary>
        /// 获取界面深度。
        /// </summary>
        public int DepthInUIGroup { get; private set; }

        /// <summary>
        /// 获取是否暂停被覆盖的界面。
        /// </summary>
        public bool PauseCoveredUIForm { get; private set; }

        /// <summary>
        /// 获取界面逻辑。
        /// </summary>
        public UIFormLogic Logic { get; private set; }

        /// <summary>
        /// 初始化界面。
        /// </summary>
        /// <param name="serialId">界面序列编号。</param>
        /// <param name="uiFormAssetName">界面资源名称。</param>
        /// <param name="uiGroup">界面所处的界面组。</param>
        /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
        /// <param name="isNewInstance">是否是新实例。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnInit(int serialId, string uiFormAssetName, IUIGroup uiGroup, bool pauseCoveredUIForm, bool isNewInstance, object userData)
        {
            SerialId = serialId;
            UIFormAssetName = uiFormAssetName;
            UIGroup = uiGroup;
            DepthInUIGroup = 0;
            PauseCoveredUIForm = pauseCoveredUIForm;

            if (!isNewInstance)
            {
                return;
            }

            Logic = GetComponent<UIFormLogic>();
            if (Logic == null)
            {
                Log.Error("UI form '{0}' can not get UI form logic.", uiFormAssetName);
                return;
            }

            try
            {
                Logic.OnInit(userData);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnInit with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面回收。
        /// </summary>
        public void OnRecycle()
        {
            try
            {
                Logic.OnRecycle();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnRecycle with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }

            SerialId = 0;
            DepthInUIGroup = 0;
            PauseCoveredUIForm = true;
        }

        /// <summary>
        /// 界面打开。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void OnOpen(object userData)
        {
            try
            {
                Logic.OnOpen(userData);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnOpen with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面关闭。
        /// </summary>
        /// <param name="isShutdown">是否是关闭界面管理器时触发。</param>
        /// <param name="userData">用户自定义数据。</param>
        public void OnClose(bool isShutdown, object userData)
        {
            try
            {
                Logic.OnClose(isShutdown, userData);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnClose with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面暂停。
        /// </summary>
        public void OnPause()
        {
            try
            {
                Logic.OnPause();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnPause with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面暂停恢复。
        /// </summary>
        public void OnResume()
        {
            try
            {
                Logic.OnResume();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnResume with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面遮挡。
        /// </summary>
        public void OnCover()
        {
            try
            {
                Logic.OnCover();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnCover with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面遮挡恢复。
        /// </summary>
        public void OnReveal()
        {
            try
            {
                Logic.OnReveal();
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnReveal with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面激活。
        /// </summary>
        /// <param name="userData">用户自定义数据。</param>
        public void OnRefocus(object userData)
        {
            try
            {
                Logic.OnRefocus(userData);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnRefocus with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面轮询。
        /// </summary>
        /// <param name="elapseSeconds">逻辑流逝时间，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实流逝时间，以秒为单位。</param>
        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            try
            {
                Logic.OnUpdate(elapseSeconds, realElapseSeconds);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnUpdate with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }

        /// <summary>
        /// 界面深度改变。
        /// </summary>
        /// <param name="uiGroupDepth">界面组深度。</param>
        /// <param name="depthInUIGroup">界面在界面组中的深度。</param>
        public void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            DepthInUIGroup = depthInUIGroup;
            try
            {
                Logic.OnDepthChanged(uiGroupDepth, depthInUIGroup);
            }
            catch (Exception exception)
            {
                Log.Error("UI form '[{0}]{1}' OnDepthChanged with exception '{2}'.", SerialId, UIFormAssetName, exception);
            }
        }
    }
}
