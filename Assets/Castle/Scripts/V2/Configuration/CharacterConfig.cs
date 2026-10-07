using System;
using UnityEngine;
namespace Castle.V2
{
    [Serializable] public sealed class CharacterDefinition { public string Id, Name; public Sprite Portrait; public Texture2D Illustration; }
    [CreateAssetMenu(menuName = "Castle/Character Config")]
    public sealed class CharacterConfig : ScriptableObject { public CharacterDefinition[] Characters = Array.Empty<CharacterDefinition>(); }
}
