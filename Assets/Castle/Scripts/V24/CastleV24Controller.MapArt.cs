using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.V24
{
    [Serializable] public sealed class V24MapRegion { public int Room; public float[] Bounds; }
    [Serializable] public sealed class V24MapLayout { public float[] Crop; public V24MapRegion[] Regions; }
    public sealed partial class CastleV24Controller
    {
        public Texture2D Floor1Background, Floor2Background;
        private V24MapLayout _mapLayout;
        private void MapArt(CastleUiView view, int floor)
        {
            if (_mapLayout == null) _mapLayout = JsonUtility.FromJson<V24MapLayout>(Resources.Load<TextAsset>("Castle/V24/MapLayout").text);
            var image = view.Get<RawImage>("FloorImage");
            image.texture = floor == 1 ? Floor1Background : Floor2Background;
            var c = _mapLayout.Crop;
            image.uvRect = new Rect(c[0], 1-c[1]-c[3], c[2], c[3]); image.color = Color.white;
        }
        private Vector2 MapPoint(int room)
        {
            var b = _mapLayout.Regions.First(x => x.Room == room).Bounds; var c = _mapLayout.Crop;
            return new Vector2((b[0]+b[2]/2-c[0])/c[2], (b[1]+b[3]/2-c[1])/c[3]);
        }
        private void PlaceMapRegion(CastleUiView card, RectTransform root, int room)
        {
            var b = _mapLayout.Regions.First(x => x.Room == room).Bounds; var c = _mapLayout.Crop;
            var point = MapPoint(room); var rect = (RectTransform)card.transform;
            rect.anchorMin=rect.anchorMax=new Vector2(point.x,1-point.y); rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            rect.sizeDelta = new Vector2(root.rect.width*b[2]/c[2], root.rect.height*b[3]/c[3]);
            var title = card.Get<Text>("Title"); title.fontSize=16;title.alignment=TextAnchor.MiddleCenter;
            title.resizeTextForBestFit=true;title.resizeTextMinSize=10;title.resizeTextMaxSize=16;
            title.rectTransform.anchorMin=Vector2.zero;title.rectTransform.anchorMax=Vector2.one;title.rectTransform.offsetMin=new Vector2(2,2);title.rectTransform.offsetMax=new Vector2(-2,-2);
            card.Get<Text>("Body").gameObject.SetActive(false);
        }
    }
}
