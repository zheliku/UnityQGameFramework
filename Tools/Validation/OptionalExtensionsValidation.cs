using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityQGameFramework.Editor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

public static class OptionalExtensionsValidation
{
    [Serializable]
    private sealed class Result
    {
        public bool passed, optionalPluginsAbsent, noImplicitInstall, noPluginAssemblyReferences;
        public int extensions;
        public string unityVersion;
    }

    public static void Run()
    {
        try
        {
            Directory.CreateDirectory("Results");
            File.WriteAllText("Results/optional-extensions.json", JsonUtility.ToJson(Inspect(true), true));
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    public static object RunInstalled() => Inspect(false);

    private static Result Inspect(bool requireAbsent)
    {
        var type = typeof(QgfExtensionsWindow);
        const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
        var extensions = (Array)type.GetField("Extensions", flags).GetValue(null);
        Require(extensions.Length == 2, "Unexpected extension catalog");
        var packages = PackageInfo.GetAllRegisteredPackages();
        string[] optional = { "com.annulusgames.alchemy", "com.borodar.rainbow-folders" };
        bool absent = packages.All(p => !optional.Contains(p.name));
        Require(!requireAbsent || absent, "This consumer must not install optional tools");
        string manifest = File.ReadAllText("Packages/manifest.json");
        var references = type.Assembly.GetReferencedAssemblies();
        Require(references.All(a => !a.Name.StartsWith("Alchemy", StringComparison.Ordinal) && !a.Name.StartsWith("Borodar", StringComparison.Ordinal)), "Framework Editor depends on a plugin assembly");
        var supports = type.GetMethod("SupportsUnity", flags);
        Require((bool)supports.Invoke(null, new object[] { "6000.3.26f1", "6000.3" }), "Unity 6.3 should support Rainbow Folders");
        Require(!(bool)supports.Invoke(null, new object[] { "2022.3.62f3", "6000.3" }), "Older Unity should disable this Rainbow Folders Git package");
        Require((bool)supports.Invoke(null, new object[] { "2022.3.62f3", "2021.2" }), "Alchemy minimum version check failed");

        QgfExtensionsWindow.Open();
        try
        {
            foreach (var extension in extensions)
            {
                string name = (string)extension.GetType().GetProperty("PackageName").GetValue(extension);
                if (requireAbsent)
                    Require(!(bool)type.GetMethod("HasAssembly", flags).Invoke(null, new[] { extension }), "Plugin unexpectedly loaded: " + name);
                else
                {
                    Require(packages.Any(p => p.name == name), "Expected an existing plugin: " + name);
                    type.GetMethod("Install", flags).Invoke(null, new[] { extension });
                }
            }
            Require(type.GetField("s_Request", flags).GetValue(null) == null, "Opening the window or selecting an existing tool started an installation");
            Require(File.ReadAllText("Packages/manifest.json") == manifest, "The extension window changed the project manifest");
        }
        finally
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<QgfExtensionsWindow>()) window.Close();
        }
        return new Result { passed = true, optionalPluginsAbsent = absent, noImplicitInstall = true,
            noPluginAssemblyReferences = true, extensions = extensions.Length, unityVersion = Application.unityVersion };
    }

    private static void Require(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
}
