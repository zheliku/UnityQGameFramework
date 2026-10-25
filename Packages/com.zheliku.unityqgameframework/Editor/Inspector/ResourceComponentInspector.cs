//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;
using UnityEditor;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Editor
{
    [CustomEditor(typeof(ResourceComponent))]
    internal sealed class ResourceComponentInspector : FrameworkInspector
    {
        /// <summary>
        /// 绘制资源组件面板。
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            ResourceComponent component = (ResourceComponent)target;
            SerializedProperty settingsProperty = serializedObject.FindProperty("m_Settings");
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUILayout.PropertyField(settingsProperty, new GUIContent("Resource Settings"));
            }

            ResourceSettings settings = settingsProperty.objectReferenceValue as ResourceSettings;
            if (settings == null)
            {
                DrawMissingSettings(component, settingsProperty);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.LabelField("Default Package", settings.DefaultPackageName);
            for (int index = 0; index < settings.Packages.Count; index++)
            {
                ResourcePackageSettings package = settings.Packages[index];
                if (package == null)
                {
                    continue;
                }

                DrawPackage(settings, package);
            }

            EditorGUILayout.HelpBox("Add asset collectors to each build profile before building the package.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_ReadWritePathType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MinUnloadUnusedAssetsInterval"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_MaxUnloadUnusedAssetsInterval"));

            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.LabelField("Read-Only Path", component.ReadOnlyPath);
                EditorGUILayout.LabelField("Read-Write Path", component.ReadWritePath);
                EditorGUILayout.LabelField("Asset Count", component.AssetCount.ToString());
                EditorGUILayout.LabelField("Loading Requests", component.LoadWorkingAgentCount.ToString());
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawMissingSettings(ResourceComponent component, SerializedProperty settingsProperty)
        {
            EditorGUILayout.HelpBox("Resource Settings is required for YooAsset loading. It is stored in Assets and assigned to this component; importing QGF does not change project settings.", MessageType.Warning);

            string componentPath = AssetDatabase.GetAssetPath(component);
            bool packageAsset = EditorUtility.IsPersistent(component) && componentPath.StartsWith("Packages/", StringComparison.Ordinal);
            if (packageAsset)
            {
                EditorGUILayout.HelpBox("Create a scene instance of this package prefab, then assign its Resource Settings.", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || packageAsset))
            {
                if (GUILayout.Button("Create Resource Settings"))
                {
                    CreateResourceSettings(settingsProperty.serializedObject, settingsProperty);
                }
            }
        }

        private static void DrawPackage(ResourceSettings settings, ResourcePackageSettings package)
        {
            ResourceBuildProfile profile = ResourceBuildService.FindProfile(settings, package.Name);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(package.Name, package.PlayMode.ToString());

            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (profile == null)
                {
                    if (GUILayout.Button("Create Profile", GUILayout.Width(110)))
                    {
                        profile = CreateResourceBuildProfile(settings, package.Name);
                        Selection.activeObject = profile;
                    }
                }
                else
                {
                    if (GUILayout.Button(settings.UseEditorSimulation ? "Build Simulation" : "Build Player", GUILayout.Width(125)))
                    {
                        ResourceBuildService.Build(profile, settings.UseEditorSimulation);
                    }
                    if (GUILayout.Button("Select", GUILayout.Width(64)))
                    {
                        Selection.activeObject = profile;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void CreateResourceSettings(SerializedObject serializedObject, SerializedProperty settingsProperty)
        {
            EnsureFolder("Assets/UnityQGameFramework/Resource/Editor");

            ResourceSettings settings = CreateInstance<ResourceSettings>();
            string settingsPath = AssetDatabase.GenerateUniqueAssetPath("Assets/UnityQGameFramework/Resource/ResourceSettings.asset");
            AssetDatabase.CreateAsset(settings, settingsPath);

            foreach (ResourcePackageSettings package in settings.Packages)
            {
                if (package != null)
                {
                    CreateResourceBuildProfile(settings, package.Name);
                }
            }

            settingsProperty.objectReferenceValue = settings;
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(settings);
        }

        private static ResourceBuildProfile CreateResourceBuildProfile(ResourceSettings settings, string packageName)
        {
            EnsureFolder("Assets/UnityQGameFramework/Resource/Editor");

            ResourceBuildProfile profile = CreateInstance<ResourceBuildProfile>();
            profile.Initialize(settings, packageName);
            string fileName = packageName.Replace('/', '_').Replace('\\', '_');
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/UnityQGameFramework/Resource/Editor/" + fileName + ".asset");
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string folderName = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
