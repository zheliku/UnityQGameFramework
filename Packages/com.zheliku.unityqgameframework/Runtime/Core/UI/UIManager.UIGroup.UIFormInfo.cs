//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.UI
{
    internal sealed partial class UIManager : FrameworkModule, IUIManager
    {
        private sealed partial class UIGroup : IUIGroup
        {
            /// <summary>
            /// 界面组界面信息。
            /// </summary>
            private sealed class UIFormInfo : IReference
            {
                /// <summary>
                /// 初始化界面状态记录。
                /// </summary>
                public UIFormInfo()
                {
                    UIForm = null;
                    Paused = false;
                    Covered = false;
                }

                /// <summary>
                /// 获取对应的界面。
                /// </summary>
                public IUIForm UIForm { get; private set; }

                /// <summary>
                /// 获取或设置界面是否已暂停。
                /// </summary>
                public bool Paused { get; set; }

                /// <summary>
                /// 获取或设置界面是否已被覆盖。
                /// </summary>
                public bool Covered { get; set; }

                /// <summary>
                /// 从引用池获取界面状态记录。
                /// </summary>
                /// <param name="uiForm">已打开的界面。</param>
                /// <returns>界面状态记录。</returns>
                public static UIFormInfo Create(IUIForm uiForm)
                {
                    if (uiForm == null)
                    {
                        throw new FrameworkException("UI form is invalid.");
                    }

                    UIFormInfo uiFormInfo = ReferencePool.Acquire<UIFormInfo>();
                    uiFormInfo.UIForm = uiForm;
                    uiFormInfo.Paused = true;
                    uiFormInfo.Covered = true;
                    return uiFormInfo;
                }

                /// <summary>
                /// 清理界面及其暂停、覆盖状态。
                /// </summary>
                public void Clear()
                {
                    UIForm = null;
                    Paused = false;
                    Covered = false;
                }
            }
        }
    }
}
