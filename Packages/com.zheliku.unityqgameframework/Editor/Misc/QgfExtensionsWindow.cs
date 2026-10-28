using UnityEditor;
using UnityEngine;

namespace UnityQGameFramework.Editor
{
    /// <summary>
    /// 显示推荐扩展的用途与 Git 安装地址。
    /// </summary>
    public sealed class QgfExtensionsWindow : EditorWindow
    {
        private Vector2 m_Scroll;

        /// <summary>
        /// 打开可选扩展说明面板。
        /// </summary>
        [MenuItem("QGF/Extensions", false, 60)]
        public static void Open()
        {
            var window = GetWindow<QgfExtensionsWindow>("QGF Extensions");
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        /// <summary>
        /// 绘制推荐扩展和 Git 安装地址。
        /// </summary>
        private void OnGUI()
        {
            using (var scroll = new EditorGUILayout.ScrollViewScope(m_Scroll))
            {
                m_Scroll = scroll.scrollPosition;
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Optional Editor Extensions", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("Copy a Git URL into Package Manager > Install package from git URL. Manage versions and removal in Package Manager.", MessageType.Info);
                DrawExtension("Alchemy", "Inspector, Hierarchy and EditorWindow enhancements. Serialization extensions also require Unity.Serialization.",
                    "https://github.com/annulusgames/Alchemy.git?path=/Alchemy/Assets/Alchemy", "Unity 2021.2+ · MIT");
                DrawExtension("Rainbow Folders 2", "Folder colors, icons and rules for the Project window.",
                    "https://github.com/Borod4r/Rainbow-Folders-2.git?path=Assets/Plugins/Borodar/RainbowFolders",
                    "Unity 6000.3+ · Apache-2.0 for code; CC BY-NC 4.0 for artwork. Check upstream authorization before commercial use.");
            }
        }

        /// <summary>
        /// 绘制扩展说明和可复制的安装地址。
        /// </summary>
        /// <param name="name">扩展名称。</param>
        /// <param name="description">用途说明。</param>
        /// <param name="gitUrl">Git 安装地址。</param>
        /// <param name="requirements">Unity 版本及许可证说明。</param>
        private static void DrawExtension(string name, string description, string gitUrl, string requirements)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField(requirements, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.SelectableLabel(gitUrl, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("Copy Git URL"))
                {
                    EditorGUIUtility.systemCopyBuffer = gitUrl;
                }
            }
        }
    }
}
