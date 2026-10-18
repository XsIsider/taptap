using System.Linq;
using Castle.Flow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Castle.Editor
{
    [CustomEditor(typeof(CastleFlowUi))]
    public sealed class CastleFlowUiInspector : UnityEditor.Editor
    {
        private int _selected;
        public override void OnInspectorGUI()
        {
            var ui = (CastleFlowUi)target;
            EditorGUILayout.HelpBox("页面已保存在 CastleEditable 场景中。选择页面即可在未运行时编辑；运行时只生成列表条目。按钮由控制器统一接线，不要重复添加 On Click。", MessageType.Info);
            var views = ui.Pages.Concat(ui.Modals).ToArray();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                _selected = EditorGUILayout.Popup("预览页面", Mathf.Clamp(_selected, 0, views.Length - 1), views.Select(view => view.Id).ToArray());
                if (GUILayout.Button("显示并定位所选页面"))
                {
                    var selected = views[_selected];
                    Undo.RecordObjects(views.Select(view => (Object)view.gameObject).Concat(new Object[] { ui.ModalRoot, ui.Hud.gameObject }).ToArray(), "Preview Flow UI");
                    foreach (var view in views) view.gameObject.SetActive(view == selected);
                    ui.ModalRoot.SetActive(ui.Modals.Contains(selected));
                    ui.Hud.gameObject.SetActive(ui.Pages.Contains(selected) && selected.Id != "Title" && selected.Id != "Invitation");
                    ui.Toast.gameObject.SetActive(false);
                    EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
                    EditorGUIUtility.PingObject(selected.gameObject);
                    SceneView.lastActiveSceneView?.Frame(new Bounds(selected.transform.position, new Vector3(1600,900,0)), false);
                }
            }
            DrawDefaultInspector();
        }
    }
}
