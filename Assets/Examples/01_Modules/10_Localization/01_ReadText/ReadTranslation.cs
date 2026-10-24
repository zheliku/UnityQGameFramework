using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
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
            QGF.Localization.Language = Language.ChineseSimplified;
            QGF.Localization.ParseData(dictionary.text);
            titleText.text = QGF.Localization.GetString("Game.Name");
            rewardButton.onClick.AddListener(() => coins += 10);
        }

        private void Update()
        {
            // 翻译中的 {0}、{1} 分别替换为名字和金币数，语序由翻译决定。
            rewardText.text = QGF.Localization.GetString("Reward.Coins", nameInput.text, coins);
        }
    }
}
