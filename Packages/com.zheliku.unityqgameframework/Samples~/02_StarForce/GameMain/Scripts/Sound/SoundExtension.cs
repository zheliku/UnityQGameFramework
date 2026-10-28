//------------------------------------------------------------
// Unity Q Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
//------------------------------------------------------------

using UnityQGameFramework.Core;
using UnityQGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 按配置表播放声音并保存音量设置。
    /// </summary>
    public static class SoundExtension
    {
        private const float FadeVolumeDuration = 1f;
        private static int? s_MusicSerialId = null;

        /// <summary>
        /// 按配置播放音乐并替换上一首音乐。
        /// </summary>
        /// <param name="soundComponent">声音组件。</param>
        /// <param name="musicId">音乐配置编号。</param>
        /// <param name="userData">业务自定义数据。</param>
        /// <returns>播放序号，配置不存在时返回空。</returns>
        public static int? PlayMusic(this SoundComponent soundComponent, int musicId, object userData = null)
        {
            soundComponent.StopMusic();

            // Luban 行提供音乐资源名；找不到行时不发起加载。
            var dtMusic = GameEntry.DataTable.GetDataTable<TbMusic>();
            DRMusic drMusic = dtMusic.GetDataRow(musicId);
            if (drMusic == null)
            {
                Log.Warning("Can not load music '{0}' from data table.", musicId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = 64;
            playSoundParams.Loop = true;
            playSoundParams.VolumeInSoundGroup = 1f;
            playSoundParams.FadeInSeconds = FadeVolumeDuration;
            playSoundParams.SpatialBlend = 0f;
            // 记录序号，下一首音乐才能先淡出当前请求。
            s_MusicSerialId = soundComponent.PlaySound(
                AssetUtility.GetMusicAsset(drMusic.AssetName),
                "Music",
                Constant.AssetPriority.MusicAsset,
                playSoundParams,
                null,
                userData);
            return s_MusicSerialId;
        }

        /// <summary>
        /// 淡出并停止当前音乐。
        /// </summary>
        /// <param name="soundComponent">声音组件。</param>
        public static void StopMusic(this SoundComponent soundComponent)
        {
            if (!s_MusicSerialId.HasValue)
            {
                return;
            }

            soundComponent.StopSound(s_MusicSerialId.Value, FadeVolumeDuration);
            s_MusicSerialId = null;
        }

        /// <summary>
        /// 按配置播放音效。
        /// </summary>
        /// <param name="soundComponent">声音组件。</param>
        /// <param name="soundId">音效配置编号。</param>
        /// <param name="bindingEntity">跟随的实体，为空时不绑定。</param>
        /// <param name="userData">业务自定义数据。</param>
        /// <returns>播放序号，配置不存在时返回空。</returns>
        public static int? PlaySound(this SoundComponent soundComponent, int soundId, Entity bindingEntity = null, object userData = null)
        {
            // 数据行提供优先级、循环、音量和空间混合参数。
            var dtSound = GameEntry.DataTable.GetDataTable<TbSound>();
            DRSound drSound = dtSound.GetDataRow(soundId);
            if (drSound == null)
            {
                Log.Warning("Can not load sound '{0}' from data table.", soundId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = drSound.Priority;
            playSoundParams.Loop = drSound.Loop;
            playSoundParams.VolumeInSoundGroup = drSound.Volume;
            playSoundParams.SpatialBlend = drSound.SpatialBlend;
            // 声音跟随绑定实体；实体隐藏后，代理会重置并停止声音。
            return soundComponent.PlaySound(
                AssetUtility.GetSoundAsset(drSound.AssetName),
                "Sound",
                Constant.AssetPriority.SoundAsset,
                playSoundParams,
                bindingEntity != null ? bindingEntity.Entity : null,
                userData);
        }

        /// <summary>
        /// 按配置播放二维界面声音。
        /// </summary>
        /// <param name="soundComponent">声音组件。</param>
        /// <param name="uiSoundId">界面声音配置编号。</param>
        /// <param name="userData">业务自定义数据。</param>
        /// <returns>播放序号，配置不存在时返回空。</returns>
        public static int? PlayUISound(this SoundComponent soundComponent, int uiSoundId, object userData = null)
        {
            // UI 声音使用独立组，始终按 2D 声音播放。
            var dtUISound = GameEntry.DataTable.GetDataTable<TbUISound>();
            DRUISound drUISound = dtUISound.GetDataRow(uiSoundId);
            if (drUISound == null)
            {
                Log.Warning("Can not load UI sound '{0}' from data table.", uiSoundId.ToString());
                return null;
            }

            PlaySoundParams playSoundParams = PlaySoundParams.Create();
            playSoundParams.Priority = drUISound.Priority;
            playSoundParams.Loop = false;
            playSoundParams.VolumeInSoundGroup = drUISound.Volume;
            playSoundParams.SpatialBlend = 0f;
            return soundComponent.PlaySound(
                AssetUtility.GetUISoundAsset(drUISound.AssetName),
                "UISound",
                Constant.AssetPriority.UISoundAsset,
                playSoundParams,
                userData);
        }

        /// <summary>
        /// 查询声音组是否静音。
        /// </summary>
        /// <param name="soundComponent">声音组件。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>声音组静音或不可用时返回真。</returns>
        public static bool IsMuted(this SoundComponent soundComponent, string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return true;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return true;
            }

            return soundGroup.Mute;
        }

        /// <summary>
        /// 修改声音组静音状态并保存。
        /// </summary>
        /// <param name="soundComponent">声音组件。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="mute">是否静音。</param>
        public static void Mute(this SoundComponent soundComponent, string soundGroupName, bool mute)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return;
            }

            soundGroup.Mute = mute;

            // 先立即应用到声音组，再保存到玩家设置。
            GameEntry.Setting.SetBool(Utility.Text.Format(Constant.Setting.SoundGroupMuted, soundGroupName), mute);
            GameEntry.Setting.Save();
        }

        /// <summary>
        /// 获取声音组音量。
        /// </summary>
        /// <param name="soundComponent">声音组件。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <returns>声音组音量，范围为 0 到 1。</returns>
        public static float GetVolume(this SoundComponent soundComponent, string soundGroupName)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return 0f;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return 0f;
            }

            return soundGroup.Volume;
        }

        /// <summary>
        /// 修改声音组音量并保存。
        /// </summary>
        /// <param name="soundComponent">声音组件。</param>
        /// <param name="soundGroupName">声音组名称。</param>
        /// <param name="volume">音量，范围为 0 到 1。</param>
        public static void SetVolume(this SoundComponent soundComponent, string soundGroupName, float volume)
        {
            if (string.IsNullOrEmpty(soundGroupName))
            {
                Log.Warning("Sound group is invalid.");
                return;
            }

            ISoundGroup soundGroup = soundComponent.GetSoundGroup(soundGroupName);
            if (soundGroup == null)
            {
                Log.Warning("Sound group '{0}' is invalid.", soundGroupName);
                return;
            }

            soundGroup.Volume = volume;

            // 组音量立即生效，并保存到下次启动。
            GameEntry.Setting.SetFloat(Utility.Text.Format(Constant.Setting.SoundGroupVolume, soundGroupName), volume);
            GameEntry.Setting.Save();
        }
    }
}
