//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 按类型汇总运行时对象内存。
        /// </summary>
        private sealed partial class RuntimeMemorySummaryWindow : ScrollableDebuggerWindowBase
        {
            /// <summary>
            /// 某一对象类型的内存汇总记录。
            /// </summary>
            private sealed class Record
            {
                /// <summary>
                /// 初始化内存汇总记录。
                /// </summary>
                /// <param name="name">对象类型名称。</param>
                public Record(string name)
                {
                    Name = name;
                    Count = 0;
                    Size = 0L;
                }

                /// <summary>
                /// 获取对象类型名称。
                /// </summary>
                public string Name
                {
                    get;
                }

                /// <summary>
                /// 获取或设置对象数量。
                /// </summary>
                public int Count
                {
                    get; set;
                }

                /// <summary>
                /// 获取或设置内存总字节数。
                /// </summary>
                public long Size
                {
                    get; set;
                }
            }
        }
    }
}
