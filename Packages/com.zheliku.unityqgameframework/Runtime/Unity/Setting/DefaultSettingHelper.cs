//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 默认游戏配置辅助器。
    /// </summary>
    public class DefaultSettingHelper : SettingHelperBase
    {
        private const string SettingFileName = "GameFrameworkSetting.dat";
        private bool m_PrimaryFileValid;

        /// <summary>
        /// 获取游戏配置项数量。
        /// </summary>
        public override int Count
        {
            get
            {
                return Setting != null ? Setting.Count : 0;
            }
        }

        /// <summary>
        /// 获取初始化时确定的游戏配置存储文件路径。
        /// </summary>
        public string FilePath
        {
            get; private set;
        }

        /// <summary>
        /// 指定文件存储位置，供项目或示例隔离各自的设置。
        /// </summary>
        /// <returns>配置文件的完整路径。</returns>
        protected virtual string GetFilePath()
        {
            return Path.Combine(Application.persistentDataPath, SettingFileName);
        }

        /// <summary>
        /// 获取当前游戏配置快照；成功加载会替换此实例。
        /// </summary>
        public DefaultSetting Setting
        {
            get; private set;
        }

        /// <summary>
        /// 获取游戏配置序列化器。
        /// </summary>
        public DefaultSettingSerializer Serializer
        {
            get; private set;
        }

        /// <summary>
        /// 完整读取配置；正式文件损坏时尝试上一份备份。
        /// </summary>
        /// <returns>文件不存在或完整读取成功时为真；全部读取失败时保留当前内存数据。</returns>
        public override bool Load()
        {
            m_PrimaryFileValid = false;
            bool found = false;
            foreach (string path in new[] { FilePath, FilePath + ".bak" })
            {
                try
                {
                    using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        found = true;
                        DefaultSetting loaded = Serializer.Deserialize(fileStream);
                        if (loaded == null || fileStream.Position != fileStream.Length)
                        {
                            throw new InvalidDataException("Setting data is incomplete or contains trailing bytes.");
                        }
                        Setting = loaded;
                    }
                    m_PrimaryFileValid = path == FilePath;
                    return true;
                }
                catch (FileNotFoundException)
                {
                }
                catch (DirectoryNotFoundException)
                {
                }
                catch (Exception exception)
                {
                    found = true;
                    Log.Warning("Cannot load settings at '{0}': {1}", path, exception.Message);
                }
            }
            return !found;
        }

        /// <summary>
        /// 写入同目录临时文件，刷新后原子替换正式文件并保留有效备份。
        /// </summary>
        /// <returns>是否完成写入；失败时正式文件不变。</returns>
        public override bool Save()
        {
            string temporary = FilePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                using (FileStream fileStream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    if (!Serializer.Serialize(fileStream, Setting))
                    {
                        return false;
                    }
                    fileStream.Flush(true);
                }
                if (File.Exists(FilePath))
                {
                    // 从备份恢复后，不把损坏的正式文件覆盖到有效备份上。
                    File.Replace(temporary, FilePath, m_PrimaryFileValid ? FilePath + ".bak" : null);
                }
                else
                {
                    File.Move(temporary, FilePath);
                }
                m_PrimaryFileValid = true;
                return true;
            }
            catch (Exception exception)
            {
                Log.Warning("Cannot save settings at '{0}': {1}", FilePath, exception.Message);
                return false;
            }
            finally
            {
                try
                {
                    File.Delete(temporary);
                }
                catch (Exception exception)
                {
                    Log.Warning("Cannot remove temporary settings at '{0}': {1}", temporary, exception.Message);
                }
            }
        }

        /// <summary>
        /// 获取所有游戏配置项的名称。
        /// </summary>
        /// <returns>所有游戏配置项的名称。</returns>
        public override string[] GetAllSettingNames()
        {
            return Setting.GetAllSettingNames();
        }

        /// <summary>
        /// 获取所有游戏配置项的名称。
        /// </summary>
        /// <param name="results">所有游戏配置项的名称；填充前清空，容量不足时仍会扩容。</param>
        public override void GetAllSettingNamesNonAlloc(List<string> results)
        {
            Setting.GetAllSettingNamesNonAlloc(results);
        }

        /// <summary>
        /// 检查是否存在指定游戏配置项。
        /// </summary>
        /// <param name="settingName">要检查游戏配置项的名称。</param>
        /// <returns>指定的游戏配置项是否存在。</returns>
        public override bool HasSetting(string settingName)
        {
            return Setting.HasSetting(settingName);
        }

        /// <summary>
        /// 移除指定游戏配置项。
        /// </summary>
        /// <param name="settingName">要移除游戏配置项的名称。</param>
        /// <returns>是否移除指定游戏配置项成功。</returns>
        public override bool RemoveSetting(string settingName)
        {
            return Setting.RemoveSetting(settingName);
        }

        /// <summary>
        /// 清空所有游戏配置项。
        /// </summary>
        public override void RemoveAllSettings()
        {
            Setting.RemoveAllSettings();
        }

        /// <summary>
        /// 从指定游戏配置项中读取布尔值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的布尔值。</returns>
        public override bool GetBool(string settingName)
        {
            return Setting.GetBool(settingName);
        }

        /// <summary>
        /// 从指定游戏配置项中读取布尔值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的布尔值。</returns>
        public override bool GetBool(string settingName, bool defaultValue)
        {
            return Setting.GetBool(settingName, defaultValue);
        }

        /// <summary>
        /// 向指定游戏配置项写入布尔值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的布尔值。</param>
        public override void SetBool(string settingName, bool value)
        {
            Setting.SetBool(settingName, value);
        }

        /// <summary>
        /// 从指定游戏配置项中读取整数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的整数值。</returns>
        public override int GetInt(string settingName)
        {
            return Setting.GetInt(settingName);
        }

        /// <summary>
        /// 从指定游戏配置项中读取整数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的整数值。</returns>
        public override int GetInt(string settingName, int defaultValue)
        {
            return Setting.GetInt(settingName, defaultValue);
        }

        /// <summary>
        /// 向指定游戏配置项写入整数值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的整数值。</param>
        public override void SetInt(string settingName, int value)
        {
            Setting.SetInt(settingName, value);
        }

        /// <summary>
        /// 从指定游戏配置项中读取浮点数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的浮点数值。</returns>
        public override float GetFloat(string settingName)
        {
            return Setting.GetFloat(settingName);
        }

        /// <summary>
        /// 从指定游戏配置项中读取浮点数值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的浮点数值。</returns>
        public override float GetFloat(string settingName, float defaultValue)
        {
            return Setting.GetFloat(settingName, defaultValue);
        }

        /// <summary>
        /// 向指定游戏配置项写入浮点数值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的浮点数值。</param>
        public override void SetFloat(string settingName, float value)
        {
            Setting.SetFloat(settingName, value);
        }

        /// <summary>
        /// 从指定游戏配置项中读取字符串值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的字符串值。</returns>
        public override string GetString(string settingName)
        {
            return Setting.GetString(settingName);
        }

        /// <summary>
        /// 从指定游戏配置项中读取字符串值。
        /// </summary>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultValue">当指定的游戏配置项不存在时，返回此默认值。</param>
        /// <returns>读取的字符串值。</returns>
        public override string GetString(string settingName, string defaultValue)
        {
            return Setting.GetString(settingName, defaultValue);
        }

        /// <summary>
        /// 向指定游戏配置项写入字符串值。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="value">要写入的字符串值。</param>
        public override void SetString(string settingName, string value)
        {
            Setting.SetString(settingName, value);
        }

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <typeparam name="T">要读取对象的类型。</typeparam>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的对象。</returns>
        public override T GetObject<T>(string settingName)
        {
            return Utility.Json.ToObject<T>(GetString(settingName));
        }

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <param name="objectType">要读取对象的类型。</param>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <returns>读取的对象。</returns>
        public override object GetObject(Type objectType, string settingName)
        {
            return Utility.Json.ToObject(objectType, GetString(settingName));
        }

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <typeparam name="T">要读取对象的类型。</typeparam>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultObj">当指定的游戏配置项不存在时，返回此默认对象。</param>
        /// <returns>读取的对象。</returns>
        public override T GetObject<T>(string settingName, T defaultObj)
        {
            string json = GetString(settingName, null);
            if (json == null)
            {
                return defaultObj;
            }

            return Utility.Json.ToObject<T>(json);
        }

        /// <summary>
        /// 从指定游戏配置项中读取对象。
        /// </summary>
        /// <param name="objectType">要读取对象的类型。</param>
        /// <param name="settingName">要获取游戏配置项的名称。</param>
        /// <param name="defaultObj">当指定的游戏配置项不存在时，返回此默认对象。</param>
        /// <returns>读取的对象。</returns>
        public override object GetObject(Type objectType, string settingName, object defaultObj)
        {
            string json = GetString(settingName, null);
            if (json == null)
            {
                return defaultObj;
            }

            return Utility.Json.ToObject(objectType, json);
        }

        /// <summary>
        /// 向指定游戏配置项写入对象。
        /// </summary>
        /// <typeparam name="T">要写入对象的类型。</typeparam>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public override void SetObject<T>(string settingName, T obj)
        {
            SetString(settingName, Utility.Json.ToJson(obj));
        }

        /// <summary>
        /// 向指定游戏配置项写入对象。
        /// </summary>
        /// <param name="settingName">要写入游戏配置项的名称。</param>
        /// <param name="obj">要写入的对象。</param>
        public override void SetObject(string settingName, object obj)
        {
            SetString(settingName, Utility.Json.ToJson(obj));
        }

        /// <summary>
        /// 注册本地设置序列化和反序列化回调。
        /// </summary>
        private void Awake()
        {
            FilePath = Utility.Path.GetRegularPath(Path.GetFullPath(GetFilePath()));
            Setting = new DefaultSetting();
            Serializer = new DefaultSettingSerializer();
            Serializer.RegisterSerializeCallback(0, SerializeDefaultSettingCallback);
            Serializer.RegisterDeserializeCallback(0, DeserializeDefaultSettingCallback);
        }

        /// <summary>
        /// 将本地设置写入流。
        /// </summary>
        /// <param name="stream">写入目标流。</param>
        /// <param name="defaultSetting">要写入的本地设置。</param>
        /// <returns>是否成功写入本地设置。</returns>
        private bool SerializeDefaultSettingCallback(Stream stream, DefaultSetting defaultSetting)
        {
            defaultSetting.Serialize(stream);
            return true;
        }

        /// <summary>
        /// 从流中读取本地设置。
        /// </summary>
        /// <param name="stream">设置数据流。</param>
        /// <returns>读取的本地设置。</returns>
        private DefaultSetting DeserializeDefaultSettingCallback(Stream stream)
        {
            var setting = new DefaultSetting();
            setting.Deserialize(stream);
            return setting;
        }
    }
}
