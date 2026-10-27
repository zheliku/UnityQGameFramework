using UnityEngine;
using UnityEngine.UI;

namespace StarForce
{
    /// <summary>
    /// 显示资源更新进度和说明。
    /// </summary>
    public class UpdateResourceForm : MonoBehaviour
    {
        [SerializeField]
        private Text m_DescriptionText = null;

        [SerializeField]
        private Slider m_ProgressSlider = null;

        /// <summary>
        /// 更新资源下载进度和说明。
        /// </summary>
        /// <param name="progress">更新进度，范围为 0 到 1。</param>
        /// <param name="description">当前进度说明。</param>
        public void SetProgress(float progress, string description)
        {
            m_ProgressSlider.value = progress;
            m_DescriptionText.text = description;
        }
    }
}
