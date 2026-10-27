//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    /// <summary>
    /// 保存一次播放声音请求的参数。
    /// </summary>
    internal sealed class PlaySoundInfo : IReference
    {
        /// <summary>
        /// 初始化声音播放请求。
        /// </summary>
        public PlaySoundInfo()
        {
            BindingEntity = null;
            WorldPosition = Vector3.zero;
            UserData = null;
        }

        /// <summary>
        /// 获取声音绑定的实体。
        /// </summary>
        public Entity BindingEntity { get; private set; }

        /// <summary>
        /// 获取声音播放的世界坐标。
        /// </summary>
        public Vector3 WorldPosition { get; private set; }

        /// <summary>
        /// 获取用户自定义数据。
        /// </summary>
        public object UserData { get; private set; }

        /// <summary>
        /// 从引用池获取请求数据。
        /// </summary>
        /// <param name="bindingEntity">声音绑定的实体。</param>
        /// <param name="worldPosition">没有绑定实体时的播放世界坐标。</param>
        /// <param name="userData">用户自定义数据。</param>
        /// <returns>用于传递本次请求参数的数据对象。</returns>
        public static PlaySoundInfo Create(Entity bindingEntity, Vector3 worldPosition, object userData)
        {
            PlaySoundInfo playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
            playSoundInfo.BindingEntity = bindingEntity;
            playSoundInfo.WorldPosition = worldPosition;
            playSoundInfo.UserData = userData;
            return playSoundInfo;
        }

        /// <summary>
        /// 清理请求参数及用户数据。
        /// </summary>
        public void Clear()
        {
            BindingEntity = null;
            WorldPosition = Vector3.zero;
            UserData = null;
        }
    }
}
