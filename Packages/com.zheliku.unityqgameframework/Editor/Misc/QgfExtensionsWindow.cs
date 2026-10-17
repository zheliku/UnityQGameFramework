using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace UnityQGameFramework.Editor
{
    public sealed class QgfExtensionsWindow : EditorWindow
    {
        private sealed class Extension
        {
            public string Name { get; }
            public string PackageName { get; }
            public string AssemblyName { get; }
            public string GitUrl { get; }
            public string Website { get; }
            public string Description { get; }
            public string MinimumUnity { get; }

            public Extension(string name, string packageName, string assemblyName, string gitUrl,
                string website, string description, string minimumUnity)
            {
                Name = name;
                PackageName = packageName;
                AssemblyName = assemblyName;
                GitUrl = gitUrl;
                Website = website;
                Description = description;
                MinimumUnity = minimumUnity;
            }
        }

        private static readonly Extension[] Extensions =
        {
            new Extension("Alchemy", "com.annulusgames.alchemy", "Alchemy",
                "https://github.com/annulusgames/Alchemy.git?path=/Alchemy/Assets/Alchemy",
                "https://github.com/annulusgames/Alchemy",
                "Inspector、Hierarchy 和 EditorWindow 增强。序列化扩展另需 Unity.Serialization。", "2021.2"),
            new Extension("Rainbow Folders 2", "com.borodar.rainbow-folders", "Borodar.RainbowFolders",
                "https://github.com/Borod4r/Rainbow-Folders-2.git?path=Assets/Plugins/Borodar/RainbowFolders",
                "https://github.com/Borod4r/Rainbow-Folders-2",
                "为 Project 窗口中的文件夹设置颜色、图标与规则。", "6000.3")
        };

        private static AddRequest s_Request;
        private static string s_Installing;
        private static string s_Error;
        private static bool CanStartInstallation => s_Request == null && !EditorApplication.isPlayingOrWillChangePlaymode
            && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
        private PackageInfo[] m_Packages = Array.Empty<PackageInfo>();
        private Vector2 m_Scroll;

        [MenuItem("QGF/Extensions", false, 60)]
        public static void Open()
        {
            var window = GetWindow<QgfExtensionsWindow>("QGF Extensions");
            window.minSize = new Vector2(520f, 400f);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshPackages();
            Events.registeredPackages += OnPackagesRegistered;
        }

        private void OnDisable() => Events.registeredPackages -= OnPackagesRegistered;
        private void OnFocus() => RefreshPackages();
        private void OnPackagesRegistered(PackageRegistrationEventArgs _) => RefreshPackages();

        private void RefreshPackages()
        {
            m_Packages = PackageInfo.GetAllRegisteredPackages();
            Repaint();
        }

        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scroll.scrollPosition;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("可选编辑器扩展", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("点击安装后，工具会添加到当前项目。QGF 本身可独立编译和运行。", MessageType.Info);
                foreach (var extension in Extensions) DrawExtension(extension);
                if (s_Request != null) EditorGUILayout.HelpBox("正在安装 " + s_Installing + "…", MessageType.Info);
                if (!string.IsNullOrEmpty(s_Error)) EditorGUILayout.HelpBox(s_Error, MessageType.Error);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("工具的更新和移除请在 Package Manager 中操作。卸载 QGF 时保留这些工具。", EditorStyles.wordWrappedLabel);
            }
        }

        private void DrawExtension(Extension extension)
        {
            var installed = m_Packages.FirstOrDefault(p => p.name == extension.PackageName);
            bool legacy = installed == null && HasAssembly(extension);
            bool compatible = SupportsUnity(Application.unityVersion, extension.MinimumUnity);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(extension.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(extension.Description, EditorStyles.wordWrappedLabel);
                string status = installed != null ? "已安装 " + installed.version : legacy ? "已检测到本地工具" : "未安装";
                EditorGUILayout.LabelField(status + "  ·  Unity " + extension.MinimumUnity + "+", EditorStyles.miniLabel);
                if (!compatible && installed == null && !legacy)
                    EditorGUILayout.LabelField("当前 Unity 版本不满足此 Git 包的要求。", EditorStyles.wordWrappedLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("仓库", GUILayout.Width(70))) Application.OpenURL(extension.Website);
                    if (installed != null)
                    {
                        if (GUILayout.Button("在 Package Manager 中查看")) UnityEditor.PackageManager.UI.Window.Open(extension.PackageName);
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(legacy || !compatible || !CanStartInstallation))
                            if (GUILayout.Button(legacy ? "已安装" : "安装")) Install(extension);
                    }
                }
            }
        }

        private static bool HasAssembly(Extension extension) => AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == extension.AssemblyName);

        private static bool SupportsUnity(string current, string minimum)
        {
            string[] parts = current.Split('.');
            return parts.Length >= 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor)
                && new System.Version(major, minor) >= new System.Version(minimum);
        }

        private static void Install(Extension extension)
        {
            // Preserve existing installations and pinned revisions, including tools imported into Assets.
            if (!CanStartInstallation || !SupportsUnity(Application.unityVersion, extension.MinimumUnity) || HasAssembly(extension)
                || PackageInfo.GetAllRegisteredPackages().Any(p => p.name == extension.PackageName)) return;
            s_Error = null;
            try
            {
                s_Request = Client.Add(extension.GitUrl);
                s_Installing = extension.Name;
                EditorApplication.update += PollInstallation;
            }
            catch (Exception exception)
            {
                s_Error = "安装 " + extension.Name + " 失败：" + exception.Message;
            }
        }

        private static void PollInstallation()
        {
            if (s_Request == null || !s_Request.IsCompleted) return;
            if (s_Request.Status == StatusCode.Failure)
                s_Error = "安装 " + s_Installing + " 失败：" + (s_Request.Error?.message ?? "未知错误");
            s_Request = null;
            s_Installing = null;
            EditorApplication.update -= PollInstallation;
            foreach (var window in Resources.FindObjectsOfTypeAll<QgfExtensionsWindow>()) window.RefreshPackages();
        }
    }
}
