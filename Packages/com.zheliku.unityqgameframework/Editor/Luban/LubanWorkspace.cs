using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 创建和维护项目中的 Luban 配置目录。
    /// </summary>
    public static class LubanWorkspace
    {
        /// <summary>
        /// 在编辑器就绪后安排 Luban 工作区初始化。
        /// </summary>
        [InitializeOnLoadMethod]
        private static void ScheduleInitialization()
        {
            if (!AssetDatabase.IsAssetImportWorkerProcess())
            {
                EditorApplication.update += InitializeWhenReady;
            }
        }

        /// <summary>
        /// 在 AssetDatabase 和脚本编译完成后初始化工作区。
        /// </summary>
        private static void InitializeWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            EditorApplication.update -= InitializeWhenReady;
            // 多个 Sample 可以各自拥有 Profile。没有全局选择时等待用户在 QGF Settings 指定，启动阶段不输出警告。
            LubanSettings settings = LubanSettings.instance;
            if ((settings.HasProfile && settings.Profile == null) || HasMultipleProfilesWithoutSelection())
            {
                return;
            }

            try
            {
                EnsureInitialized();
            }
            catch (Exception exception)
            {
                Debug.LogWarning("QGF Luban initialization failed: " + exception.Message);
            }
        }

        /// <summary>
        /// 检查是否存在多个未选择的 Luban profile。
        /// </summary>
        /// <returns>是否需要用户明确选择全局 profile。</returns>
        private static bool HasMultipleProfilesWithoutSelection()
        {
            LubanSettings settings = LubanSettings.instance;
            if (settings.HasProfile)
            {
                return false;
            }

            string legacyKey = "QGF.Luban.Profile." + Application.dataPath;
            string previous = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(legacyKey, string.Empty));
            if (!string.IsNullOrEmpty(previous) && AssetDatabase.LoadAssetAtPath<LubanProfile>(previous) != null)
            {
                return false;
            }

            LubanProfile workspaceProfile = FindProfile(settings.WorkspaceRoot);
            if (workspaceProfile != null)
            {
                return false;
            }

            return AssetDatabase.FindAssets("t:LubanProfile", new[] { "Assets" }).Length > 1;
        }

        /// <summary>
        /// 获取全局 Profile，首次使用时创建默认配置目录。
        /// </summary>
        /// <returns>当前全局 Profile。</returns>
        public static LubanProfile EnsureInitialized()
        {
            var settings = LubanSettings.instance;
            if (settings.HasProfile)
            {
                if (settings.Profile == null)
                {
                    throw new InvalidOperationException("The global Profile is missing. Assign it in QGF > Settings.");
                }

                RepairStarterSchemas(settings.Profile);
                return settings.Profile;
            }

            // 保留旧版项目已选定的全局 Profile。
            string legacyKey = "QGF.Luban.Profile." + Application.dataPath;
            string previous = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(legacyKey, string.Empty));
            var profile = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<LubanProfile>(previous);
            if (profile == null)
            {
                profile = FindProfile(settings.WorkspaceRoot);
            }

            if (profile == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:LubanProfile", new[] { "Assets" });
                if (guids.Length > 1)
                {
                    throw new InvalidOperationException("Multiple Profiles were found. Select the global Profile in QGF project settings.");
                }

                profile = guids.Length == 1 ? AssetDatabase.LoadAssetAtPath<LubanProfile>(AssetDatabase.GUIDToAssetPath(guids[0])) : Create(settings.WorkspaceRoot);
            }

            settings.Profile = profile;
            EditorPrefs.DeleteKey(legacyKey);
            RepairStarterSchemas(profile);
            return profile;
        }

        /// <summary>
        /// 补齐未改动的初始配置中缺失的 Excel 表定义。
        /// </summary>
        /// <param name="profile">生成配置。</param>
        public static void RepairStarterSchemas(LubanProfile profile)
        {
            string source = LubanGenerator.SourcePath(profile);
            string template = Path.Combine(LubanGenerator.PackageRoot, "Editor/Luban/Starter~");
            string conf = LubanGenerator.ConfigurationPath(profile);
            string datas = source;
            if (string.IsNullOrEmpty(source) || !File.Exists(conf) || !Directory.Exists(datas))
            {
                return;
            }

            if (NormalizeStarterConfiguration(File.ReadAllText(conf)) != NormalizeStarterConfiguration(File.ReadAllText(Path.Combine(template, "luban.conf"))))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(datas, "*.xlsx", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(file);
                if (name != "Item.xlsx" && name != "__tables__.xlsx" && name != "__beans__.xlsx" && name != "__enums__.xlsx")
                {
                    return;
                }
            }

            if (!File.Exists(Path.Combine(datas, "Item.xlsx")))
            {
                return;
            }

            bool repaired = false;
            foreach (string name in new[]
            {
                "__tables__.xlsx",
                "__beans__.xlsx",
                "__enums__.xlsx"
            }

            )
            {
                string path = Path.Combine(datas, name);
                if (File.Exists(path))
                {
                    continue;
                }

                File.Copy(Path.Combine(template, "Source", name), path);
                repaired = true;
            }

            if (repaired)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
        }

        /// <summary>
        /// 规范化 starter 配置文本。
        /// </summary>
        /// <param name="text">初始 Luban 配置的 JSON 文本。</param>
        /// <returns>规范化后的配置文本。</returns>
        private static string NormalizeStarterConfiguration(string text)
        {
            return text.Replace("Source/", string.Empty).Replace("Datas/", string.Empty).Replace("\"dataDir\": \"Source\"", "\"dataDir\": \".\"").Replace("\"dataDir\": \"Datas\"", "\"dataDir\": \".\"").Replace("\r\n", "\n").Trim();
        }

        /// <summary>
        /// 查找指定根目录下的 Luban profile。
        /// </summary>
        /// <param name="root">Luban 工作区根目录。</param>
        /// <returns>找到的 Luban profile；不存在时返回空。</returns>
        internal static LubanProfile FindProfile(string root)
        {
            return AssetDatabase.LoadAssetAtPath<LubanProfile>(root + "/LubanProfile.asset") ?? AssetDatabase.LoadAssetAtPath<LubanProfile>(root + "/Editor/LubanProfile.asset");
        }

        /// <summary>
        /// 检查配置目录是否位于 Assets 内。
        /// </summary>
        /// <param name="root">配置目录。</param>
        /// <returns>规范化后的项目相对路径。</returns>
        public static string ValidateRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("The workspace path must not be empty.");
            }

            root = root.Replace('\\', '/').TrimEnd('/');
            if (!root.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new ArgumentException("The workspace must be inside Assets.");
            }

            foreach (string segment in root.Split('/'))
            {
                if (string.IsNullOrWhiteSpace(segment) || segment == "." || segment == "..")
                {
                    throw new ArgumentException("The workspace path must not contain empty segments, . or ...");
                }
            }

            string assets = Path.GetFullPath(Application.dataPath) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(root).StartsWith(assets, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The workspace must be inside Assets.");
            }

            return root;
        }

        /// <summary>
        /// 创建初始表格和生成配置。
        /// </summary>
        /// <param name="root">配置根目录。</param>
        /// <returns>新建的生成配置。</returns>
        public static LubanProfile Create(string root = LubanSettings.DefaultWorkspaceRoot)
        {
            root = ValidateRoot(root);
            if (Directory.Exists(root))
            {
                throw new IOException("The workspace already exists. Select its Profile or choose a new folder: " + root);
            }

            EnsureFolder(root + "/Source");
            EnsureFolder(root + "/Generated");
            EnsureFolder(root + "/Data");
            string template = Path.Combine(LubanGenerator.PackageRoot, "Editor/Luban/Starter~");
            foreach (string file in Directory.GetFiles(template, "*", SearchOption.AllDirectories))
            {
                string relative = file.Substring(template.Length + 1);
                string destination = Path.Combine(root, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(file, destination);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var profile = ScriptableObject.CreateInstance<LubanProfile>();
            profile.ConfigurationFile = AssetDatabase.LoadAssetAtPath<DefaultAsset>(root + "/luban.conf");
            profile.CodeFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(root + "/Generated");
            profile.DataFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(root + "/Data");
            AssetDatabase.CreateAsset(profile, root + "/LubanProfile.asset");
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>
        /// 创建不存在的项目目录。
        /// </summary>
        /// <param name="path">项目相对路径。</param>
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            ValidateRoot(path);
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            if (string.IsNullOrEmpty(AssetDatabase.CreateFolder(parent, Path.GetFileName(path))))
            {
                throw new IOException("Failed to create folder: " + path);
            }
        }
    }
}
