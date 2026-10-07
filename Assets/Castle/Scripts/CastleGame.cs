using System;
using Castle.V2;
using UnityEngine;
namespace Castle
{
    public sealed class CastleGame : MonoBehaviour
    {
        public GameSettings Settings;
        public ContentDatabase Content;
        public SessionService Session { get; private set; }
        PrototypeUi _ui;
        bool _smoke;
        float _saveTimer;
        void Awake()
        {
            _smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "-castleV2Smoke") >= 0;
            Settings = Settings ? Settings : Resources.Load<GameSettings>("Castle/GameSettings");
            Content = Content ? Content : Resources.Load<ContentDatabase>("Castle/ContentV2");
            if (!Settings || !Content) { Debug.LogError("请先执行 Tools/Castle/Content/Create Missing Example Config 及 Import All Six Tables。"); enabled = false; return; }
            Session = new SessionService(Content, Settings, new SaveService(_smoke ? null : SaveService.DefaultPath));
            _ui = new PrototypeUi(this, Session); _ui.ShowTitle();
            if (_smoke) StartCoroutine(PrototypeSmoke.Run(this, _ui));
        }
        void Update()
        {
            if (_ui == null) return;
            _ui.Tick(Time.unscaledDeltaTime);
            _saveTimer += Time.unscaledDeltaTime; if (_saveTimer > 2) { _saveTimer = 0; Session.Persist(); }
        }
        void OnApplicationPause(bool pause) { if (pause && Session != null) Session.Persist(); }
        void OnApplicationQuit() { Session?.Persist(); }
    }
}
