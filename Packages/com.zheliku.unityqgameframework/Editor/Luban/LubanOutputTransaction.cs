using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>Backs up only the generated assets this operation can change.</summary>
    internal sealed class LubanOutputTransaction
    {
        private readonly Dictionary<string, string> m_Files = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> m_NewDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly LubanProfile m_Profile;
        private readonly string m_ProfileState;

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

        private void Capture(string path, string backup)
        {
            if (m_Files.ContainsKey(path)) return;
            string copy = null;
            if (File.Exists(path))
            {
                copy = Path.Combine(backup, m_Files.Count.ToString("D6"));
                File.Copy(path, copy);
            }
            m_Files.Add(path, copy);
        }

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
                            // A locked destination may have caused the failure without changing it.
                            if (!File.Exists(file.Key) || !File.ReadAllBytes(file.Value).SequenceEqual(File.ReadAllBytes(file.Key)))
                                File.Copy(file.Value, file.Key, true);
                        }
                        else if (File.Exists(file.Key)) File.Delete(file.Key);
                    }
                    catch (Exception exception) { failures.Add(exception); }
                }
                foreach (string directory in m_NewDirectories.OrderByDescending(path => path.Length))
                {
                    try
                    {
                        if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                            Directory.Delete(directory);
                    }
                    catch (Exception exception) { failures.Add(exception); }
                }
                EditorJsonUtility.FromJsonOverwrite(m_ProfileState, m_Profile);
            }
            finally { AssetDatabase.AllowAutoRefresh(); }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (failures.Count != 0) throw new AggregateException("Could not restore all generated assets.", failures);
        }

        [Serializable] private sealed class BackupManifest { public BackupEntry[] files; public string[] newDirectories; }
        [Serializable] private sealed class BackupEntry { public string path; public string backup; }
    }
}
