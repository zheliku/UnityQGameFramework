#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class OneShotSoundExample : MonoBehaviour
    {
        [SerializeField] private string soundGuid;
        [SerializeField] private Button playButton;
        [SerializeField] private Text outputText;
        private string asset;
        private int serialId;

        private void Start()
        {
            asset = UnityEditor.AssetDatabase.GUIDToAssetPath(soundGuid);
            // 组统一控制音量；一个代理同时播放一路声音。
            QGF.Sound.AddSoundGroup("Prompt", false, false, 0.25f, 1);
            playButton.onClick.AddListener(() =>
            {
                if (serialId != 0) QGF.Sound.StopSound(serialId);
                serialId = QGF.Sound.PlaySound(asset, "Prompt");
                outputText.text = "已请求播放提示音";
            });
        }

        private void Update()
        {
            playButton.interactable = !QGF.Sound.IsLoadingSound(serialId);
        }

        private void OnDestroy()
        {
            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null && serialId != 0) sound.StopSound(serialId);
        }
    }
}
#endif
