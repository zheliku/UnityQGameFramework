using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示解析与查询配置。
    /// </summary>
    public sealed class ReadConfigValues : MonoBehaviour
    {
        [SerializeField] private TextAsset config;
        [SerializeField] private Button parseButton;
        [SerializeField] private Text valuesText;

        private void Start()
        {
            parseButton.onClick.AddListener(ReadConfig);
            ReadConfig();
        }

        private void ReadConfig()
        {
            // 本场景单独使用 Config。清空后重新解析 Inspector 中的 TOML。
            QGF.Config.RemoveAllConfigs();
            QGF.Config.ParseData(config.text);
            valuesText.text = $"游戏：{QGF.Config.GetString("Game.Title")}\n"
                + $"生命：{QGF.Config.GetInt("Player.MaxHp")}\n"
                + $"速度：{QGF.Config.GetFloat("Player.Speed")}";
        }
    }
}
