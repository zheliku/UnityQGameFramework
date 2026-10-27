using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 备份本次生成涉及的资产，并在失败时恢复。
    /// </summary>
    internal sealed class LubanOutputTransaction
    {
        private readonly Dictionary<string, string> m_Files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> m_NewDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly LubanProfile m_Profile;
        private readonly string m_ProfileState;

        /// <summary>
        /// 备份 Luban 输出文件及配置状态。
        /// </summary>
        /// <param name="backup">备份目录。</param>
        /// <param name="profile">要保护的 Luban 配置。</param>
        /// <param name="files">本次生成可能修改的文件。</param>
        /// <param name="roots">允许保留的输出根目录。</param>
        public LubanOutputTransaction(string backup, LubanProfile profile, IEnumerable<string> files, IEnumerable<string> roots)
        {
            m_Profile = profile;
            m_ProfileState = EditorJsonUtility.ToJson(profile);
            Directory.CreateDirectory(backup);
            var outputRoots = new HashSet<string>(roots.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
            foreach (string file in files)
            {
                string full = Path.GetFullPath(file);
                Capture(full, backup);
                Capture(full + ".meta", backup);
                string directory = Path.GetDirectoryName(full);
                while (!outputRoots.Contains(directory) && !Directory.Exists(directory))
                {
                    m_NewDirectories.Add(directory);
                    Capture(directory + ".meta", backup);
                    directory = Path.GetDirectoryName(directory);
                }
            }

            var manifest = new BackupManifest
            {
                files = m_Files.Select(file => new BackupEntry { path = file.Key, backup = file.Value }).ToArray(),
                newDirectories = m_NewDirectories.ToArray()
            };
            File.WriteAllText(Path.Combine(backup, "manifest.json"), JsonUtility.ToJson(manifest, true));
        }

        /// <summary>
        /// 登记文件或新目录的备份。
        /// </summary>
        /// <param name="path">要备份的路径。</param>
        /// <param name="backup">备份目录。</param>
        private void Capture(string path, string backup)
        {
            if (m_Files.ContainsKey(path))
            {
                return;
            }

            string copy = null;
            if (File.Exists(path))
            {
                copy = Path.Combine(backup, m_Files.Count.ToString("D6"));
                File.Copy(path, copy);
            }

            m_Files.Add(path, copy);
        }

        /// <summary>
        /// 恢复生成事务开始前的文件和配置。
        /// </summary>
        public void Rollback()
        {
            var failures = new List<Exception>();
            AssetDatabase.DisallowAutoRefresh();
            try
            {
                foreach (var file in m_Files)
                {
                    try
                    {
                        if (file.Value != null)
                        {
                            // 目标文件被锁定时可能未发生写入，内容未变则无需恢复。
                            if (!File.Exists(file.Key) || !File.ReadAllBytes(file.Value).SequenceEqual(File.ReadAllBytes(file.Key)))
                            {
                                File.Copy(file.Value, file.Key, true);
                            }
                        }
                        else if (File.Exists(file.Key))
                        {
                            File.Delete(file.Key);
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add(exception);
                    }
                }

                foreach (string directory in m_NewDirectories.OrderByDescending(path => path.Length))
                {
                    try
                    {
                        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                        {
                            Directory.Delete(directory);
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add(exception);
                    }
                }

                EditorJsonUtility.FromJsonOverwrite(m_ProfileState, m_Profile);
            }
            finally
            {
                AssetDatabase.AllowAutoRefresh();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (failures.Count != 0)
            {
                throw new AggregateException("Could not restore all generated assets.", failures);
            }
        }

        /// <summary>
        /// 生成事务回滚所需的备份清单。
        /// </summary>
        [Serializable]
        private sealed class BackupManifest
        {
            /// <summary>
            /// 生成文件的备份记录。
            /// </summary>
            public BackupEntry[] files;

            /// <summary>
            /// 事务开始前不存在的目录。
            /// </summary>
            public string[] newDirectories;
        }

        /// <summary>
        /// 生成事务中单个文件的备份记录。
        /// </summary>
        [Serializable]
        private sealed class BackupEntry
        {
            /// <summary>
            /// 原始文件路径。
            /// </summary>
            public string path;

            /// <summary>
            /// 备份文件路径；原文件不存在时为空。
            /// </summary>
            public string backup;
        }
    }
}
