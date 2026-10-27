using System;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 管理可选编辑器扩展的安装与移除。
    /// </summary>
    public sealed class QgfExtensionsWindow : EditorWindow
    {
        /// <summary>
        /// 可选扩展的包地址和安装状态。
        /// </summary>
        private sealed class Extension
        {
            /// <summary>
            /// 获取扩展名称。
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// 获取扩展包名称。
            /// </summary>
            public string PackageName { get; }

            /// <summary>
            /// 获取扩展程序集名称。
            /// </summary>
            public string AssemblyName { get; }

            /// <summary>
            /// 获取扩展 Git 地址。
            /// </summary>
            public string GitUrl { get; }

            /// <summary>
            /// 获取扩展网站地址。
            /// </summary>
            public string Website { get; }

            /// <summary>
            /// 获取扩展说明。
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// 获取扩展要求的最低 Unity 版本。
            /// </summary>
            public string MinimumUnity { get; }

            /// <summary>
            /// 获取扩展许可证说明。
            /// </summary>
            public string LicenseInfo { get; }

            /// <summary>
            /// 初始化可选扩展信息。
            /// </summary>
            /// <param name="name">扩展名称。</param>
            /// <param name="packageName">扩展包名称。</param>
            /// <param name="assemblyName">扩展程序集名称。</param>
            /// <param name="gitUrl">扩展 Git 地址。</param>
            /// <param name="website">扩展网站地址。</param>
            /// <param name="description">扩展说明。</param>
            /// <param name="minimumUnity">最低 Unity 版本。</param>
            /// <param name="licenseInfo">许可证说明。</param>
            public Extension(string name, string packageName, string assemblyName, string gitUrl, string website, string description, string minimumUnity, string licenseInfo)
            {
                Name = name;
                PackageName = packageName;
                AssemblyName = assemblyName;
                GitUrl = gitUrl;
                Website = website;
                Description = description;
                MinimumUnity = minimumUnity;
                LicenseInfo = licenseInfo;
            }
        }

        private static readonly Extension[] Extensions =
        {
            new Extension("Alchemy", "com.annulusgames.alchemy", "Alchemy", "https://github.com/annulusgames/Alchemy.git?path=/Alchemy/Assets/Alchemy", "https://github.com/annulusgames/Alchemy", "Inspector、Hierarchy 和 EditorWindow 增强。序列化扩展另需 Unity.Serialization。", "2021.2", "许可：MIT。"),
            new Extension("Rainbow Folders 2", "com.borodar.rainbow-folders", "Borodar.RainbowFolders", "https://github.com/Borod4r/Rainbow-Folders-2.git?path=Assets/Plugins/Borodar/RainbowFolders", "https://github.com/Borod4r/Rainbow-Folders-2", "为 Project 窗口中的文件夹设置颜色、图标与规则。", "6000.3", "许可：代码为 Apache-2.0；图标、图片等美术资源为 CC BY-NC 4.0（非商业用途）。商业使用请先核对上游授权。")
        };
        private static AddRequest s_Request;
        private static string s_Installing;
        private static string s_Error;
        private static bool CanStartInstallation
        {
            get
            {
                return s_Request == null && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && !EditorApplication.isUpdating;
            }
        }

        private PackageInfo[] m_Packages = Array.Empty<PackageInfo>();
        private Vector2 m_Scroll;

        /// <summary>
        /// 打开可选扩展管理面板。
        /// </summary>
        [MenuItem("QGF/Extensions", false, 60)]
        public static void Open()
        {
            var window = GetWindow<QgfExtensionsWindow>("QGF Extensions");
            window.minSize = new Vector2(520f, 400f);
            window.Show();
        }

        /// <summary>
        /// 初始化扩展窗口。
        /// </summary>
        private void OnEnable()
        {
            RefreshPackages();
            Events.registeredPackages += OnPackagesRegistered;
        }

        /// <summary>
        /// 停止扩展包注册监听。
        /// </summary>
        private void OnDisable()
        {
            Events.registeredPackages -= OnPackagesRegistered;
        }

        /// <summary>
        /// 窗口获得焦点时刷新扩展状态。
        /// </summary>
        private void OnFocus()
        {
            RefreshPackages();
        }

        /// <summary>
        /// 包注册完成后刷新扩展状态。
        /// </summary>
        /// <param name="_">包注册事件参数。</param>
        private void OnPackagesRegistered(PackageRegistrationEventArgs _)
        {
            RefreshPackages();
        }

        /// <summary>
        /// 刷新已安装包和扩展状态。
        /// </summary>
        private void RefreshPackages()
        {
            m_Packages = PackageInfo.GetAllRegisteredPackages();
            Repaint();
        }

        /// <summary>
        /// 绘制扩展列表和安装按钮。
        /// </summary>
        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scroll.scrollPosition;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("可选编辑器扩展", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("选择要安装到当前项目的工具。", MessageType.Info);
                foreach (var extension in Extensions)
                {
                    DrawExtension(extension);
                }

                if (s_Request != null)
                {
                    EditorGUILayout.HelpBox("正在安装 " + s_Installing + "…", MessageType.Info);
                }

                if (!string.IsNullOrEmpty(s_Error))
                {
                    EditorGUILayout.HelpBox(s_Error, MessageType.Error);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("在 Package Manager 中更新或移除工具。", EditorStyles.wordWrappedLabel);
            }
        }

        /// <summary>
        /// 绘制单个扩展的状态和操作。
        /// </summary>
        /// <param name="extension">扩展信息。</param>
        private void DrawExtension(Extension extension)
        {
            var installed = m_Packages.FirstOrDefault(p => p.name == extension.PackageName);
            bool legacy = installed == null && HasAssembly(extension);
            bool compatible = SupportsUnity(Application.unityVersion, extension.MinimumUnity);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(extension.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(extension.Description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField(extension.LicenseInfo, EditorStyles.wordWrappedLabel);
                string status = installed != null ? "已安装 " + installed.version : legacy ? "已检测到本地工具" : "未安装";
                EditorGUILayout.LabelField(status + "  ·  Unity " + extension.MinimumUnity + "+", EditorStyles.miniLabel);
                if (!compatible && installed == null && !legacy)
                {
                    EditorGUILayout.LabelField("当前 Unity 版本不满足此 Git 包的要求。", EditorStyles.wordWrappedLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("仓库", GUILayout.Width(70)))
                    {
                        Application.OpenURL(extension.Website);
                    }

                    if (installed != null)
                    {
                        if (GUILayout.Button("在 Package Manager 中查看"))
                        {
                            UnityEditor.PackageManager.UI.Window.Open(extension.PackageName);
                        }
                    }
                    else
                    {
                        using (new EditorGUI.DisabledScope(legacy || !compatible || !CanStartInstallation))
                        {
                            if (GUILayout.Button(legacy ? "已安装" : "安装"))
                            {
                                Install(extension);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 检查扩展程序集是否已安装。
        /// </summary>
        /// <param name="extension">扩展信息。</param>
        /// <returns>是否已安装程序集。</returns>
        private static bool HasAssembly(Extension extension)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == extension.AssemblyName);
        }

        /// <summary>
        /// 比较当前 Unity 与扩展最低版本。
        /// </summary>
        /// <param name="current">当前 Unity 版本。</param>
        /// <param name="minimum">扩展要求的最低 Unity 版本。</param>
        /// <returns>当前 Unity 是否满足最低版本。</returns>
        private static bool SupportsUnity(string current, string minimum)
        {
            string[] parts = current.Split('.');
            return parts.Length >= 2 && int.TryParse(parts[0], out int major) && int.TryParse(parts[1], out int minor) && new System.Version(major, minor) >= new System.Version(minimum);
        }

        /// <summary>
        /// 通过 Package Manager 安装扩展。
        /// </summary>
        /// <param name="extension">扩展信息。</param>
        private static void Install(Extension extension)
        {
            // Preserve existing installations and pinned revisions, including tools imported into Assets.
            if (!CanStartInstallation || !SupportsUnity(Application.unityVersion, extension.MinimumUnity) || HasAssembly(extension) || PackageInfo.GetAllRegisteredPackages().Any(p => p.name == extension.PackageName))
            {
                return;
            }

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

        /// <summary>
        /// 轮询扩展安装请求。
        /// </summary>
        private static void PollInstallation()
        {
            if (s_Request == null || !s_Request.IsCompleted)
            {
                return;
            }

            if (s_Request.Status == StatusCode.Failure)
            {
                s_Error = "安装 " + s_Installing + " 失败：" + (s_Request.Error?.message ?? "未知错误");
            }

            s_Request = null;
            s_Installing = null;
            EditorApplication.update -= PollInstallation;
            foreach (var window in Resources.FindObjectsOfTypeAll<QgfExtensionsWindow>())
            {
                window.RefreshPackages();
            }
        }
    }
}
