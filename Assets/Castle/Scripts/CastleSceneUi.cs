using System;
using UnityEngine;
using UnityEngine.UI;

namespace Castle
{
    public sealed class CastleSceneUi : MonoBehaviour
    {
        public Canvas Canvas;
        public RectTransform Stage;
        public AudioSource Audio;
        public CastleUiView[] Pages;
        public CastleUiView[] Overlays;
        public CastleUiView SceneHud;
        public GameObject OverlayRoot;
        public GameObject ToastRoot;
        public Text ToastText;
        public CastleJournalCardView JournalCardPrefab;
        public RectTransform[] WaveBars;
        public Color SelectedColor = new Color32(210, 181, 127, 255);
        public Color SelectedTextColor = new Color32(22, 34, 27, 255);

        public CastleUiView Page(ScreenId id) => Find(Pages, id.ToString());
        public CastleUiView Overlay(string id) => Find(Overlays, id);
        static CastleUiView Find(CastleUiView[] views, string id)
        {
            foreach (var view in views) if (view && view.Id == id) return view;
            throw new InvalidOperationException("场景缺少 UI 面板：" + id);
        }
        public void ValidateBindings()
        {
            if (!Canvas || !Stage || !Audio || !SceneHud || !OverlayRoot || !ToastRoot || !ToastText || !JournalCardPrefab)
                throw new InvalidOperationException("CastleSceneUi：公共引用不完整，请检查 Inspector。");
            foreach (ScreenId id in Enum.GetValues(typeof(ScreenId))) Page(id).ValidateBindings();
            foreach (string id in new[] { "Navigation", "Settings", "Journal", "JournalDetail", "InvitationArchive", "Message" }) Overlay(id).ValidateBindings();
            SceneHud.ValidateBindings();
            if (WaveBars == null || WaveBars.Length != 78) throw new InvalidOperationException("真相重建需要绑定78条波形。");
            foreach (var bar in WaveBars) if (!bar) throw new InvalidOperationException("波形引用缺失。");
        }
    }
}
