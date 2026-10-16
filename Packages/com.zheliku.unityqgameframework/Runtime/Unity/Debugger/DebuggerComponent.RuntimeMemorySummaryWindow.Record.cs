//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        private sealed partial class RuntimeMemorySummaryWindow : ScrollableDebuggerWindowBase
        {
            private sealed class Record
            {
                public Record(string name)
                {
                    Name = name;
                    Count = 0;
                    Size = 0L;
                }

                public string Name { get; }

                public int Count { get; set; }

                public long Size { get; set; }
            }
        }
    }
}
