using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class SaveCounter : MonoBehaviour
    {
        [SerializeField] private Button saveButton, resetButton;
        [SerializeField] private Text countText;
        private int count;

        private void Start()
        {
            // 第二个参数是缺少该键时的默认值。
            count = QGF.Setting.GetInt("Examples.Counter", 0);
            countText.text = $"已保存：{count}";
            saveButton.onClick.AddListener(() =>
            {
                QGF.Setting.SetInt("Examples.Counter", ++count); // 先修改内存。
                QGF.Setting.Save(); // 再写入持久化文件，下次运行仍可读取。
                countText.text = $"已保存：{count}";
            });
            resetButton.onClick.AddListener(() =>
            {
                QGF.Setting.RemoveSetting("Examples.Counter");
                QGF.Setting.Save();
                count = 0;
                countText.text = "已清除：0";
            });
        }
    }
}
