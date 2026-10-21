using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Castle.V24
{
    [Serializable] public sealed class V24RoomArt
    {
        public int Room;
        public Texture2D Day, Night;
        // 仅 2.5D 房间背景允许缩放；走廊和中控室等固定视角保持关闭。
        public bool AllowZoom;
    }
    [Serializable] public sealed class V24CharacterArt
    {
        public int Character;
        public Sprite Portrait;
        public float Height = 1000;
        // 归一化纹理坐标（左下原点），只截取肩部以上；可在 Inspector 调整。
        public Rect HeadRect = new Rect(.25f, .75f, .5f, .25f);
    }
    [Serializable] public sealed class V24ConversationChoice
    {
        public int Event, Character;
    }
    public sealed partial class CastleV24Controller
    {
        public V24RoomArt[] RoomArt = Array.Empty<V24RoomArt>();
        public V24CharacterArt[] CharacterArt = Array.Empty<V24CharacterArt>();
        public Texture2D CorridorDay, CorridorNight;
        public V24ConversationChoice[] ConversationChoices = Array.Empty<V24ConversationChoice>();
        // 下一版配置表提供唯一顺序。空数组不推测顺序，仍按现有事件条件判定。
        public int[] ConversationOrder = Array.Empty<int>();
        private bool _avatarsExpanded;
        private int _presentationRoom;
        private const float SceneZoomDefault = 1.4f;
        private const float SceneZoomMin = 1f;
        private const float SceneZoomMax = 1.5f;
        private bool _sceneZoomEnabled;
        private float _sceneZoom = 1;

        private V24ConversationChoice[] CurrentChoices() => !_data.Formal ? ConversationChoices : (Engine.CurrentConversationGroup()?.Events ?? Array.Empty<int>())
            .Select(id => new V24ConversationChoice { Event = id, Character = _data.Lines(_data.EventById(id).Plot).FirstOrDefault(x => x.SpeakerId != 0 && x.SpeakerId != 200001)?.SpeakerId ?? 0 }).ToArray();
        private bool IsConversationChoice(V24Event ev) => CurrentChoices().Any(x => x.Event == ev.Id);
        private bool ConversationAvailable() => _data.Formal ? Engine.CurrentConversationGroup() != null : S.Room == 100002 && S.Goals.Contains(1900085)
            && ConversationChoices.Any(x => !Engine.Done(_data.EventById(x.Event)));

        private void SceneBackground(bool reception)
        {
            bool night = Engine.Now.Hour >= 18 || Engine.Now.Hour < 6;
            var art = RoomArt.FirstOrDefault(x => x.Room == S.Room);
            var texture = reception ? (night ? CorridorNight : CorridorDay) : art == null ? null : night ? art.Night : art.Day;
            V("Explore").transform.Find("Backdrop").GetComponent<RawImage>().texture = texture ? texture : _defaultRoomBackground;
            bool zoomEnabled = !reception && art != null && art.AllowZoom && texture != null;
            if (_presentationRoom != S.Room || _sceneZoomEnabled != zoomEnabled)
                _sceneZoom = zoomEnabled ? SceneZoomDefault : 1f;
            if (_presentationRoom != S.Room) { _presentationRoom = S.Room; _avatarsExpanded = false; }
            _sceneZoomEnabled = zoomEnabled;
            ApplySceneZoom();
        }

        private void ApplySceneZoom()
        {
            _sceneZoom = _sceneZoomEnabled ? Mathf.Clamp(_sceneZoom, SceneZoomMin, SceneZoomMax) : 1f;
            V("Explore").transform.Find("Backdrop").localScale = new Vector3(_sceneZoom, _sceneZoom, 1);
            Show(V("Explore"), "ZoomHint", false);
        }

        private bool SceneZoomInput()
        {
            if (_page != "Explore" || !_sceneZoomEnabled) return false;
            bool reset = Input.GetKeyDown(KeyCode.Space) && !Mathf.Approximately(_sceneZoom, SceneZoomDefault);
            if (reset) { _sceneZoom = SceneZoomDefault; ApplySceneZoom(); return true; }
            if (Input.mouseScrollDelta.y == 0) return false;
            // 列表和对白消费自己的滚轮，不同时缩放场景。
            var hits = new System.Collections.Generic.List<RaycastResult>();
            if (EventSystem.current)
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Input.mousePosition }, hits);
            if (hits.Any(x => x.gameObject.GetComponentInParent<ScrollRect>() != null)) return false;
            _sceneZoom = Mathf.Clamp(_sceneZoom + Input.mouseScrollDelta.y * .05f, SceneZoomMin, SceneZoomMax);
            ApplySceneZoom();
            return false;
        }

        private void DrawCharacter(string binding, int id, bool visible)
        {
            var image = V("Explore").Get<Image>(binding);
            var art = CharacterArt.FirstOrDefault(x => x.Character == id);
            image.sprite = art?.Portrait;
            image.preserveAspect = true;
            image.gameObject.SetActive(visible && image.sprite != null);
            if (art?.Portrait != null)
            {
                float aspect = art.Portrait.rect.width / art.Portrait.rect.height;
                float height = aspect > 1 ? 120 : art.Height; // 姓名占位图保持小尺寸，正式立绘按身高。
                image.rectTransform.sizeDelta = new Vector2(height * aspect, height);
            }
            if (binding == "CharacterImage")
            {
                image.rectTransform.anchoredPosition = new Vector2(-468.4181f, -331.5084f);
                image.rectTransform.sizeDelta = new Vector2(612.4181f, 1122.509f);
            }
        }

        private void SceneCharacters(bool talk, int[] participants)
        {
            var v = V("Explore");
            bool group = ConversationAvailable() || talk && CurrentChoices().Any(x => x.Event == S.ActiveEvent)
                || !_data.Formal && S.Room == 100002 && S.Transcript.Count > 0 && ConversationChoices.Any(x => x.Event == _portraitEvent);
            var people = v.Get<RectTransform>("People");
            int[] visibleParticipants = participants;
            if (!talk && S.Transcript.Count > 0)
                visibleParticipants = S.Transcript.Where(x => x.Plot != 0).Select(x => _data.Plot.First(p => p.Id == x.Plot).SpeakerId).Where(x => x != 0 && x != 200001).Distinct().ToArray();
            bool hasDialogue = talk || S.Transcript.Count > 0;
            bool three = hasDialogue && visibleParticipants.Length == 2 && !group;
            bool two = hasDialogue && visibleParticipants.Length == 1 && !group;
            if (talk && _portraitEvent != S.ActiveEvent)
            {
                _portraitEvent = S.ActiveEvent;
                _selectedCharacter = visibleParticipants.FirstOrDefault();
            }
            if (!visibleParticipants.Contains(_selectedCharacter) && !(two && _selectedCharacter == 200001))
                _selectedCharacter = visibleParticipants.FirstOrDefault();
            Show(v, "AvatarStack", group && !_avatarsExpanded);
            Btn(v, "AvatarStack", () => { _avatarsExpanded = !_avatarsExpanded; Explore(); }, "", !Engine.Busy);
            DrawHead(v.Get<RawImage>("StackHead"), _selectedCharacter);
            var grid = people.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112, 143);
            if (group && _avatarsExpanded)
            {
                foreach (var choice in CurrentChoices())
                {
                    var local = choice;
                    var card = Card(people, "", "", () => ChooseConversation(local), template: ConversationPortraitPrefab);
                    DrawHead(card.Get<RawImage>("Head"), choice.Character);
                }
            }
            else if (two || three)
            {
                foreach (int character in two ? new[] {200001, visibleParticipants[0]} : visibleParticipants)
                {
                    int id = character;
                    var card = Card(people, "", "", () => { _selectedCharacter = id; Portrait(id); }, template: ConversationPortraitPrefab);
                    DrawHead(card.Get<RawImage>("Head"), id);
                }
            }
            DrawCharacter("HeroineImage", 200001, false);
            DrawCharacter("CharacterImage", _selectedCharacter, hasDialogue || !group);
        }

        private int _portraitEvent;
        private void DrawHead(RawImage image, int character)
        {
            var art = CharacterArt.FirstOrDefault(x => x.Character == character);
            bool valid = art?.Portrait != null && art.Portrait.rect.height > art.Portrait.rect.width;
            image.gameObject.SetActive(valid);
            if (!valid) return;
            image.texture = art.Portrait.texture;
            // 保持肩部头像的比例，居中裁掉多余宽度，避免被头像框拉伸。
            var rect = art.HeadRect;
            float desired = image.rectTransform.rect.width / Mathf.Max(1, image.rectTransform.rect.height);
            float actual = rect.width * image.texture.width / (rect.height * image.texture.height);
            if (actual > desired) { float width = rect.width * desired / actual; rect.x += (rect.width - width) / 2; rect.width = width; }
            image.uvRect = rect;
        }

        private void ChooseConversation(V24ConversationChoice choice)
        {
            if (_data.Formal)
            {
                if (!Engine.SelectFormalEvent(choice.Event)) { Toast("ta好像正在忙"); return; }
                _avatarsExpanded = false; _selectedCharacter = choice.Character; Refresh(); return;
            }
            int next = ConversationOrder.FirstOrDefault(id => !Engine.Done(_data.EventById(id)));
            if (Engine.Busy || next != 0 && next != choice.Event || !Engine.Eligible(_data.EventById(choice.Event)))
            { Toast("ta好像正在忙"); return; }
            if (Engine.Start(choice.Event)) { _avatarsExpanded = false; _selectedCharacter = choice.Character; Refresh(); }
        }
    }
}
