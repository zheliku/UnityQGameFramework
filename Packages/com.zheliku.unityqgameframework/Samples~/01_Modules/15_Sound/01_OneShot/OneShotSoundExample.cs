using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 通过 QGF.Sound 播放一次性提示音。
    /// </summary>
    public sealed class OneShotSoundExample : MonoBehaviour
    {
        private const string AssetName = "Sound_OneShot_Tone";
        [SerializeField]
        private Button playButton;
        [SerializeField]
        private Text outputText;
        private int serialId;
        private void Start()
        {
            // 组统一控制音量；一个代理同时播放一路声音。
            QGF.Sound.AddSoundGroup("Prompt", false, false, 0.25f, 1);
            playButton.onClick.AddListener(() =>
            {
                if (serialId != 0)
                {
                    QGF.Sound.StopSound(serialId);
                }

                serialId = QGF.Sound.PlaySound(AssetName, "Prompt");
                outputText.text = "Requested notification sound playback";
            });
        }

        private void Update()
        {
            playButton.interactable = !QGF.Sound.IsLoadingSound(serialId);
        }

        private void OnDestroy()
        {
            var sound = GameEntry.GetComponent<SoundComponent>();
            if (sound != null && serialId != 0)
            {
                sound.StopSound(serialId);
            }
        }
    }
}
