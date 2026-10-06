using System;
using UnityEngine;
namespace Castle.V2
{
    [Serializable] public sealed class DeviceDefinition
    {
        public string Id, Channel, Label, Room;
        public int Offset;
        public string[] Proof = Array.Empty<string>();
        public bool Personal;
    }
    [CreateAssetMenu(menuName = "Castle/Device Config")]
    public sealed class DeviceConfig : ScriptableObject { public DeviceDefinition[] Devices = Array.Empty<DeviceDefinition>(); }
}
