//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        private sealed class PathInformationWindow : ScrollableDebuggerWindowBase
        {
            protected override void OnDrawScrollableWindow()
            {
                GUILayout.Label("<b>Path Information</b>");
                GUILayout.BeginVertical("box");
                {
                    DrawItem("Current Directory", Utility.Path.GetRegularPath(Environment.CurrentDirectory));
                    DrawItem("Data Path", Utility.Path.GetRegularPath(Application.dataPath));
                    DrawItem("Persistent Data Path", Utility.Path.GetRegularPath(Application.persistentDataPath));
                    DrawItem("Streaming Assets Path", Utility.Path.GetRegularPath(Application.streamingAssetsPath));
                    DrawItem("Temporary Cache Path", Utility.Path.GetRegularPath(Application.temporaryCachePath));
                    DrawItem("Console Log Path", Utility.Path.GetRegularPath(Application.consoleLogPath));
                }
                GUILayout.EndVertical();
            }
        }
    }
}
