using TMPro;
using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 验证资源寻址示例中的三种 YooAsset 地址。
/// </summary>
public static class ResourceAddressExamplesValidation
{
    /// <summary>
    /// 运行寻址规则示例并检查每个地址都能创建实例。
    /// </summary>
    /// <returns>验证结果。</returns>
    public static async Task<object> Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Use the isolated validation project in Edit Mode.");
        }
        EditorSceneManager.OpenScene("Assets/Examples/01_Modules/12_Resource/02_AddressRules/LoadAddressRules.unity");
        await SetPlayMode(true);
        try
        {
            var example = Find("LoadAddressRules");
            Type type = example.GetType();
            FieldInfo rule = type.GetField("ruleDropdown", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo load = type.GetField("loadButton", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo release = type.GetField("releaseButton", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo asset = type.GetField("asset", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo instance = type.GetField("instance", BindingFlags.Instance | BindingFlags.NonPublic);
            var dropdown = (TMP_Dropdown)rule.GetValue(example);
            var loadButton = (Button)load.GetValue(example);
            var releaseButton = (Button)release.GetValue(example);
            for (int index = 0; index < 3; index++)
            {
                dropdown.value = index;
                loadButton.onClick.Invoke();
                await Until(() => asset.GetValue(example) != null, "Address did not load: " + index);
                if (instance.GetValue(example) == null)
                {
                    throw new InvalidOperationException("Address load did not create an instance: " + index);
                }
                releaseButton.onClick.Invoke();
                await Until(() => asset.GetValue(example) == null, "Address asset was not returned: " + index);
            }
            return new
            {
                passed = true,
                rules = new[] { "AddressByFileName", "AddressByFolderAndFileName", "AddressByGroupAndFileName" }
            };
        }
        finally
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                await SetPlayMode(false);
            }
        }
    }

    private static MonoBehaviour Find(string name)
    {
        foreach (MonoBehaviour component in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (component.GetType().Name == name)
            {
                return component;
            }
        }
        throw new InvalidOperationException("Example not found: " + name);
    }

    private static async Task Until(Func<bool> condition, string message)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException(message);
            }
            await Task.Delay(30);
        }
    }

    private static async Task SetPlayMode(bool playing)
    {
        var completion = new TaskCompletionSource<bool>();
        Action<PlayModeStateChange> handler = state =>
        {
            if (state == (playing ? PlayModeStateChange.EnteredPlayMode : PlayModeStateChange.EnteredEditMode))
            {
                completion.TrySetResult(true);
            }
        };
        EditorApplication.playModeStateChanged += handler;
        try
        {
            EditorApplication.isPlaying = playing;
            if (await Task.WhenAny(completion.Task, Task.Delay(30000)) != completion.Task)
            {
                throw new TimeoutException("Play Mode transition");
            }
        }
        finally
        {
            EditorApplication.playModeStateChanged -= handler;
        }
    }
}
