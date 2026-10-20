using System;
using System.Linq;
using UnityEngine;
namespace Castle.V24
{
    [Serializable] public sealed class V24AudioBinding
    { public string Key; public AudioClip Clip; public bool Voice; }

    public sealed partial class CastleV24Controller
    {
        public V24AudioBinding[] PlotAudio = Array.Empty<V24AudioBinding>();
        public AudioClip DefaultBgm, BanquetBgm;
        private int _audioRecord, _audioLine;
        private float _heardPosition;

        private void SetPlaybackPlaying(bool playing)
        {
            _playing = playing;
            if (playing) { if(Voice)Voice.UnPause();if(Effects)Effects.UnPause(); }
            else { if(Voice)Voice.Pause();if(Effects)Effects.Pause(); }
        }

        private void PlayPlotAudio(int id)
        {
            if (!_data.Formal) return;
            var plot = _data.Plot.First(x => x.Id == id);
            var binding = PlotAudio.FirstOrDefault(x => x.Key == plot.Sfx);
            if (binding?.Clip == null) return;
            if (binding.Voice) PlayVoice(binding.Clip);
            else { Effects.volume = S.EffectsVolume; Effects.PlayOneShot(binding.Clip, id == 520036 ? .5f : 1f); }
        }
        private void UpdateFormalMusic()
        {
            if (!_data.Formal) return;
            bool dinner = _page != "Title" && S.Room == 100002 && S.Day == 2 && Engine.Now.Hour >= 18 && Engine.Now.Hour < 20;
            var clip = dinner ? BanquetBgm : DefaultBgm;
            if (clip && Music.clip != clip) { Music.clip = clip; Music.loop = true; Music.Play(); }
        }
        private void UpdatePlaybackAudio()
        {
            if (!_data.Formal || !_playing) return;
            var record = _data.RecordById(_record);
            if (record == null) return;
            if (_audioRecord != _record) { _audioRecord = _record; _audioLine = 0; }
            var current = _data.Lines(record.Plot).LastOrDefault(x => x.At <= _position);
            if (current != null && current.Id != _audioLine) { _audioLine = current.Id; PlayPlotAudio(current.Id); }
        }
        private string SpeakerName(V24Plot plot, bool recording)
        {
            if (plot.SpeakerId == 0) return "";
            var meta = _data.PlotMeta.FirstOrDefault(x => x.Id == plot.Id);
            if (!string.IsNullOrEmpty(meta?.SpeakerDisplayOverride)) return meta.SpeakerDisplayOverride;
            if (recording && !string.IsNullOrEmpty(meta?.Unknown)) return meta.Unknown;
            return Engine.CharacterName(plot.SpeakerId);
        }
        private void OpenGate()
        {
            var gate = _data.PlaybackGates.First(x => x.Event == S.PlaybackGate);
            if (_record != gate.Record) { _position = S.RecordId==gate.Record?S.RecordPosition:0; _heardPosition = S.RecordId==gate.Record?S.PlaybackHeard:0; }
            _record = gate.Record; _playTape = 0; _playing = true; Page("Tape");
        }
    }
}
