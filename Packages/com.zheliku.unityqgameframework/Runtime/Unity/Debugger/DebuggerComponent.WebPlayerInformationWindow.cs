//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        private sealed class WebPlayerInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Web Player Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Absolute URL", Application.absoluteURL);
                }
                GUILayout.EndVertical();
            }
        }
    }
}
