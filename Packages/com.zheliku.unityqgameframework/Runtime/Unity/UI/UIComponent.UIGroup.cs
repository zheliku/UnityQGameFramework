//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class UIComponent : FrameworkComponent
    {
        [Serializable]
        private sealed class UIGroup
        {
            [SerializeField]
            private string m_Name = null;

            [SerializeField]
            private int m_Depth = 0;

            public string Name => m_Name;

            public int Depth => m_Depth;
        }
    }
}
