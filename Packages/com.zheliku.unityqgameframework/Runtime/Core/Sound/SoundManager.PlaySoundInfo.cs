//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

namespace UnityQGameFramework.Core.Sound
{
    internal sealed partial class SoundManager : FrameworkModule, ISoundManager
    {
        /// <summary>
        /// 保存一次播放声音请求的参数。
        /// </summary>
        private sealed class PlaySoundInfo : IReference
        {
            /// <summary>
            /// 初始化声音播放请求。
            /// </summary>
            public PlaySoundInfo()
            {
                SerialId = 0;
                SoundGroup = null;
                PlaySoundParams = null;
                UserData = null;
            }

            /// <summary>
            /// 获取声音序列编号。
            /// </summary>
            public int SerialId { get; private set; }

            /// <summary>
            /// 获取目标声音组。
            /// </summary>
            public SoundGroup SoundGroup { get; private set; }

            /// <summary>
            /// 获取声音播放参数。
            /// </summary>
            public PlaySoundParams PlaySoundParams { get; private set; }

            /// <summary>
            /// 获取用户自定义数据。
            /// </summary>
            public object UserData { get; private set; }

            /// <summary>
            /// 从引用池获取声音播放请求。
            /// </summary>
            /// <param name="serialId">声音序列编号。</param>
            /// <param name="soundGroup">目标声音组。</param>
            /// <param name="playSoundParams">声音播放参数。</param>
            /// <param name="userData">用户自定义数据。</param>
            /// <returns>声音播放请求。</returns>
            public static PlaySoundInfo Create(int serialId, SoundGroup soundGroup, PlaySoundParams playSoundParams, object userData)
            {
                PlaySoundInfo playSoundInfo = ReferencePool.Acquire<PlaySoundInfo>();
                playSoundInfo.SerialId = serialId;
                playSoundInfo.SoundGroup = soundGroup;
                playSoundInfo.PlaySoundParams = playSoundParams;
                playSoundInfo.UserData = userData;
                return playSoundInfo;
            }

            /// <summary>
            /// 清理声音播放请求数据。
            /// </summary>
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
