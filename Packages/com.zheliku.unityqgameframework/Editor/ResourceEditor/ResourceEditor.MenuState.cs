//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityEditor;

namespace UnityQGameFramework.Editor.ResourceTools
{
    internal sealed partial class ResourceEditor : EditorWindow
    {
        private enum MenuState : byte
        {
            Normal,
            Add,
            Rename,
            Remove,
        }
    }
}
