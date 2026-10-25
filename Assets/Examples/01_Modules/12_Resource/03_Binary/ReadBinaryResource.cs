using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 读取原生文件，并在业务侧提取所需的字节片段。
    /// </summary>
    public sealed class ReadBinaryResource : MonoBehaviour
    {
        [SerializeField] private Button initializeButton, readButton;
        [SerializeField] private Text stateText, pathText, bytesText;
        private byte[] result;
        private bool initialized, loading;

        private void Start()
        {
            stateText.text = "初始化 Raw 包，读取文件，再提取偏移 8 开始的 16 个字节。";
            initializeButton.onClick.AddListener(Initialize);
            readButton.onClick.AddListener(ReadFile);
        }

        private async void Initialize()
        {
            loading = true;
            try
            {
                await QGF.Resource.InitializePackageAsync(ResourceExamplePaths.RawPackageName, destroyCancellationToken);
                initialized = true;
                stateText.text = "原生文件包已就绪。";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = exception.Message;
            }
            finally
            {
                loading = false;
            }
        }

        private async void ReadFile()
        {
            loading = true;
            try
            {
                // 返回的 bytes 已脱离资源句柄，读取完成后无需再手动 UnloadAsset。
                byte[] bytes = await QGF.Resource.LoadBinaryAsync(ResourceExamplePaths.BinaryGuid, ResourceExamplePaths.RawPackageName, destroyCancellationToken);
                const int offset = 8;
                int length = Math.Min(16, Math.Max(0, bytes.Length - offset));
                result = new byte[length];
                Buffer.BlockCopy(bytes, Math.Min(offset, bytes.Length), result, 0, length);
                pathText.text = $"资源包：{ResourceExamplePaths.RawPackageName}\n完整文件：{bytes.Length} 字节；本次片段：{length} 字节";
                bytesText.text = BitConverter.ToString(result);
                stateText.text = "文件内容属于本次读取；片段提取由业务代码完成。";
            }
            catch (OperationCanceledException) when (destroyCancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                stateText.text = exception.Message;
            }
            finally
            {
                loading = false;
            }
        }

        private void Update()
        {
            initializeButton.interactable = !initialized && !loading;
            readButton.interactable = initialized && !loading;
        }
    }
}
