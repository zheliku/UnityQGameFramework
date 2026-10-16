//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using System;

namespace UnityQGameFramework.Core.Sound
{
    internal sealed partial class SoundManager : FrameworkModule, ISoundManager
    {
        /// <summary>
        /// 声音代理。
        /// </summary>
        private sealed class SoundAgent : ISoundAgent
        {
            private readonly SoundGroup m_SoundGroup;
            private readonly ISoundHelper m_SoundHelper;
            private object m_SoundAsset;
            private bool m_MuteInSoundGroup;
            private float m_VolumeInSoundGroup;

            /// <summary>
            /// 初始化声音代理的新实例。
            /// </summary>
            /// <param name="soundGroup">所在的声音组。</param>
            /// <param name="soundHelper">声音辅助器接口。</param>
            /// <param name="soundAgentHelper">声音代理辅助器接口。</param>
            public SoundAgent(SoundGroup soundGroup, ISoundHelper soundHelper, ISoundAgentHelper soundAgentHelper)
            {
                if (soundGroup == null)
                {
                    throw new FrameworkException("Sound group is invalid.");
                }

                if (soundHelper == null)
                {
                    throw new FrameworkException("Sound helper is invalid.");
                }

                if (soundAgentHelper == null)
                {
                    throw new FrameworkException("Sound agent helper is invalid.");
                }

                m_SoundGroup = soundGroup;
                m_SoundHelper = soundHelper;
                Helper = soundAgentHelper;
                Helper.ResetSoundAgent += OnResetSoundAgent;
                SerialId = 0;
                m_SoundAsset = null;
                Reset();
            }

            /// <summary>
            /// 获取所在的声音组。
            /// </summary>
            public ISoundGroup SoundGroup => m_SoundGroup;

            /// <summary>
            /// 获取或设置声音的序列编号。
            /// </summary>
            public int SerialId { get; set; }

            /// <summary>
            /// 获取当前是否正在播放。
            /// </summary>
            public bool IsPlaying => Helper.IsPlaying;

            /// <summary>
            /// 获取声音长度。
            /// </summary>
            public float Length => Helper.Length;

            /// <summary>
            /// 获取或设置播放位置。
            /// </summary>
            public float Time
            {
                get => Helper.Time;
                set => Helper.Time = value;
            }

            /// <summary>
            /// 获取是否静音。
            /// </summary>
            public bool Mute => Helper.Mute;

            /// <summary>
            /// 获取或设置在声音组内是否静音。
            /// </summary>
            public bool MuteInSoundGroup
            {
                get
                {
                    return m_MuteInSoundGroup;
                }
                set
                {
                    m_MuteInSoundGroup = value;
                    RefreshMute();
                }
            }

            /// <summary>
            /// 获取或设置是否循环播放。
            /// </summary>
            public bool Loop
            {
                get => Helper.Loop;
                set => Helper.Loop = value;
            }

            /// <summary>
            /// 获取或设置声音优先级。
            /// </summary>
            public int Priority
            {
                get => Helper.Priority;
                set => Helper.Priority = value;
            }

            /// <summary>
            /// 获取音量大小。
            /// </summary>
            public float Volume => Helper.Volume;

            /// <summary>
            /// 获取或设置在声音组内音量大小。
            /// </summary>
            public float VolumeInSoundGroup
            {
                get
                {
                    return m_VolumeInSoundGroup;
                }
                set
                {
                    m_VolumeInSoundGroup = value;
                    RefreshVolume();
                }
            }

            /// <summary>
            /// 获取或设置声音音调。
            /// </summary>
            public float Pitch
            {
                get => Helper.Pitch;
                set => Helper.Pitch = value;
            }

            /// <summary>
            /// 获取或设置声音立体声声相。
            /// </summary>
            public float PanStereo
            {
                get => Helper.PanStereo;
                set => Helper.PanStereo = value;
            }

