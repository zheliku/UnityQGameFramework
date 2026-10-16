//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System.IO;

namespace StarForce.Editor.DataTableTools
{
    public sealed partial class DataTableProcessor
    {
        private sealed class IdProcessor : DataProcessor
        {
            public override System.Type Type => typeof(int);

            public override bool IsId => true;

            public override bool IsComment => false;

            public override bool IsSystem => false;

            public override string LanguageKeyword => "int";

            public override string[] GetTypeStrings()
            {
                return new string[]
                {
                    "id"
                };
            }

            public override void WriteToStream(DataTableProcessor dataTableProcessor, BinaryWriter binaryWriter, string value)
            {
                binaryWriter.Write7BitEncodedInt32(int.Parse(value));
            }
        }
    }
}
