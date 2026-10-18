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
    }
    [Serializable] public sealed class V24CharacterArt
    {
        public int Character;
        public Sprite Portrait;
        public float Height = 700;
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
        private float _sceneZoom = 1;

        private bool IsConversationChoice(V24Event ev) => ConversationChoices.Any(x => x.Event == ev.Id);
        private bool ConversationAvailable() => S.Room == 100002 && S.Goals.Contains(1900085)
            && ConversationChoices.Any(x => !Engine.Done(_data.EventById(x.Event)));

        private void SceneBackground(bool reception)
        {
            bool night = Engine.Now.Hour >= 18;
            var art = RoomArt.FirstOrDefault(x => x.Room == S.Room);
            var texture = reception ? (night ? CorridorNight : CorridorDay) : art == null ? null : night ? art.Night : art.Day;
            V("Explore").transform.Find("Backdrop").GetComponent<RawImage>().texture = texture ? texture : _defaultRoomBackground;
            if (_presentationRoom != S.Room) { _presentationRoom = S.Room; _sceneZoom = 1; _avatarsExpanded = false; }
            ApplySceneZoom();
        }

        private void ApplySceneZoom()
        {
            V("Explore").transform.Find("Backdrop").localScale = new Vector3(_sceneZoom, _sceneZoom, 1);
            Show(V("Explore"), "ZoomHint", _sceneZoom >= 1.349f);
        }

        private bool SceneZoomInput()
        {
            if (_page != "Explore") return false;
            bool reset = Input.GetKeyDown(KeyCode.Space) && _sceneZoom > 1;
            if (reset) { _sceneZoom = 1; ApplySceneZoom(); return true; }
            if (Input.mouseScrollDelta.y == 0) return false;
            // 列表和对白消费自己的滚轮，不同时缩放场景。
            var hits = new System.Collections.Generic.List<RaycastResult>();
            if (EventSystem.current)
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = Input.mousePosition }, hits);
            if (hits.Any(x => x.gameObject.GetComponentInParent<ScrollRect>() != null)) return false;
            _sceneZoom = Mathf.Clamp(_sceneZoom + Input.mouseScrollDelta.y * .05f, 1, 1.35f);
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
        }

        private void SceneCharacters(bool talk, int[] participants)
        {
            var v = V("Explore");
            bool group = ConversationAvailable() || talk && ConversationChoices.Any(x => x.Event == S.ActiveEvent);
            var people = v.Get<RectTransform>("People");
            bool three = talk && participants.Length == 2 && !group;
            Show(v, "AvatarStack", group && !_avatarsExpanded);
            Btn(v, "AvatarStack", () => { _avatarsExpanded = !_avatarsExpanded; Explore(); }, "", !Engine.Busy);
            var grid = people.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112, 143);
            if (group && _avatarsExpanded)
            {
                foreach (var choice in ConversationChoices)
                {
                    var local = choice;
                    var card = Card(people, Engine.CharacterName(choice.Character), "", () => ChooseConversation(local), template: ConversationPortraitPrefab);
                    card.Get<Image>("Portrait").sprite = PortraitSprite(choice.Character);
                    card.Get<Image>("Portrait").gameObject.SetActive(card.Get<Image>("Portrait").sprite != null);
                }
            }
            else if (three)
            {
                foreach (int character in participants)
                {
                    int id = character;
                    var card = Card(people, Engine.CharacterName(id), "", () => { _selectedCharacter = id; Portrait(id); }, template: ConversationPortraitPrefab);
                    card.Get<Image>("Portrait").sprite = PortraitSprite(id);
                    card.Get<Image>("Portrait").gameObject.SetActive(card.Get<Image>("Portrait").sprite != null);
                }
            }
            int[] visibleParticipants = participants;
            if (!talk && S.Transcript.Count > 0)
                visibleParticipants = S.Transcript.Where(x => x.Plot != 0).Select(x => _data.Plot.First(p => p.Id == x.Plot).SpeakerId).Where(x => x != 0 && x != 200001).Distinct().ToArray();
            if (!visibleParticipants.Contains(_selectedCharacter)) _selectedCharacter = visibleParticipants.FirstOrDefault();
            if ((group || participants.Length > 2) && talk)
            {
                int speaker = S.Transcript.Where(x => x.Plot != 0).Select(x => _data.Plot.First(p => p.Id == x.Plot).SpeakerId).LastOrDefault(x => x != 0 && x != 200001 && PortraitSprite(x));
                if (speaker != 0) _selectedCharacter = speaker;
            }
            DrawCharacter("HeroineImage", 200001, !three);
            DrawCharacter("CharacterImage", _selectedCharacter, talk || S.Transcript.Count > 0 || !group);
        }

        private void ChooseConversation(V24ConversationChoice choice)
        {
            int next = ConversationOrder.FirstOrDefault(id => !Engine.Done(_data.EventById(id)));
            if (Engine.Busy || next != 0 && next != choice.Event || !Engine.Eligible(_data.EventById(choice.Event)))
            { Toast("ta好像正在忙"); return; }
            if (Engine.Start(choice.Event)) { _avatarsExpanded = false; _selectedCharacter = choice.Character; Refresh(); }
        }
    }
}
