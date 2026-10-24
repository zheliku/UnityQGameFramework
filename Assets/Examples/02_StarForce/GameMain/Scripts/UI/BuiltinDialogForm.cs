using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    // 资源更新前使用。预制体由 Launcher 直接引用，不经过配置表和资源加载。
    public sealed class BuiltinDialogForm : MonoBehaviour
    {
        [SerializeField] private Text m_TitleText, m_MessageText;
        [SerializeField] private GameObject[] m_ModeObjects;
        [SerializeField] private Text[] m_ConfirmTexts, m_CancelTexts, m_OtherTexts;
        private DialogParams parameters;
        private bool answered, pausedByDialog;

        public void Show(DialogParams value)
        {
            parameters = value;
            m_TitleText.text = value.Title;
            m_MessageText.text = value.Message;
            for (int i = 0; i < m_ModeObjects.Length; i++)
                m_ModeObjects[i].SetActive(i + 1 == value.Mode);
            SetText(m_ConfirmTexts, value.ConfirmText, "Dialog.ConfirmButton");
            SetText(m_CancelTexts, value.CancelText, "Dialog.CancelButton");
            SetText(m_OtherTexts, value.OtherText, "Dialog.OtherButton");
            // 只恢复自己发起的暂停，避免关闭窗口时解除外部暂停。
            pausedByDialog = value.PauseGame && !GameEntry.Base.IsGamePaused;
            if (pausedByDialog) GameEntry.Base.PauseGame();
        }

        public void OnConfirmButtonClick() => Answer(parameters.OnClickConfirm);
        public void OnCancelButtonClick() => Answer(parameters.OnClickCancel);
        public void OnOtherButtonClick() => Answer(parameters.OnClickOther);

        private void Answer(FrameworkAction<object> callback)
        {
            if (answered) return;
            answered = true;
            object userData = parameters.UserData;
            // 先移除输入并恢复本窗口的暂停，再执行业务回调。
            gameObject.SetActive(false);
            ResumeGame();
            Destroy(gameObject);
            callback?.Invoke(userData);
        }

        private static void SetText(Text[] texts, string label, string key)
        {
            if (string.IsNullOrEmpty(label)) label = GameEntry.Localization.GetString(key);
            foreach (var text in texts) text.text = label;
        }

        private void OnDestroy() => ResumeGame();

        private void ResumeGame()
        {
            if (!pausedByDialog) return;
            pausedByDialog = false;
            var component = UnityQGameFramework.Runtime.GameEntry.GetComponent<BaseComponent>();
            if (component != null) component.ResumeGame();
        }
    }
}