            /// <summary>
            /// 获取或设置声音空间混合量。
            /// </summary>
            public float SpatialBlend
            {
                get => Helper.SpatialBlend;
                set => Helper.SpatialBlend = value;
            }

            /// <summary>
            /// 获取或设置声音最大距离。
            /// </summary>
            public float MaxDistance
            {
                get => Helper.MaxDistance;
                set => Helper.MaxDistance = value;
            }

            /// <summary>
            /// 获取或设置声音多普勒等级。
            /// </summary>
            public float DopplerLevel
            {
                get => Helper.DopplerLevel;
                set => Helper.DopplerLevel = value;
            }

            /// <summary>
            /// 获取声音代理辅助器。
            /// </summary>
            public ISoundAgentHelper Helper { get; }

            /// <summary>
            /// 获取声音创建时间。
            /// </summary>
            internal DateTime SetSoundAssetTime { get; private set; }

            /// <summary>
            /// 播放声音。
            /// </summary>
            public void Play()
            {
                Helper.Play(Constant.DefaultFadeInSeconds);
            }

            /// <summary>
            /// 播放声音。
            /// </summary>
            /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
            public void Play(float fadeInSeconds)
            {
                Helper.Play(fadeInSeconds);
            }

            /// <summary>
            /// 停止播放声音。
            /// </summary>
            public void Stop()
            {
                Helper.Stop(Constant.DefaultFadeOutSeconds);
            }

            /// <summary>
            /// 停止播放声音。
            /// </summary>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            public void Stop(float fadeOutSeconds)
            {
                Helper.Stop(fadeOutSeconds);
            }

            /// <summary>
            /// 暂停播放声音。
            /// </summary>
            public void Pause()
            {
                Helper.Pause(Constant.DefaultFadeOutSeconds);
            }

            /// <summary>
            /// 暂停播放声音。
            /// </summary>
            /// <param name="fadeOutSeconds">声音淡出时间，以秒为单位。</param>
            public void Pause(float fadeOutSeconds)
            {
                Helper.Pause(fadeOutSeconds);
            }

            /// <summary>
            /// 恢复播放声音。
            /// </summary>
            public void Resume()
            {
                Helper.Resume(Constant.DefaultFadeInSeconds);
            }

            /// <summary>
            /// 恢复播放声音。
            /// </summary>
            /// <param name="fadeInSeconds">声音淡入时间，以秒为单位。</param>
            public void Resume(float fadeInSeconds)
            {
                Helper.Resume(fadeInSeconds);
            }

            /// <summary>
            /// 重置声音代理。
            /// </summary>
            public void Reset()
            {
                if (m_SoundAsset != null)
                {
                    m_SoundHelper.ReleaseSoundAsset(m_SoundAsset);
                    m_SoundAsset = null;
                }

                SetSoundAssetTime = DateTime.MinValue;
                Time = Constant.DefaultTime;
                MuteInSoundGroup = Constant.DefaultMute;
                Loop = Constant.DefaultLoop;
                Priority = Constant.DefaultPriority;
                VolumeInSoundGroup = Constant.DefaultVolume;
                Pitch = Constant.DefaultPitch;
                PanStereo = Constant.DefaultPanStereo;
                SpatialBlend = Constant.DefaultSpatialBlend;
                MaxDistance = Constant.DefaultMaxDistance;
                DopplerLevel = Constant.DefaultDopplerLevel;
                Helper.Reset();
            }

            internal bool SetSoundAsset(object soundAsset)
            {
                Reset();
                m_SoundAsset = soundAsset;
                SetSoundAssetTime = DateTime.UtcNow;
                return Helper.SetSoundAsset(soundAsset);
            }

            internal void RefreshMute()
            {
                Helper.Mute = m_SoundGroup.Mute || m_MuteInSoundGroup;
            }

            internal void RefreshVolume()
            {
                Helper.Volume = m_SoundGroup.Volume * m_VolumeInSoundGroup;
            }

            private void OnResetSoundAgent(object sender, ResetSoundAgentEventArgs e)
            {
                Reset();
            }
        }
    }
}
