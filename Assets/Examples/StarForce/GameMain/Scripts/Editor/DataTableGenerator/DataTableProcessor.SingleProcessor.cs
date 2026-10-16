//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.IO;

namespace StarForce.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class SingleProcessor : GenericDataProcessor<float>
        {
            public override bool IsSystem => true;

            public override string LanguageKeyword => "float";

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "float",
                    "single",
                    "system.single"
                };
            }

            public override float Parse(string value)
            {
                return float.Parse(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(Parse(value));
            }
        }
    }
}
