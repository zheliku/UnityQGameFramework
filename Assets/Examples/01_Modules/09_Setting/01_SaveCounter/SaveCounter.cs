using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示保存计数。
    /// </summary>
    public sealed class SaveCounter : MonoBehaviour
    {
        [SerializeField] private Button saveButton, resetButton;
        [SerializeField] private Text countText;
        private int count;

        private void Start()
        {
            // 第二个参数是缺少该键时的默认值。
            count = QGF.Setting.GetInt("Examples.Counter", 0);
            countText.text = $"Saved: {count}";
            saveButton.onClick.AddListener(() =>
            {
                QGF.Setting.SetInt("Examples.Counter", ++count); // 先修改内存。
                // 保存结果用于反馈磁盘写入是否成功，失败时内存中的修改仍可重试。
                countText.text = QGF.Setting.Save() ? $"Saved: {count}" : "Save failed. Try again.";
            });
            resetButton.onClick.AddListener(() =>
            {
                QGF.Setting.RemoveSetting("Examples.Counter");
                count = 0;
                countText.text = QGF.Setting.Save() ? "Cleared: 0" : "Save failed. Try again.";
            });
        }
    }
}
