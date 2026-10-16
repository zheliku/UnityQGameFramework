//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace UnityQGameFramework.Core.Config
{
    internal sealed partial class ConfigManager : FrameworkModule, IConfigManager
    {
        [StructLayout(LayoutKind.Auto)]
        private struct ConfigData
        {
            public ConfigData(bool boolValue, int intValue, float floatValue, string stringValue)
            {
                BoolValue = boolValue;
                IntValue = intValue;
                FloatValue = floatValue;
                StringValue = stringValue;
            }

            public bool BoolValue { get; }

            public int IntValue { get; }

            public float FloatValue { get; }

            public string StringValue { get; }
        }
    }
}
