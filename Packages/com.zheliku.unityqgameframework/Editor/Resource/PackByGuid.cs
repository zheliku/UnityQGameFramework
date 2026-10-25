using System;
using UnityEditor;
using YooAsset.Editor;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 按资产 GUID 分别打包资源。
    /// </summary>
    [DisplayName("资源包名: 资产 GUID")]
    public sealed class PackByGuid : IBundlePackRule
    {
        /// <summary>
        /// 获取资源打包规则结果。
        /// </summary>
        /// <param name="data">资源打包规则数据。</param>
        /// <returns>以资产 GUID 命名的独立资源包。</returns>
        public BundlePackRuleResult GetPackRuleResult(BundlePackRuleData data)
        {
            string guid = AssetDatabase.AssetPathToGUID(data.AssetPath);
            if (string.IsNullOrEmpty(guid))
            {
                throw new InvalidOperationException("Resource asset GUID is missing: " + data.AssetPath);
            }

            // 目录移动不改变包名，也避免完整资源路径拉长构建缓存文件名。
            return new BundlePackRuleResult(guid, DefaultBundlePackRule.AssetBundleFileExtension);
        }
    }
}
