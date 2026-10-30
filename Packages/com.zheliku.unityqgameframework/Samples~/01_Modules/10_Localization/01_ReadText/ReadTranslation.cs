using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示查询与格式化翻译。
    /// </summary>
    public sealed class ReadTranslation : MonoBehaviour
    {
        [SerializeField] private TextAsset dictionary;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Button rewardButton;
        [SerializeField] private Text titleText, rewardText;
        private int coins;

        private void Start()
        {
            // 语言决定读取 TOML 中哪个子树。
            QGF.Localization.Language = Language.English;
            QGF.Localization.ParseData(dictionary.text);
            titleText.text = QGF.Localization.GetString("Game.Name");
            rewardButton.onClick.AddListener(() =>
            {
                coins += 10;
                RefreshReward();
            });
            nameInput.onValueChanged.AddListener(_ => RefreshReward());
            RefreshReward();
        }

        private void RefreshReward()
        {
            // 数据变化时才格式化；翻译中的 {0}、{1} 分别对应名字和金币数。
            rewardText.text = QGF.Localization.GetString("Reward.Coins", nameInput.text, coins);
        }
    }
}
