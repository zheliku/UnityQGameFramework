using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    public static class LubanWorkspace
    {
        [InitializeOnLoadMethod]
        private static void ScheduleInitialization()
        {
            if (!AssetDatabase.IsAssetImportWorkerProcess()) EditorApplication.update += InitializeWhenReady;
        }

        private static void InitializeWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
                return;
            EditorApplication.update -= InitializeWhenReady;
            try { EnsureInitialized(); }
            catch (Exception exception) { Debug.LogWarning("QGF Luban 初始化未完成：" + exception.Message); }
        }

        public static LubanProfile EnsureInitialized()
        {
            var settings = LubanSettings.instance;
            if (settings.HasProfile)
                return settings.Profile != null ? settings.Profile : throw new InvalidOperationException("全局 Profile 已丢失，请在 Project Settings > Unity Q Game Framework > Luban 中重新指定。");

            // Migrate the former per-machine choice once, without changing its target.
            string legacyKey = "QGF.Luban.Profile." + Application.dataPath;
            string previous = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(legacyKey, string.Empty));
            var profile = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<LubanProfile>(previous);
            if (profile == null) profile = AssetDatabase.LoadAssetAtPath<LubanProfile>(settings.WorkspaceRoot + "/Editor/LubanProfile.asset");
            if (profile == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LubanProfile", new[] { "Assets" });
                if (guids.Length > 1) throw new InvalidOperationException("存在多个 Profile，请在 QGF 项目设置中指定全局 Profile。");
                profile = guids.Length == 1
                    ? AssetDatabase.LoadAssetAtPath<LubanProfile>(AssetDatabase.GUIDToAssetPath(guids[0]))
                    : Create(settings.WorkspaceRoot);
            }
            settings.Profile = profile;
            EditorPrefs.DeleteKey(legacyKey);
            return profile;
        }

        public static string ValidateRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root)) throw new ArgumentException("工作区目录不能为空。");
            root = root.Replace('\\', '/').TrimEnd('/');
            if (!root.StartsWith("Assets/", StringComparison.Ordinal)) throw new ArgumentException("工作区必须位于 Assets 内。");
            foreach (string segment in root.Split('/'))
                if (string.IsNullOrWhiteSpace(segment) || segment == "." || segment == "..") throw new ArgumentException("工作区目录不能包含空目录段、. 或 ..。");
            string assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(root).StartsWith(assets, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("工作区必须位于 Assets 内。");
            return root;
        }

        public static LubanProfile Create(string root = LubanSettings.DefaultWorkspaceRoot)
        {
            root = ValidateRoot(root);
            if (Directory.Exists(root)) throw new IOException("工作区目录已存在，请指定已有 Profile 或选择新的目录：" + root);
            EnsureFolder(root + "/Source");
            EnsureFolder(root + "/Generated");
            EnsureFolder(root + "/Data");
            EnsureFolder(root + "/Extensions");
            EnsureFolder(root + "/Editor");
            string template = Path.Combine(LubanGenerator.PackageRoot, "Editor/Luban/Starter~");
            foreach (string file in Directory.GetFiles(template, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(template.Length + 1);
                string destination = Path.Combine(root, "Source", relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var profile = ScriptableObject.CreateInstance<LubanProfile>();
            profile.SourceFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(root + "/Source");
            profile.CodeFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(root + "/Generated");
            profile.DataFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(root + "/Data");
            AssetDatabase.CreateAsset(profile, root + "/Editor/LubanProfile.asset");
            AssetDatabase.SaveAssets();
            return profile;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            ValidateRoot(path);
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path)))) throw new IOException("无法创建目录：" + path);
        }
    }
}
