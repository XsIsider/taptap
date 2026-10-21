using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Castle
{
    [Serializable]
    public sealed class CastleRoomButton
    {
        public string RoomId;
        public Button Button;
        public Text Label;
        public RectTransform MarkerAnchor;
        [Tooltip("从房间出口到公共走廊的路点，坐标属于 MapArea。")]
        public RectTransform[] Corridor;
    }

    public sealed class CastleMapView : MonoBehaviour
    {
        public RectTransform MapArea;
        public RawImage Background;
        public Texture2D Floor1, Floor2;
        public CastleRoomButton[] Rooms;
        public Text[] Markers;
        public RectTransform[] Lines;
        public int Floor { get; private set; }
        int _usedLines;
        public void ShowFloor(int floor, CastleDatabase database)
        {
            Floor = floor; Background.texture = floor == 1 ? Floor1 : Floor2;
            foreach (var room in Rooms) room.Button.gameObject.SetActive(database.Room(room.RoomId).floor == floor);
            foreach (var line in Lines) line.gameObject.SetActive(false);
            foreach (var marker in Markers) marker.gameObject.SetActive(false);
            _usedLines = 0;
        }
        CastleRoomButton Find(string title, CastleDatabase database) => Array.Find(Rooms, r => database.Room(r.RoomId).title == title);
        Vector2 Position(RectTransform point)
        {
            var p = MapArea.InverseTransformPoint(point.TransformPoint(point.rect.center));
            return new Vector2(p.x, p.y);
        }
        public void Mark(int index, string roomTitle, string text, CastleDatabase database)
        {
            if (Floor != 1 || string.IsNullOrEmpty(roomTitle)) return;
            var room = Find(roomTitle, database); if (room == null) return;
            var marker = Markers[index]; marker.gameObject.SetActive(true); marker.text = text;
            marker.rectTransform.localPosition = Position(room.MarkerAnchor);
        }
        public void Connect(string from, string to, bool dashed, CastleDatabase database)
        {
            if (Floor != 1 || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return;
            var a = Find(from, database); var b = Find(to, database);
            if (a == null || b == null) return;
            var points = new List<Vector2> { Position(a.MarkerAnchor) };
            foreach (var p in a.Corridor) points.Add(Position(p));
            for (int i = b.Corridor.Length - 1; i >= 0; i--) points.Add(Position(b.Corridor[i]));
            points.Add(Position(b.MarkerAnchor));
            for (int i = 1; i < points.Count; i++)
            {
                if (dashed)
                    for (float t = 0; t < 1; t += .17f) Segment(Vector2.Lerp(points[i - 1], points[i], t), Vector2.Lerp(points[i - 1], points[i], Mathf.Min(1, t + .09f)));
                else Segment(points[i - 1], points[i]);
            }
        }
        void Segment(Vector2 a, Vector2 b)
        {
            if (_usedLines >= Lines.Length) throw new InvalidOperationException("地图预置连线不足，请增加 Lines 引用。");
            var line = Lines[_usedLines++]; line.gameObject.SetActive(true);
            line.localPosition = a; line.sizeDelta = new Vector2(Vector2.Distance(a, b), line.sizeDelta.y);
            line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
        }
    }
}
