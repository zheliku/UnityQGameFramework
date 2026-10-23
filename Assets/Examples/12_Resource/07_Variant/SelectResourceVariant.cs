#if UNITY_EDITOR
using System;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Core.Resource;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class SelectResourceVariant : MonoBehaviour
    {
        [SerializeField] private Button initializeButton, loadButton;
        [SerializeField] private Dropdown variantMode;
        [SerializeField] private Toggle customDecryptToggle;
        [SerializeField] private ResourceComponent resourceComponent;
        [SerializeField] private Text stateText, assetsText, payloadText, decryptText;
        private ResourceFixtureManifest fixture;
        private GameObject asset, instance;
        private string selectedAsset, otherAsset;
        private bool ready, initialized, loading;
        private int decryptCount;
        private VariantPayload payload;

        [Serializable]
        private sealed class VariantPayload
        {
            // JsonUtility 按字段名填入变体文件中的值。
            public string Name = string.Empty;
            public string Color = string.Empty;
            public int Damage = 0;
            public float Scale = 0f;
        }

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                fixture = ResourceExamplePaths.Fixture;
                stateText.text = fixture == null ? ResourceExamplePaths.Notice : "选择变体后初始化；重新运行可比较另一个变体。";
                initializeButton.onClick.AddListener(Initialize);
                loadButton.onClick.AddListener(Load);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private async void Initialize()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                loading = true;
                bool hard = variantMode.value == 1;
                selectedAsset = hard ? fixture.HardVariantAsset : fixture.NormalVariantAsset;
                otherAsset = hard ? fixture.NormalVariantAsset : fixture.HardVariantAsset;
                // 解密回调必须先于加载代理创建；不设置时使用框架内置处理。
                DecryptResourceCallback decrypt = null;
                if (customDecryptToggle.isOn) decrypt = Decrypt;
                resourceComponent.SetDecryptResourceCallback(decrypt);
                resourceComponent.enabled = true;
                await UniTask.NextFrame(cancellation);
                // 初始化时只登记当前变体和无变体资源；初始化后不能再切换 CurrentVariant。
                QGF.Resource.SetCurrentVariant(hard ? "hard" : "normal");
                QGF.Resource.InitResources(() =>
                {
                    if (this == null) return;
                    initialized = true;
                    loading = false;
                    stateText.text = $"当前变体 {QGF.Resource.CurrentVariant}，已登记 {QGF.Resource.AssetCount} 个资产。";
                });
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { loading = false; stateText.text = "初始化失败：" + exception.Message; }
        }

        private void Load()
        {
            loading = true;
            var resources = QGF.Resource;
            // Cube 没有变体，两种模式都能加载；颜色、大小和数值来自当前变体的二进制资源。
            resources.LoadAsset(fixture.PrefabAssetName, typeof(GameObject), new LoadAssetCallbacks(
                (name, value, duration, context) =>
                {
                    if (this == null) { if (resources != null) resources.UnloadAsset(value); return; }
                    asset = (GameObject)value;
                    instance = Instantiate(asset, new Vector3(3, -3.3f, 0), Quaternion.Euler(20, 35, 0));
                    resources.LoadBinary(selectedAsset, new LoadBinaryCallbacks(
                        (binaryName, bytes, seconds, userData) =>
                        {
                            if (this == null) return;
                            loading = false;
                            try
                            {
                                payload = JsonUtility.FromJson<VariantPayload>(Encoding.UTF8.GetString(bytes));
                                if (payload == null || payload.Scale <= 0 || !ColorUtility.TryParseHtmlString(payload.Color, out var color))
                                    throw new FormatException("Variant payload is invalid.");
                                instance.transform.localScale = Vector3.one * payload.Scale;
                                var properties = new MaterialPropertyBlock();
                                properties.SetColor("_BaseColor", color);
                                instance.GetComponent<Renderer>().SetPropertyBlock(properties);
                                payloadText.text = $"{payload.Name}\n伤害 {payload.Damage}    大小 {payload.Scale:F1}\n颜色 {payload.Color}";
                                stateText.text = "已读取当前变体，并应用到 Cube。";
                            }
                            catch (Exception exception) { stateText.text = "数据解析失败：" + exception.Message; }
                        },
                        (binaryName, status, message, userData) =>
                        {
                            if (this == null) return;
                            loading = false;
                            stateText.text = "读取失败：" + message;
                        }));
                },
                (name, status, message, context) =>
                {
                    if (this == null) return;
                    loading = false;
                    stateText.text = "Cube 加载失败：" + message;
                }));
        }

        private void Decrypt(byte[] bytes, int startIndex, int count, string name, string variant, string extension,
            bool storageInReadOnly, string fileSystem, byte loadType, int length, int hashCode)
        {
            // 与构建器的内置异或处理一致，直接还原指定区间。改用其他算法时，构建端也要对应修改。
            byte[] key = Utility.Converter.GetBytes(hashCode);
            // 构建示例给 normal 选择了快速格式：只处理开头 220 字节，其余内容保持原样。
            int lengthToDecode = variant == "normal" ? Math.Min(count, 220) : count;
            for (int index = 0; index < lengthToDecode; index++) bytes[startIndex + index] ^= key[index % key.Length];
            decryptCount++;
            decryptText.text = $"自定义回调 {decryptCount} 次\n{name}.{variant}.{extension}，{count} 字节\n只读区：{storageInReadOnly}";
        }

        private void Update()
        {
            if (!ready) return;
            initializeButton.interactable = fixture != null && !initialized && !loading;
            variantMode.interactable = customDecryptToggle.interactable = !initialized && !loading;
            loadButton.interactable = initialized && !loading && asset == null;
            if (!initialized) return;
            assetsText.text = $"当前变体：{ResourceExamplePaths.GetAssetStatusText(QGF.Resource.HasAsset(selectedAsset))}\n"
                + $"其他变体：{ResourceExamplePaths.GetAssetStatusText(QGF.Resource.HasAsset(otherAsset))}\n"
                + $"共同 Cube：{ResourceExamplePaths.GetAssetStatusText(QGF.Resource.HasAsset(fixture.PrefabAssetName))}\n"
                + $"游戏版本 {QGF.Resource.ApplicableGameVersion}，资源版本 {QGF.Resource.InternalResourceVersion}";
            if (!customDecryptToggle.isOn) decryptText.text = "使用框架内置还原处理；自定义回调未注册。";
        }

        private void OnDestroy()
        {
            if (instance != null) Destroy(instance);
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null) resources.UnloadAsset(asset);
        }
    }
}
#endif
