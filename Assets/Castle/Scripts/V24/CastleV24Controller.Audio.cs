using UnityEngine;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        public Texture2D TalkingCursor;
        public AudioSource Voice;

        private void ApplyAudioSettings()
        {
            Music.volume = S.Volume;
            Effects.volume = S.EffectsVolume;
            if (Voice) Voice.volume = S.EffectsVolume;
        }

        private void SaveAudioSettings()
        {
            // 标题页尚无游戏进度时也保留音量偏好，不创建虚假的游戏存档。
            if (!_test)
            {
                PlayerPrefs.SetFloat("Castle.MusicVolume", S.Volume);
                PlayerPrefs.SetFloat("Castle.EffectsVolume", S.EffectsVolume);
                PlayerPrefs.Save();
            }
            Save();
        }

        /// <summary>后续配置表的音效与 CV 统一由这两个入口播放；BGM 不受影响。</summary>
        public void PlaySoundEffect(AudioClip clip)
        {
            if (clip) { Effects.volume = S.EffectsVolume; Effects.PlayOneShot(clip); }
        }

        public void PlayVoice(AudioClip clip)
        {
            if (!Voice || !clip) return;
            Voice.volume = S.EffectsVolume;
            Voice.clip = clip;
            Voice.Play();
        }

        private void RequestQuit()
        {
            Confirm("保存进度并退出游戏？", () =>
            {
                SaveAudioSettings();
                if (_test) return;
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            });
        }
    }
}
