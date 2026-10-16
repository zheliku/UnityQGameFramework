//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.IO;

namespace StarForce.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class Int16Processor : GenericDataProcessor<short>
        {
            public override bool IsSystem => true;

            public override string LanguageKeyword => "short";

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "short",
                    "int16",
                    "system.int16"
                };
            }

            public override short Parse(string value)
            {
                return short.Parse(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(Parse(value));
            }
        }
    }
}
