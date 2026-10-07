using System;
using UnityEngine;
namespace Castle.V2
{
    [Serializable] public sealed class SoundBinding { public string LineId; public AudioClip Clip; }
    [CreateAssetMenu(menuName = "Castle/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        public string InitialTime = "2026-10-07 18:45:00";
        public string[] InitialItems = Array.Empty<string>();
        public string ArrivalRoom = "R_HALL", HomeRoom = "R_HOME", ControlRoom = "R_CONTROL";
        public string OpeningEvent = "E_INVITE", ArrivalEvent = "E_ARRIVE", TapeTutorialEvent = "E_TUTORIAL", TutorialRecord = "REC_TAPE";
        public string MapEntry = "I_MAP", WakeTime = "08:00";
        public Texture2D TitleScene, ExteriorScene;
        public int ControlMinutes = 15;
        public bool KeepRecords = true;
        public float CalibrationStep = 1, CalibrationTolerance = 2, DialogueHold = 2, SecondsPerCharacter = .055f, EndingHold = 3;
        public DeviceConfig Devices;
        public CharacterConfig Characters;
        public MapConfig Map;
        public HotspotBinding Hotspots;
        public SoundBinding[] Sounds = Array.Empty<SoundBinding>();
    }
}
