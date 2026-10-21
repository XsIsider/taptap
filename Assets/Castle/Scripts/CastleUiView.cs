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
        // 仅供明确允许省略的装饰或快捷入口使用；必需引用仍由Get严格校验。
        public bool TryGet<T>(string id, out T target) where T : Component
        {
            target = null;
            foreach (var item in Controls)
                if (item.Id == id) { target = item.Target as T; return target != null; }
            return false;
        }
        public void ValidateBindings()
        {
            _controls = null;
            foreach (var item in Controls) Get<Component>(item.Id);
        }
    }
}
