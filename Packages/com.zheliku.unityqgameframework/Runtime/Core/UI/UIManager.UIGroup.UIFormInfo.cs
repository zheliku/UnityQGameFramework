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
                public UIFormInfo()
                {
                    UIForm = null;
                    Paused = false;
                    Covered = false;
                }

                public IUIForm UIForm { get; private set; }

                public bool Paused { get; set; }

                public bool Covered { get; set; }

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
