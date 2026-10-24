using UnityEngine;
using UnityEngine.UI;
using UnityQGameFramework.Core.Event;
using UnityQGameFramework.Runtime;

namespace UnityQGameFramework.Examples.Modules
{
    public sealed class SubscribeEvent : MonoBehaviour
    {
        [SerializeField] private Button sendButton;
        [SerializeField] private Text coinsText;
        private int coins;

        private void Start()
        {
            // Subscribe 建立监听；Fire 将消息加入队列，框架更新时派发。
            QGF.Event.Subscribe(CoinsAdded.EventId, OnCoinsAdded);
            sendButton.onClick.AddListener(() => QGF.Event.Fire(this, CoinsAdded.Create(10)));
            coinsText.text = "金币：0";
        }

        private void OnCoinsAdded(object sender, GameEventArgs args)
        {
            coins += ((CoinsAdded)args).Amount;
            coinsText.text = $"金币：{coins}";
        }

        private void OnDestroy()
        {
            // 解除本对象的监听；事件参数由框架自动归还，不在这里 Release。
            var events = GameEntry.GetComponent<EventComponent>();
            if (events != null) events.Unsubscribe(CoinsAdded.EventId, OnCoinsAdded);
        }
    }
}
