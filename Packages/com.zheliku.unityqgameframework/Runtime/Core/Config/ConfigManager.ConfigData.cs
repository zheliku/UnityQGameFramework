//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.Runtime.InteropServices;

namespace UnityQGameFramework.Core
{
    internal sealed partial class ConfigManager : GameFrameworkModule, IConfigManager
    {
        /// <summary>
        /// 保存同一配置项的布尔、整数、浮点和文本值。
        /// </summary>
        [StructLayout(LayoutKind.Auto)]
        private struct ConfigData
        {
            /// <summary>
            /// 初始化全局配置值。
            /// </summary>
            /// <param name="boolValue">布尔值。</param>
            /// <param name="intValue">整数值。</param>
            /// <param name="floatValue">浮点数值。</param>
            /// <param name="stringValue">字符串值。</param>
            public ConfigData(bool boolValue, int intValue, float floatValue, string stringValue)
            {
                BoolValue = boolValue;
                IntValue = intValue;
                FloatValue = floatValue;
                StringValue = stringValue;
            }

            /// <summary>
            /// 获取布尔值。
            /// </summary>
            public bool BoolValue
            {
                get;
            }

            /// <summary>
            /// 获取整数值。
            /// </summary>
            public int IntValue
            {
                get;
            }

            /// <summary>
            /// 获取浮点数值。
            /// </summary>
            public float FloatValue
            {
                get;
            }

            /// <summary>
            /// 获取字符串值。
            /// </summary>
            public string StringValue
            {
                get;
            }
        }
    }
}
