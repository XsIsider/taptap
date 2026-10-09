using System;
using System.Collections.Generic;
using UnityEngine;

namespace Castle
{
    [Serializable]
    public sealed class CastleUiBinding
    {
        public string Id;
        public Component Target;
    }

    // 引用按稳定键保存；改 Hierarchy 名称或移动物体不影响接线。
    public sealed class CastleUiView : MonoBehaviour
    {
        public string Id;
        public List<CastleUiBinding> Controls = new List<CastleUiBinding>();
        Dictionary<string, Component> _controls;
        public T Get<T>(string id) where T : Component
        {
            if (_controls == null)
            {
                _controls = new Dictionary<string, Component>();
                foreach (var item in Controls)
                {
                    if (string.IsNullOrEmpty(item.Id) || !item.Target || _controls.ContainsKey(item.Id))
                        throw new InvalidOperationException("UI 引用缺失或重复：" + Id + "/" + item.Id);
                    _controls.Add(item.Id, item.Target);
                }
            }
            if (!_controls.TryGetValue(id, out var control) || !(control is T))
                throw new InvalidOperationException("UI 引用类型不匹配：" + Id + "/" + id + "，需要 " + typeof(T).Name);
            return (T)control;
        }
        public void ValidateBindings()
        {
            _controls = null;
            foreach (var item in Controls) Get<Component>(item.Id);
        }
    }
}
