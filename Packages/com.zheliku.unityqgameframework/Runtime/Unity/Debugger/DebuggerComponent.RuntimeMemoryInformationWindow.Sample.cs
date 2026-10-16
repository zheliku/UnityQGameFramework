//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Runtime
{
    public sealed partial class DebuggerComponent : FrameworkComponent
    {
        private sealed partial class RuntimeMemoryInformationWindow<T> : ScrollableDebuggerWindowBase where T : UnityEngine.Object
        {
            private sealed class Sample
            {
                public Sample(string name, string type, long size)
                {
                    Name = name;
                    Type = type;
                    Size = size;
                    Highlight = false;
                }

                public string Name { get; }

                public string Type { get; }

                public long Size { get; }

                public bool Highlight { get; set; }
            }
        }
    }
}
