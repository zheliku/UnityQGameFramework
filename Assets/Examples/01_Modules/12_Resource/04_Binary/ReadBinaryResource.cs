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
            stateText.text = "Initialize the Raw package, read the file, then extract 16 bytes starting at offset 8.";
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
                stateText.text = "Raw file package ready.";
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
                byte[] bytes = await QGF.Resource.LoadBinaryAsync(ResourceExamplePaths.BinaryAddress, ResourceExamplePaths.RawPackageName, destroyCancellationToken);
                const int offset = 8;
                int length = Math.Min(16, Math.Max(0, bytes.Length - offset));
                result = new byte[length];
                Buffer.BlockCopy(bytes, Math.Min(offset, bytes.Length), result, 0, length);
                pathText.text = $"Resource package: {ResourceExamplePaths.RawPackageName}\nFull file: {bytes.Length} bytes; selected segment: {length} bytes";
                bytesText.text = BitConverter.ToString(result);
                stateText.text = "The returned contents belong to this read. Application code extracts the segment.";
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
