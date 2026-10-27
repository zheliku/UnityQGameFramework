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
        [SerializeField] private Dropdown ruleDropdown;
        [SerializeField] private Button loadButton, releaseButton;
        [SerializeField] private Text ruleText, stateText;

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
            stateText.text = "选择寻址规则，再加载同一个 Cube。无需先初始化包。";
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
                stateText.text = "已用地址 " + address + " 加载 Cube。归还后可切换规则对比。";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = "加载失败：" + exception.Message;
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
                stateText.text = "资产已归还，可以切换规则。";
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
            ruleText.text = "规则：" + Rules[index] + "\n地址：" + Addresses[index] + "\n包：" + PackageNames[index];
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
