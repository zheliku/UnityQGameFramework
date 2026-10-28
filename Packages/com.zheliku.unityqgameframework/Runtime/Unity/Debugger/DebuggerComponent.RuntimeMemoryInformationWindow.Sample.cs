//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        private sealed partial class RuntimeMemoryInformationWindow<T> : ScrollableDebuggerWindowBase where T : UnityEngine.Object
        {
            /// <summary>
            /// 一组运行时内存采样数据。
            /// </summary>
            private sealed class Sample
            {
                /// <summary>
                /// 初始化内存采样记录。
                /// </summary>
                /// <param name="name">对象名称。</param>
                /// <param name="type">对象类型名称。</param>
                /// <param name="size">内存字节数。</param>
                public Sample(string name, string type, long size)
                {
                    Name = name;
                    Type = type;
                    Size = size;
                    Highlight = false;
                }

                /// <summary>
                /// 获取对象名称。
                /// </summary>
                public string Name
                {
                    get;
                }

                /// <summary>
                /// 获取对象类型名称。
                /// </summary>
                public string Type
                {
                    get;
                }

                /// <summary>
                /// 获取内存字节数。
                /// </summary>
                public long Size
                {
                    get;
                }

                /// <summary>
                /// 获取或设置是否高亮重复采样记录。
                /// </summary>
                public bool Highlight
                {
                    get; set;
                }
            }
        }
    }
}
