//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Editor.ResourceTools
{
    public sealed partial class ResourceBuilderController
    {
        private sealed class AssetData
        {
            private readonly string[] m_DependencyAssetNames;

            public AssetData(string guid, string name, int length, int hashCode, string[] dependencyAssetNames)
            {
                Guid = guid;
                Name = name;
                Length = length;
                HashCode = hashCode;
                m_DependencyAssetNames = dependencyAssetNames;
            }

            public string Guid { get; }

            public string Name { get; }

            public int Length { get; }

            public int HashCode { get; }

            public string[] GetDependencyAssetNames()
            {
                return m_DependencyAssetNames;
            }
        }
    }
}
