using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        public CastleUiView JournalRecordPrefab;
        public V24ItemArt[] ItemArt = System.Array.Empty<V24ItemArt>();
        public Sprite BookmarkNormal, BookmarkSelected;

        private Texture2D ItemTexture(V24Entry entry, bool detail = false)
        {
            string key = detail ? entry.DetailImage : entry.Icon;
            var texture = ItemArt.FirstOrDefault(x => x.Key == key)?.Texture;
            if (texture) return texture;
            if (entry.Id == _data.InvitationEntry) return InvitationOpen.texture;
            return entry.Id == _data.GameSettings.MapEntry ? Floor1Background : null;
        }

        private void ItemDetail(V24Entry entry, string body)
        {
            Push("Letter");
            var view = V("Letter");
            var art = view.Get<RawImage>("ItemArt");
            art.texture = ItemTexture(entry, true);
            art.gameObject.SetActive(art.texture != null);
            Txt(view, "Body", body);
            Btn(view, "Close", Pop);
        }

        private void JournalAppearance(CastleUiView view)
        {
            SyncJournalPaper(view);
            for (int i=0;i<3;i++)
            {
                bool selected = i == S.JournalCategory;
                var tab = view.Get<Button>("Tab"+i);
                JournalPlace(tab.transform,201.667f+i*62.5f,selected?20.833f:32.5f,59.167f,selected?79.167f:67.5f);
                tab.transform.localScale = Vector3.one;
                tab.GetComponent<Image>().sprite = selected ? BookmarkSelected : BookmarkNormal;
                var icon = (RectTransform)tab.transform.Find("BookmarkIcon");
                icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, selected?-19f:-12f);
            }
            Show(view,"PlainRightPage",S.JournalCategory!=0);
            Show(view,"CharacterDecoration",S.JournalCategory==0);
            Show(view,"RecordDecoration",S.JournalCategory==2);
            Show(view,"DetailHead",S.JournalCategory==0 && S.JournalSelection!=0);
            Show(view,"Category",false);
            bool isCharacter = S.JournalCategory == 0;
            var title = view.Get<Text>("DetailTitle").rectTransform;
            JournalPlace(title,isCharacter?1120:1125,isCharacter?190:112,260,70);
            title.localRotation = Quaternion.identity;
            JournalPlace(view.Get<ScrollRect>("DetailScroll").transform,915,isCharacter?375:355,455,isCharacter?360:380);
            var body = view.Get<Text>("DetailBody");
            body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,445);
            // 人物资料统一加大行距，贴合便笺横线；切换记录页时恢复原间距。
            body.lineSpacing = isCharacter ? 1.4f : 1f;
            view.Get<ScrollRect>("DetailScroll").verticalNormalizedPosition=1;
            JournalPlace(view.Get<RectTransform>("LeftCards"),isCharacter?226.667f:S.JournalCategory==1?233:225,isCharacter?111.667f:S.JournalCategory==1?155:158,530,680);
            JournalPlace(view.Get<RectTransform>("RightCards"),865,130,530,680);
        }

        private static void SyncJournalPaper(CastleUiView view)
        {
            // Cover 是场景作者的尺寸入口；分类覆盖图只跟随它，不反向改写封面。
            var cover = (RectTransform)view.transform.Find("Cover");
            var paper = view.Get<RawImage>("PlainRightPage").rectTransform;
            paper.anchorMin = cover.anchorMin;
            paper.anchorMax = cover.anchorMax;
            paper.pivot = cover.pivot;
            paper.sizeDelta = cover.sizeDelta;
            paper.anchoredPosition3D = cover.anchoredPosition3D;
            paper.localRotation = cover.localRotation;
            paper.localScale = cover.localScale;
        }

        private static void JournalPlace(Transform transform,float x,float y,float width,float height)
        {
            var rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(width,height);
        }

        private void JournalCharacterPortrait(CastleUiView view)
        {
            var image = view.Get<Image>("CharacterPortrait");
            var art = S.JournalCategory == 0 && Engine.CharacterInspirationComplete(S.JournalSelection)
                ? CharacterArt.FirstOrDefault(x => x.Character == S.JournalSelection) : null;
            var sprite = art?.Portrait;
            // 横向姓名占位图不作为人物立绘显示；沿用已有正式立绘及剪影绑定。
            bool visible = sprite != null && sprite.rect.height > sprite.rect.width;
            image.sprite = visible ? sprite : null;
            if (visible)
            {
                float height = image.rectTransform.sizeDelta.y;
                image.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                    height * sprite.rect.width / sprite.rect.height);
            }
            image.gameObject.SetActive(visible);
        }

        private void LayoutItemCard(CastleUiView card,V24Entry entry,int index)
        {
            // 以 Overlays/example 的 1920×1080 参考图换算至 1600×900 画布。
            // 下排道具框比上排大，不能统一缩放为同一个尺寸。
            bool reverse = index % 2 == 1;
            float frameX = reverse ? (index < 2 ? 298.667f : 293.333f) : 0;
            float frameY = index == 0 ? 33.333f : index == 2 ? 24.167f : 31.667f;
            float frameWidth = reverse ? 216.667f : 195f;
            float frameHeight = reverse ? 187.5f : 170.833f;
            float tapeX = reverse ? (index < 2 ? 32f : 22.5f) : (index == 0 ? 257.833f : 250f);
            float tapeY = index == 0 ? 22.5f : index == 1 ? -24.167f : index == 2 ? 20f : -16.667f;
            // 原框 PNG 为 309×267，可见框为 (14,2) 起的 260×225；补偿透明留边。
            JournalPlace(card.Get<RectTransform>("ArtFrame"),frameX-14*frameWidth/260,
                frameY-2*frameHeight/225,frameWidth*309/260,frameHeight*267/225);
            JournalPlace(card.Get<Image>("Tape").transform,tapeX,tapeY,236.667f,81.667f);
            JournalPlace(card.Get<Text>("Title").transform,tapeX+5,tapeY+15,230,45);
            JournalPlace(card.Get<Text>("Body").transform,tapeX+5,tapeY+85,230,165);
            var art=card.Get<RawImage>("ItemArt");
            art.texture=ItemTexture(entry);
            art.uvRect=new Rect(0,0,1,1);art.gameObject.SetActive(art.texture!=null);
            if (art.texture) FitItemArt(art, frameX+15, frameY+15, frameWidth-30, frameHeight-35);
        }

        private static void FitItemArt(RawImage art, float x, float y, float width, float height)
        {
            float scale = Mathf.Min(width / art.texture.width, height / art.texture.height);
            float w = art.texture.width * scale, h = art.texture.height * scale;
            JournalPlace(art.transform, x + (width-w)/2, y + (height-h)/2, w, h);
        }

        private string RecordDescription(V24Entry entry)
        {
            var lines = new System.Collections.Generic.List<V24Plot>();
            foreach (string source in entry.Source.Where(x=>x.StartsWith("plot:")))
            {
                if(int.TryParse(source.Substring(5),out int id))
                {var line=_data.Plot.FirstOrDefault(x=>x.Id==id);if(line!=null)lines.Add(line);}
            }
            var direct = _data.RecordById(entry.Record);
            if(direct!=null) lines.AddRange(_data.Lines(direct.Plot).Where(x=>x.Anchor));
            // 验证结果通过 Source 指向谜题；仅显示该已验证谜题的正确证据，不展示候选干扰项。
            foreach(string source in entry.Source.Where(x=>x.StartsWith("puzzle:")))
            {
                if(!int.TryParse(source.Substring(7),out int puzzleId))continue;
                var puzzle=_data.PuzzleById(puzzleId);
                if(puzzle==null)continue;
                foreach(string token in puzzle.Answer.Split('|'))
                    if(int.TryParse(token,out int id))
                    {
                        var line=_data.Plot.FirstOrDefault(x=>x.Id==id);
                        if(line!=null&&!lines.Contains(line))lines.Add(line);
                    }
            }
            var details=lines.Select(line=>
            {
                var record=_data.Record.FirstOrDefault(x=>x.Plot==line.Group);
                string origin=record==null?"":Engine.DeviceName(record.Device)+" · "+record.Name+"\n";
                return origin+(line.Anchor?"锚点句：":"证据句：")+line.Text;
            });
            string evidence=string.Join("\n\n",details);
            return evidence.Length>0?evidence+"\n\n"+entry.Text:entry.Text;
        }
    }

    [System.Serializable]
    public sealed class V24ItemArt
    {
        public string Key;
        public Texture2D Texture;
    }
}
