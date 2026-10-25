//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using System.Text;
using UnityEngine;
using UnityQGameFramework.Core.Config;
using YooAsset;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 默认配置辅助器。
    /// </summary>
    public class DefaultConfigHelper : ConfigHelperBase
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private ResourceComponent m_ResourceComponent;

        /// <summary>
        /// 读取配置资源。
        /// </summary>
        /// <param name="manager">配置管理器。</param>
        /// <param name="assetName">配置资源名称。</param>
        /// <param name="asset">配置资源。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否读取配置成功。</returns>
        public override bool ReadData(IConfigManager manager, string assetName, object asset, object userData)
        {
            if (asset is TextAsset text)
            {
                return manager.ParseData(text.text, userData);
            }
            if (asset is RawFileObject rawFile)
            {
                byte[] bytes = rawFile.GetBytes();
                return ParseData(manager, bytes, 0, bytes.Length, userData);
            }
            Log.Warning("Config asset '{0}' is not a text or raw file asset.", assetName);
            return false;
        }

        /// <summary>
        /// 读取配置内容。
        /// </summary>
        /// <param name="manager">配置管理器。</param>
        /// <param name="assetName">配置资源名称。</param>
        /// <param name="bytes">配置二进制内容。</param>
        /// <param name="startIndex">二进制内容的起始位置。</param>
        /// <param name="length">二进制内容的长度。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否读取配置成功。</returns>
        public override bool ReadData(IConfigManager manager, string assetName, byte[] bytes, int startIndex, int length, object userData)
        {
            return ParseData(manager, bytes, startIndex, length, userData);
        }

        /// <summary>
        /// 解析 TOML 配置。
        /// </summary>
        /// <param name="manager">配置管理器。</param>
        /// <param name="text">配置内容。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析配置成功。</returns>
        public override bool ParseData(IConfigManager manager, string text, object userData)
        {
            try
            {
                TomlConfigParser.ParseAndAdd(manager, text);
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("Cannot parse TOML configuration: {0}", exception.Message);
                return false;
            }
        }

        /// <summary>
        /// 解析 UTF-8 编码的 TOML 配置。
        /// </summary>
        /// <param name="manager">配置管理器。</param>
        /// <param name="bytes">配置二进制内容。</param>
        /// <param name="startIndex">二进制内容的起始位置。</param>
        /// <param name="length">二进制内容的长度。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>是否解析配置成功。</returns>
        public override bool ParseData(IConfigManager manager, byte[] bytes, int startIndex, int length, object userData)
        {
            try
            {
                return ParseData(manager, Utf8.GetString(bytes, startIndex, length), userData);
            }
            catch (Exception exception)
            {
                Log.Warning("Cannot decode UTF-8 TOML configuration: {0}", exception.Message);
                return false;
            }
        }

        /// <summary>
        /// 归还配置资源。
        /// </summary>
        /// <param name="manager">配置管理器。</param>
        /// <param name="asset">要归还的配置资源。</param>
        public override void ReleaseDataAsset(IConfigManager manager, object asset)
        {
            m_ResourceComponent.UnloadAsset(asset);
        }

        private void Start()
        {
            m_ResourceComponent = GameEntry.GetComponent<ResourceComponent>();
            if (m_ResourceComponent == null)
            {
                Log.Fatal("Resource component is invalid.");
            }
        }
    }
}
