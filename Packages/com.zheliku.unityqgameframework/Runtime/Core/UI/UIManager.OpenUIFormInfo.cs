//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core
{
    internal sealed partial class UIManager : GameFrameworkModule, IUIManager
    {
        /// <summary>
        /// 保存打开界面请求的参数。
        /// </summary>
        private sealed class OpenUIFormInfo : IReference
        {
            /// <summary>
            /// 初始化界面打开请求。
            /// </summary>
            public OpenUIFormInfo()
            {
                SerialId = 0;
                UIGroup = null;
                PauseCoveredUIForm = false;
                UserData = null;
            }

            /// <summary>
            /// 获取界面序列编号。
            /// </summary>
            public int SerialId
            {
                get; private set;
            }

            /// <summary>
            /// 获取目标界面组。
            /// </summary>
            public UIGroup UIGroup
            {
                get; private set;
            }

            /// <summary>
            /// 获取是否暂停被覆盖的界面。
            /// </summary>
            public bool PauseCoveredUIForm
            {
                get; private set;
            }

            /// <summary>
            /// 获取用户自定义数据。
            /// </summary>
            public object UserData
            {
                get; private set;
            }

            /// <summary>
            /// 从引用池获取界面打开请求。
            /// </summary>
            /// <param name="serialId">界面序列编号。</param>
            /// <param name="uiGroup">目标界面组。</param>
            /// <param name="pauseCoveredUIForm">是否暂停被覆盖的界面。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>界面打开请求。</returns>
            public static OpenUIFormInfo Create(int serialId, UIGroup uiGroup, bool pauseCoveredUIForm, object userData)
            {
                OpenUIFormInfo openUIFormInfo = ReferencePool.Acquire<OpenUIFormInfo>();
                openUIFormInfo.SerialId = serialId;
                openUIFormInfo.UIGroup = uiGroup;
                openUIFormInfo.PauseCoveredUIForm = pauseCoveredUIForm;
                openUIFormInfo.UserData = userData;
                return openUIFormInfo;
            }

            /// <summary>
            /// 清理界面打开请求数据。
            /// </summary>
            public void Clear()
            {
                SerialId = 0;
                UIGroup = null;
                PauseCoveredUIForm = false;
                UserData = null;
            }
        }
    }
}
