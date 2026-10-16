//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.IO;

namespace StarForce.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class ByteProcessor : GenericDataProcessor<byte>
        {
            public override bool IsSystem => true;

            public override string LanguageKeyword => "byte";

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "byte",
                    "system.byte"
                };
            }

            public override byte Parse(string value)
            {
                return byte.Parse(value);
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(Parse(value));
            }
        }
    }
}
