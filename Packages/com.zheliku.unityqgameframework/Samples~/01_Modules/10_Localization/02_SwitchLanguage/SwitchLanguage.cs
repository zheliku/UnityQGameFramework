using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Localization;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class SwitchLanguage : MonoBehaviour
    {
        [SerializeField] private TextAsset dictionary;
        [SerializeField] private Button action1Button, action2Button;
        [SerializeField] private Text outputText;

        private void Start()
        {
            action1Button.onClick.AddListener(() => Apply(Language.English));
            action2Button.onClick.AddListener(() => Apply(Language.ChineseSimplified));
            Apply(Language.ChineseSimplified);
        }

        private void Apply(Language language)
        {
            // 设置 Language 不会自动换字典；清空旧键，再读取新语言。
            QGF.Localization.Language = language;
            QGF.Localization.RemoveAllRawStrings();
            QGF.Localization.ParseData(dictionary.text);
            outputText.text = QGF.Localization.GetString("Game.Name");
        }
    }
}
