using TMPro;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 用三种官方寻址规则加载同一个预制体。
    /// </summary>
    public sealed class LoadAddressRules : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown ruleDropdown;
        [SerializeField] private Button loadButton, releaseButton;
        [SerializeField] private TMP_Text ruleText, stateText;

        private static readonly string[] PackageNames =
        {
            "QgfExamples.Modules.ByFileName",
            "QgfExamples.Modules.ByFolder",
            "QgfExamples.Modules.ByGroup"
        };

        private static readonly string[] Addresses = { "Cube", "Art_Cube", "Demo_Cube" };
        private static readonly string[] Rules = { "AddressByFileName", "AddressByFolderAndFileName", "AddressByGroupAndFileName" };
        private GameObject asset, instance;
        private string loadedPackage;
        private bool busy;

        private void Start()
        {
            loadButton.onClick.AddListener(Load);
            releaseButton.onClick.AddListener(Release);
            stateText.text = "Choose an addressing rule, then load the same Cube. Package initialization is automatic.";
        }

        private async void Load()
        {
            busy = true;
            loadedPackage = PackageNames[ruleDropdown.value];
            string address = Addresses[ruleDropdown.value];
            try
            {
                // 地址由 YooAsset Collector 中的规则生成，三个包收集的是同一份 Cube.prefab。
                // 指定包名后，QGF 自动初始化对应包；这里不需要 GUID 或编辑器 API。
                asset = await QGF.Resource.LoadAssetAsync<GameObject>(address, loadedPackage, cancellationToken: destroyCancellationToken);
                instance = Instantiate(asset, new Vector3(3, -1, 0), Quaternion.Euler(20, 35, 0));
                stateText.text = "Loaded using address " + address + " to load Cube. Release it to compare another rule.";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = "Load failed: " + exception.Message;
            }
            finally
            {
                busy = false;
            }
        }

        private async void Release()
        {
            busy = true;
            Destroy(instance);
            try
            {
                // 实例销毁后，将资产归还给借用时指定的包。
                await UniTask.NextFrame(destroyCancellationToken);
                QGF.Resource.UnloadAsset(asset, loadedPackage);
                asset = null;
                stateText.text = "Asset released. You can switch addressing rules.";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            finally
            {
                busy = false;
            }
        }

        private void Update()
        {
            int index = ruleDropdown.value;
            ruleText.text = "Rule: " + Rules[index] + "\nAddress: " + Addresses[index] + "\nPackage: " + PackageNames[index];
            ruleDropdown.interactable = !busy && asset == null;
            loadButton.interactable = !busy && asset == null;
            releaseButton.interactable = !busy && asset != null;
        }

        private void OnDestroy()
        {
            if (instance != null)
            {
                Destroy(instance);
            }
            var resources = GameEntry.GetComponent<ResourceComponent>();
            if (resources != null && asset != null)
            {
                resources.UnloadAsset(asset, loadedPackage);
            }
        }
    }
}
