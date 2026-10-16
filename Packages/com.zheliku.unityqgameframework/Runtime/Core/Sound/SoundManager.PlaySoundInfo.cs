//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Sound
{
    internal sealed partial class SoundManager : FrameworkModule, ISoundManager
    {
        private sealed class PlaySoundInfo : IReference
        {
            public PlaySoundInfo()
            {
                SerialId = 0;
                SoundGroup = null;
                PlaySoundParams = null;
                UserData = null;
            }

            public int SerialId { get; private set; }

            public SoundGroup SoundGroup { get; private set; }

            public PlaySoundParams PlaySoundParams { get; private set; }

            public object UserData { get; private set; }

            public static PlaySoundInfo Create(int serialId, SoundGroup soundGroup, PlaySoundParams playSoundParams, object userData)
            {
                PlaySoundInfo playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
                playSoundInfo.SerialId = serialId;
                playSoundInfo.SoundGroup = soundGroup;
                playSoundInfo.PlaySoundParams = playSoundParams;
                playSoundInfo.UserData = userData;
                return playSoundInfo;
            }

            public void Clear()
            {
                SerialId = 0;
                SoundGroup = null;
                PlaySoundParams = null;
                UserData = null;
            }
        }
    }
}
