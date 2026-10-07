using System;
using UnityEngine;
namespace Castle.V2
{
    [Serializable] public sealed class HotspotDefinition { public string Id, Room, Mode, PuzzleId, Label; public Vector2 Position; }
    [CreateAssetMenu(menuName = "Castle/Hotspot Binding")]
    public sealed class HotspotBinding : ScriptableObject { public HotspotDefinition[] Hotspots = Array.Empty<HotspotDefinition>(); }
}
