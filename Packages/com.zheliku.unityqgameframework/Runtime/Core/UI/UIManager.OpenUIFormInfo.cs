//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.UI
{
    internal sealed partial class UIManager : FrameworkModule, IUIManager
    {
        private sealed class OpenUIFormInfo : IReference
        {
            public OpenUIFormInfo()
            {
                SerialId = 0;
                UIGroup = null;
                PauseCoveredUIForm = false;
                UserData = null;
            }

            public int SerialId { get; private set; }

            public UIGroup UIGroup { get; private set; }

            public bool PauseCoveredUIForm { get; private set; }

            public object UserData { get; private set; }

            public static OpenUIFormInfo Create(int serialId, UIGroup uiGroup, bool pauseCoveredUIForm, object userData)
            {
                OpenUIFormInfo openUIFormInfo = ReferencePool.Acquire<OpenUIFormInfo>();
                openUIFormInfo.SerialId = serialId;
                openUIFormInfo.UIGroup = uiGroup;
                openUIFormInfo.PauseCoveredUIForm = pauseCoveredUIForm;
                openUIFormInfo.UserData = userData;
                return openUIFormInfo;
            }

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
