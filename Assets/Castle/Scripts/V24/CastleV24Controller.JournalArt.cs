using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        public CastleUiView JournalRecordPrefab;
        public V24ItemArt[] ItemArt = System.Array.Empty<V24ItemArt>();

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
            for (int i=0;i<3;i++) view.Get<Button>("Tab"+i).transform.localScale = Vector3.one * (i==S.JournalCategory?1.18f:1);
            Show(view,"CharacterDecoration",S.JournalCategory==0);
            Show(view,"RecordDecoration",S.JournalCategory==2);
            Show(view,"DetailHead",S.JournalCategory==0 && S.JournalSelection!=0);
            Show(view,"Category",false);
            bool isCharacter = S.JournalCategory == 0;
            var title = view.Get<Text>("DetailTitle").rectTransform;
            JournalPlace(title,isCharacter?1068:1084.4f,isCharacter?184:200.7f,300,85);
            title.localRotation = Quaternion.Euler(0,0,isCharacter?-4f:3.53f);
            JournalPlace(view.Get<ScrollRect>("DetailScroll").transform,isCharacter?950:890,isCharacter?409:340,isCharacter?420:450,isCharacter?330:390);
            var body = view.Get<Text>("DetailBody");
            body.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,isCharacter?405:445);
            // 人物资料统一加大行距，贴合便笺横线；切换记录页时恢复原间距。
            body.lineSpacing = isCharacter ? 1.4f : 1f;
            view.Get<ScrollRect>("DetailScroll").verticalNormalizedPosition=1;
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

        private void LayoutItemCard(CastleUiView card,V24Entry entry,bool reverse)
        {
            JournalPlace(card.Get<RectTransform>("ArtFrame"),reverse?325:0,25,190,190);
            JournalPlace(card.Get<RawImage>("ItemArt").transform,reverse?340:15,40,160,160);
            JournalPlace(card.Get<Image>("Tape").transform,reverse?0:215,25,295,55);
            JournalPlace(card.Get<Text>("Title").transform,reverse?5:220,30,280,45);
            JournalPlace(card.Get<Text>("Body").transform,reverse?5:220,95,280,170);
            var art=card.Get<RawImage>("ItemArt");
            art.texture=ItemTexture(entry);
            art.uvRect=new Rect(0,0,1,1);art.gameObject.SetActive(art.texture!=null);
            if (art.texture) FitItemArt(art, reverse?340:15, 40, 160, 160);
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
