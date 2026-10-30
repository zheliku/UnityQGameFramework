using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    /// <summary>
    /// 演示订阅、发送与取消。
    /// </summary>
    public sealed class SubscribeEvent : MonoBehaviour
    {
        [SerializeField]
        private Button sendButton;
        [SerializeField]
        private TMP_Text coinsText;
        private int coins;
        private void Start()
        {
            // Subscribe 建立监听；Fire 将消息加入队列，框架更新时派发。
            QGF.Event.Subscribe(CoinsAdded.EventId, OnCoinsAdded);
            sendButton.onClick.AddListener(() => QGF.Event.Fire(this, CoinsAdded.Create(10)));
            coinsText.text = "Coins: 0";
        }

        private void OnCoinsAdded(object sender, GameEventArgs args)
        {
            coins += ((CoinsAdded)args).Amount;
            coinsText.text = $"Coins: {coins}";
        }

        private void OnDestroy()
        {
            // 解除本对象的监听；事件参数由框架自动归还，不在这里 Release。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null)
            {
                events.Unsubscribe(CoinsAdded.EventId, OnCoinsAdded);
            }
        }
    }
}
