//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    internal sealed class WWWFormInfo : IReference
    {
        public WWWFormInfo()
        {
            WWWForm = null;
            UserData = null;
        }

        public WWWForm WWWForm { get; private set; }

        public object UserData { get; private set; }

        public static WWWFormInfo Create(WWWForm wwwForm, object userData)
        {
            WWWFormInfo wwwFormInfo = ReferencePool.Acquire<WWWFormInfo>();
            wwwFormInfo.WWWForm = wwwForm;
            wwwFormInfo.UserData = userData;
            return wwwFormInfo;
        }

        public void Clear()
        {
            WWWForm = null;
            UserData = null;
        }
    }
}
