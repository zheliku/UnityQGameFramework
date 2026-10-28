using System;
using System.IO;
using StarForce;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 在隔离工程制作启动窗口；变体继承 DialogForm 的布局与贴图。
public static class BuildBuiltinDialog
{
    public static object Style()
    {
        if (!Application.dataPath.Contains("Artifacts~") || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Use the stopped Consumer project.");
        const string path = "Assets/Examples/02_StarForce/GameMain/UI/UIForms/DialogForm.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var mask = root.transform.Find("Panel_Mask");
            var panel = (RectTransform)mask.Find("Panel_Background");
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, .5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(680, 340);
            var size = panel.GetComponent<LayoutElement>() ?? panel.gameObject.AddComponent<LayoutElement>();
            size.minWidth = 0;
            size.preferredWidth = 680;
            size.flexibleWidth = 0;
            var layout = mask.GetComponent<HorizontalLayoutGroup>() ?? mask.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 0, 0);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            mask.GetComponent<Image>().color = new Color(.01f, .02f, .03f, .4f);
            var image = panel.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Examples/02_StarForce/GameMain/UI/UISprites/Common/dialog-background.png");
            if (image.sprite == null)
                throw new InvalidOperationException("The opaque dialog background sprite could not be loaded.");
            image.type = Image.Type.Sliced;
            image.color = new Color(.025f, .07f, .1f, 1f);
            var message = (RectTransform)panel.Find("Text_Message");
            message.sizeDelta = new Vector2(-64, -176);
            message.anchoredPosition = new Vector2(0, 16);
            message.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return new
            {
                styled = path,
                maximumWidth = 680,
                minimumSideMargin = 24
            };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static object Run()
    {
        if (!Application.dataPath.Contains("Artifacts~") || !File.Exists("QgfExampleConsumer~.json"))
            throw new InvalidOperationException("Use the isolated Consumer project.");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play first.");
        const string folder = "Assets/Examples/02_StarForce/GameMain/";
        const string path = folder + "UI/UIForms/BuiltinDialogForm.prefab";
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "UI/UIForms/DialogForm.prefab");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);
            var original = instance.GetComponent<DialogForm>();
            var builtin = instance.AddComponent<BuiltinDialogForm>();
            var from = new SerializedObject(original);
            var to = new SerializedObject(builtin);
            foreach (string field in new[] { "m_TitleText", "m_MessageText", "m_ModeObjects", "m_ConfirmTexts", "m_CancelTexts", "m_OtherTexts" })
                to.CopyFromSerializedProperty(from.FindProperty(field));
            to.ApplyModifiedPropertiesWithoutUndo();
            foreach (var button in instance.GetComponentsInChildren<CommonButton>(true))
            {
                var serialized = new SerializedObject(button);
                foreach (string name in new[] { "m_OnHover", "m_OnClick" })
                {
                    var calls = serialized.FindProperty(name + ".m_PersistentCalls.m_Calls");
                    for (int i = calls.arraySize - 1; i >= 0; i--)
                    {
                        var call = calls.GetArrayElementAtIndex(i);
                        if (call.FindPropertyRelative("m_MethodName").stringValue == "PlayUISound")
                            calls.DeleteArrayElementAtIndex(i); // UI 音效表尚未加载。
                        else
                        {
                            call.FindPropertyRelative("m_Target").objectReferenceValue = builtin;
                            call.FindPropertyRelative("m_TargetAssemblyTypeName").stringValue = typeof(BuiltinDialogForm).AssemblyQualifiedName;
                        }
                    }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            UnityEngine.Object.DestroyImmediate(original);
            var font = AssetDatabase.LoadAssetAtPath<Font>(folder + "Fonts/MainFont.ttf");
            foreach (var text in instance.GetComponentsInChildren<Text>(true))
                text.font = font;
            var canvas = instance.GetComponent<Canvas>();
            canvas.sortingOrder = 30000;
            instance.AddComponent<GraphicRaycaster>();
            var scaler = instance.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(768, 1024);
            scaler.matchWidthOrHeight = 1;
            instance.name = "BuiltinDialogForm";
            var background = instance.transform.Find("Panel_Mask/Panel_Background").GetComponent<Image>();
            if (background.sprite == null || background.color.a < 1f)
                throw new InvalidOperationException("The dialog card must use an opaque background sprite.");
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab.GetComponent<BuiltinDialogForm>(), out string guid, out long fileId);
            return new
            {
                path,
                guid,
                fileId,
                variant = PrefabUtility.GetPrefabAssetType(prefab).ToString()
            };
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
}
