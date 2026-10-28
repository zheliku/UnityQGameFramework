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
        var packages = PackageInfo.GetAllRegisteredPackages();
        string[] optional = { "com.annulusgames.alchemy", "com.borodar.rainbow-folders" };
        bool absent = packages.All(p => !optional.Contains(p.name));
        Require(!requireAbsent || absent, "This consumer must not install optional tools");
        string manifest = File.ReadAllText("Packages/manifest.json");
        var references = type.Assembly.GetReferencedAssemblies();
        Require(references.All(a => !a.Name.StartsWith("Alchemy", StringComparison.Ordinal) && !a.Name.StartsWith("Borodar", StringComparison.Ordinal)), "Framework Editor depends on a plugin assembly");
        Require(type.GetMethod("Install", flags) == null && type.GetField("s_Request", flags) == null,
            "The recommendation window must not manage package installation");

        QgfExtensionsWindow.Open();
        try
        {
            Require(File.ReadAllText("Packages/manifest.json") == manifest, "The extension window changed the project manifest");
        }
        finally
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<QgfExtensionsWindow>())
                window.Close();
        }
        return new Result
        {
            passed = true,
            optionalPluginsAbsent = absent,
            noImplicitInstall = true,
            noPluginAssemblyReferences = true,
            extensions = 2,
            unityVersion = Application.unityVersion
        };
    }

    private static void Require(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
    }
}
