using System;
using UnityEngine;
namespace Castle.V2
{
    [Serializable] public sealed class MapNode { public string Id, Room; public int Floor; public Vector2 Position; }
    [Serializable] public sealed class FloorBinding { public int Floor; public Texture2D Background; }
    [Serializable] public sealed class RoomBinding { public string Room; public Texture2D Background; }
    [CreateAssetMenu(menuName = "Castle/Map Config")]
    public sealed class MapConfig : ScriptableObject
    {
        public string Id = "MAP_CASTLE";
        public MapNode[] Nodes = Array.Empty<MapNode>();
        public FloorBinding[] Floors = Array.Empty<FloorBinding>();
        public RoomBinding[] Rooms = Array.Empty<RoomBinding>();
    }
}
