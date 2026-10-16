//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityEngine;

namespace UnityQGameFramework.Runtime
{
    internal sealed class PlaySoundInfo : IReference
    {
        public PlaySoundInfo()
        {
            BindingEntity = null;
            WorldPosition = Vector3.zero;
            UserData = null;
        }

        public Entity BindingEntity { get; private set; }

        public Vector3 WorldPosition { get; private set; }

        public object UserData { get; private set; }

        public static PlaySoundInfo Create(Entity bindingEntity, Vector3 worldPosition, object userData)
        {
            PlaySoundInfo playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
            playSoundInfo.BindingEntity = bindingEntity;
            playSoundInfo.WorldPosition = worldPosition;
            playSoundInfo.UserData = userData;
            return playSoundInfo;
        }

        public void Clear()
        {
            BindingEntity = null;
            WorldPosition = Vector3.zero;
            UserData = null;
        }
    }
}
