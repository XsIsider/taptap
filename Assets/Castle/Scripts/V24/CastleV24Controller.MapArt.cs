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
        public Sprite MapFloor1Paper, MapFloor2Paper;
        private V24MapLayout _mapLayout;
        private void MapArt(CastleUiView view, int floor)
        {
            if (view.Id == "Map") SetMapPaperFloor(view, floor);
            if (_mapLayout == null) _mapLayout = JsonUtility.FromJson<V24MapLayout>(Resources.Load<TextAsset>("Castle/V24/MapLayout").text);
            var image = view.Get<RawImage>("FloorImage");
            image.gameObject.SetActive(true);
            image.enabled = true;
            image.texture = floor == 1 ? Floor1Background : Floor2Background;
            var c = _mapLayout.Crop;
            image.uvRect = new Rect(c[0], 1-c[1]-c[3], c[2], c[3]); image.color = Color.white;
            if (view.Id == "Path") FitPathMap(view, image);
            if (!view.TryGet<RectTransform>("CurrentLocationMarker",out var marker)) return;
            bool onFloor = _data.Room.Any(x=>x.Id==S.Room && x.Floor==floor) && _mapLayout.Regions.Any(x=>x.Room==S.Room);
            marker.gameObject.SetActive(onFloor);
            if (onFloor)
            {
                var point = MapPoint(S.Room);
                var imageRect = image.rectTransform;
                var local = (Vector2)marker.parent.InverseTransformPoint(imageRect.TransformPoint(
                    new Vector3(imageRect.rect.xMin + point.x * imageRect.rect.width,
                        imageRect.rect.yMax - point.y * imageRect.rect.height, 0)));
                var parentRect = (RectTransform)marker.parent;
                marker.anchorMin = marker.anchorMax = new Vector2(0,1);
                marker.pivot = new Vector2(.5f,0);
                marker.anchoredPosition = local - new Vector2(parentRect.rect.xMin,parentRect.rect.yMax);
                marker.SetAsLastSibling();
            }
        }

        private void FitPathMap(CastleUiView view, RawImage image)
        {
            var pan = view.Get<V24MapPan>("MapPan");
            var viewportSize = ((RectTransform)pan.transform).rect.size;
            float aspect = image.texture.width * image.uvRect.width / (image.texture.height * image.uvRect.height);
            float width = Mathf.Min(viewportSize.x, viewportSize.y * aspect);
            // 底图和房间热区共用等比内容区域，保持标记、连线和点击位置对齐。
            pan.Content.sizeDelta = new Vector2(width, width / aspect);
            var rooms = view.Get<RectTransform>("Rooms");
            rooms.anchorMin = Vector2.zero;
            rooms.anchorMax = Vector2.one;
            rooms.offsetMin = rooms.offsetMax = Vector2.zero;
            pan.ClampToContent = true;
        }

        private void SetMapPaperFloor(CastleUiView view, int floor)
        {
            var paper = view.Get<Image>("Paper");
            paper.sprite = floor == 1 ? MapFloor1Paper : MapFloor2Paper;
            // Rooms 只承载热区，旧的一层贴图不能遮住真正切层的 FloorImage。
            var roomGraphic = view.Get<RectTransform>("Rooms").GetComponent<RawImage>();
            if (roomGraphic) roomGraphic.enabled = false;
            // 由后向前：未选中标签、当层纸张、地图、房间热区、选中标签。
            // 不移动 Dismiss 到前景，保持空白关闭与楼层按钮可点击。
            var backTab = view.Get<Button>(floor == 1 ? "Floor2" : "Floor1");
            var frontTab = view.Get<Button>(floor == 1 ? "Floor1" : "Floor2");
            backTab.transform.SetSiblingIndex(1);
            paper.transform.SetSiblingIndex(2);
            view.Get<RawImage>("FloorImage").transform.SetSiblingIndex(3);
            view.Get<RectTransform>("Rooms").SetSiblingIndex(4);
            frontTab.transform.SetSiblingIndex(5);
            foreach (string id in new[] { "Floor1", "Floor2" })
            {
                var button = view.Get<Button>(id);
                button.transition = Selectable.Transition.None;
                button.GetComponent<Image>().color = Color.white;
                var label = view.Get<Text>(id + "Label");
                label.text = id == "Floor1" ? "1F" : "2F";
                label.enabled = true;
                label.gameObject.SetActive(true);
            }
        }
        private Vector2 MapPoint(int room)
        {
            var b = _mapLayout.Regions.First(x => x.Room == room).Bounds; var c = _mapLayout.Crop;
            return new Vector2((b[0]+b[2]/2-c[0])/c[2], (b[1]+b[3]/2-c[1])/c[3]);
        }
        private void ShowRoomTooltip(CastleUiView view, int cardRoom, string extra = "")
        {
            var rooms = view.Get<RectTransform>("Rooms");
            var bounds = _mapLayout.Regions.First(x => x.Room == cardRoom).Bounds;
            var crop = _mapLayout.Crop;
            // 从同一房间区域换算到弹窗父坐标，确保两层地图的说明都跟随房间。
            var point = new Vector3(MapPoint(cardRoom).x * rooms.rect.width,
                -(bounds[1] - crop[1]) / crop[3] * rooms.rect.height + 8f, 0);
            ShowMapTooltip(view, rooms.TransformPoint(point), RoomDetail(cardRoom)+(string.IsNullOrEmpty(extra)?"":"\n"+extra));
        }

        private void ShowCorridorTooltip(CastleUiView view, RectTransform hotspot)
        {
            var point = new Vector3(hotspot.rect.center.x, hotspot.rect.yMax + 8f, 0);
            ShowMapTooltip(view, hotspot.TransformPoint(point), "连廊\n常开");
        }

        private void ShowMapTooltip(CastleUiView view, Vector3 worldPoint, string text)
        {
            var panel = view.Get<RectTransform>("TooltipPanel");
            var local = (Vector2)panel.parent.InverseTransformPoint(worldPoint);
            var parent = (RectTransform)panel.parent;
            panel.anchorMin = panel.anchorMax = new Vector2(0, 1);
            panel.pivot = new Vector2(.5f, 0);
            local -= new Vector2(parent.rect.xMin, parent.rect.yMax);
            local.x = Mathf.Clamp(local.x, panel.rect.width / 2 + 8, parent.rect.width - panel.rect.width / 2 - 8);
            local.y = Mathf.Clamp(local.y, -parent.rect.height + 8, -panel.rect.height - 8);
            panel.anchoredPosition = local;
            panel.SetAsLastSibling();
            Show(view,"TooltipPanel",true);
            Txt(view,"Tooltip",text);
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
