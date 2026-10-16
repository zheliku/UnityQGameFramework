//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.IO;

namespace StarForce.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class StringProcessor : GenericDataProcessor<string>
        {
            public override bool IsSystem => true;

            public override string LanguageKeyword => "string";

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "string",
                    "system.string"
                };
            }

            public override string Parse(string value)
            {
                return value;
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write(Parse(value));
            }
        }
    }
}
