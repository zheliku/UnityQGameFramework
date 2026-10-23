using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples
{
    public sealed class ReadTranslation : MonoBehaviour
    {
        [SerializeField] private Text titleText, rewardText, missingText, formatText;
        [SerializeField] private Dropdown argumentCount;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Button rewardButton;
        [SerializeField] private TextAsset dictionary;
        private int coins;
        private bool ready;

        private async void Start()
        {
            var cancellation = destroyCancellationToken;
            try
            {
                await UniTask.NextFrame(cancellation);
                // Language 选择 TOML 的语言子树；ParseData 将该语言的翻译装入字典。
                QGF.Localization.Language = Language.ChineseSimplified;
                QGF.Localization.ParseData(dictionary.text);
                titleText.text = QGF.Localization.GetString("Game.Name");
                missingText.text = "缺少翻译时：" + QGF.Localization.GetString("Unknown.Key");
                rewardButton.onClick.AddListener(() => coins += 10);
                ready = true;
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (Exception exception) { Log.Error(exception); }
        }

        private void Update()
        {
            if (!ready) return;
            // {0} 与 {1} 在翻译中决定语序，业务代码只传玩家名字和数量。
            rewardText.text = QGF.Localization.GetString("Reward.Coins", nameInput.text, coins);
            // 同一组格式化重载支持 1 至 16 个参数，翻译中的占位符从 {0} 开始。
            string key = "Format.Args" + (argumentCount.value + 1);
            string player = nameInput.text;
            switch (argumentCount.value)
            {
                case 0: formatText.text = QGF.Localization.GetString(key, player); break;
                case 1: formatText.text = QGF.Localization.GetString(key, player, coins); break;
                case 2: formatText.text = QGF.Localization.GetString(key, player, coins, 3); break;
                case 3: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4); break;
                case 4: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5); break;
                case 5: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6); break;
                case 6: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7); break;
                case 7: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8); break;
                case 8: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8, 9); break;
                case 9: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8, 9, 10); break;
                case 10: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8, 9, 10, 11); break;
                case 11: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12); break;
                case 12: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13); break;
                case 13: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14); break;
                case 14: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15); break;
                default: formatText.text = QGF.Localization.GetString(key, player, coins, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16); break;
            }
        }
    }
}
