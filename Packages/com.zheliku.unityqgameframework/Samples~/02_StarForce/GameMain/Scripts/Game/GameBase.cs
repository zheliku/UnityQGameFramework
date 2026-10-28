//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 一局游戏的初始化、更新和实体事件处理。
    /// </summary>
    public abstract class GameBase
    {
        /// <summary>
        /// 获取当前游戏模式。
        /// </summary>
        public abstract GameMode GameMode
        {
            get;
        }

        /// <summary>
        /// 获取本局使用的背景与边界。
        /// </summary>
        protected ScrollableBackground SceneBackground
        {
            get; private set;
        }

        /// <summary>
        /// 获取或设置本局是否结束。
        /// </summary>
        public bool GameOver
        {
            get; protected set;
        }

        private MyAircraft m_MyAircraft = null;

        /// <summary>
        /// 初始化本局游戏。
        /// </summary>
        public virtual void Initialize()
        {
            // ShowMyAircraft 会异步加载预制体，拿到成功事件后才持有飞机逻辑实例。
            GameEntry.Event.Subscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            GameEntry.Event.Subscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);

            SceneBackground = Object.FindFirstObjectByType<ScrollableBackground>();
            if (SceneBackground == null)
            {
                Log.Warning("Can not find scene background.");
                return;
            }

            SceneBackground.VisibleBoundary.gameObject.GetOrAddComponent<HideByBoundary>();
            GameEntry.Entity.ShowMyAircraft(new MyAircraftData(GameEntry.Entity.GenerateSerialId(), 10000)
            {
                Name = "My Aircraft",
                Position = Vector3.zero,
            });

            GameOver = false;
            m_MyAircraft = null;
        }

        /// <summary>
        /// 结束本局并解除事件订阅。
        /// </summary>
        public virtual void Shutdown()
        {
            // 游戏实例会被下一局复用，离开本局时解除订阅，避免重复收到实体事件。
            GameEntry.Event.Unsubscribe(ShowEntitySuccessEventArgs.EventId, OnShowEntitySuccess);
            GameEntry.Event.Unsubscribe(ShowEntityFailureEventArgs.EventId, OnShowEntityFailure);
        }

        /// <summary>
        /// 更新当前玩法。
        /// </summary>
        /// <param name="elapseSeconds">逻辑时间间隔，以秒为单位。</param>
        /// <param name="realElapseSeconds">真实时间间隔，以秒为单位。</param>
        public virtual void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (m_MyAircraft != null && m_MyAircraft.IsDead)
            {
                GameOver = true;
                return;
            }
        }

        /// <summary>
        /// 在实体显示成功后记录玩家飞机。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="e">实体事件数据。</param>
        protected virtual void OnShowEntitySuccess(object sender, GameEventArgs e)
        {
            ShowEntitySuccessEventArgs ne = (ShowEntitySuccessEventArgs)e;
            if (ne.EntityLogicType == typeof(MyAircraft))
            {
                m_MyAircraft = (MyAircraft)ne.Entity.Logic;
            }
        }

        /// <summary>
        /// 报告实体显示失败。
        /// </summary>
        /// <param name="sender">事件发送方。</param>
        /// <param name="e">实体事件数据。</param>
        protected virtual void OnShowEntityFailure(object sender, GameEventArgs e)
        {
            ShowEntityFailureEventArgs ne = (ShowEntityFailureEventArgs)e;
            Log.Warning("Show entity failure with error message '{0}'.", ne.ErrorMessage);
        }
    }
}
