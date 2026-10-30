using System;
using UnityEngine;
using UnityQGameFramework.Core;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 仅在物体启用期间接收语言切换通知。
    /// </summary>
    public abstract class LocalizeComponent : MonoBehaviour
    {
        [SerializeField, Tooltip("Translation key for Text, or localized asset key for Image.")]
        private string m_Key;
        private EventComponent m_Events;

        /// <summary>
        /// 获取或设置本地化业务键，修改后立即刷新。
        /// </summary>
        public string Key
        {
            get
            {
                return m_Key;
            }
            set
            {
                m_Key = value;
                Refresh();
            }
        }

        /// <summary>
        /// 获取当前绑定的本地化组件。
        /// </summary>
        protected LocalizationComponent Localization
        {
            get; private set;
        }

        /// <summary>
        /// 刷新文本、字体或图片。
        /// </summary>
        public abstract void Refresh();

        /// <summary>
        /// 初次启动时等待框架组件完成 Awake 后建立绑定。
        /// </summary>
        protected virtual void Start()
        {
            Bind();
        }

        /// <summary>
        /// 物体从池中重新启用时读取当前语言。
        /// </summary>
        protected virtual void OnEnable()
        {
            Bind();
        }

        /// <summary>
        /// 物体隐藏时结束订阅并还原原始资源引用。
        /// </summary>
        protected virtual void OnDisable()
        {
            if (m_Events != null && m_Events.Check(LanguageChangedEventArgs.EventId, OnLanguageChanged))
            {
                m_Events.Unsubscribe(LanguageChangedEventArgs.EventId, OnLanguageChanged);
            }
            m_Events = null;
            Localization = null;
            Restore();
        }

        /// <summary>
        /// 还原预制体上的字体或图片，避免池内实例继续引用旧语言资源。
        /// </summary>
        protected virtual void Restore()
        {
        }

        /// <summary>
        /// 连接当前框架实例，避免重复订阅。
        /// </summary>
        private void Bind()
        {
            if (m_Events == null || Localization == null)
            {
                m_Events = GameEntry.GetComponent<EventComponent>();
                Localization = GameEntry.GetComponent<LocalizationComponent>();
                if (m_Events != null && Localization != null)
                {
                    m_Events.Subscribe(LanguageChangedEventArgs.EventId, OnLanguageChanged);
                }
                else
                {
                    m_Events = null;
                    Localization = null;
                }
            }
            if (Localization != null && Localization.DictionaryCount > 0)
            {
                Refresh();
            }
        }

        /// <summary>
        /// 刷新属于当前本地化组件的界面。
        /// </summary>
        /// <param name="sender">本地化组件。</param>
        /// <param name="args">语言切换通知。</param>
        private void OnLanguageChanged(object sender, GameEventArgs args)
        {
            if (ReferenceEquals(sender, Localization))
            {
                try
                {
                    Refresh();
                }
                catch (Exception exception)
                {
                    // 单个绑定错误不阻断其他界面刷新，并先解除对旧语言资源的引用。
                    Restore();
                    Log.Error("Failed to refresh localization key '{0}': {1}", Key, exception);
                }
            }
        }
    }
}
