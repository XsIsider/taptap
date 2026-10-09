using System.Linq;
using UnityEditor;
using UnityEngine;
namespace Castle.Editor
{
    [CustomEditor(typeof(CastleSceneUi))]
    public sealed class CastleSceneUiInspector : UnityEditor.Editor
    {
        int previewIndex;
        public override void OnInspectorGUI()
        {
            var ui=(CastleSceneUi)target;
            EditorGUILayout.HelpBox("固定页面已保存在场景中。下方预览只切换显隐，不会重新创建 UI。按钮的 On Click 由 CastleGame 运行时绑定，无须重复添加。",MessageType.Info);
            if(ui.Pages!=null && ui.Overlays!=null)
            {
                var views=ui.Pages.Concat(ui.Overlays).Where(v=>v).ToArray();
                using(new EditorGUI.DisabledScope(Application.isPlaying || views.Length==0))
                {
                    previewIndex=EditorGUILayout.Popup("编辑时预览页面",Mathf.Clamp(previewIndex,0,Mathf.Max(0,views.Length-1)),views.Select(v=>v.Id).ToArray());
                    if(GUILayout.Button("显示所选页面（编辑模式）") && views.Length>0)
                    {
                        var view=views[previewIndex];
                        Undo.RecordObjects(views.Select(v=>(Object)v.gameObject).Concat(new Object[]{ui.OverlayRoot,ui.SceneHud.gameObject}).ToArray(),"Preview Castle UI");
                        foreach(var item in views)item.gameObject.SetActive(item==view);
                        ui.OverlayRoot.SetActive(ui.Overlays.Contains(view));
                        ui.SceneHud.gameObject.SetActive(view.Id=="Outside" || view.Id=="Lounge" || view.Id=="Room");
                        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
                        EditorGUIUtility.PingObject(view.gameObject);
                    }
                }
            }
            EditorGUILayout.Space();DrawDefaultInspector();
        }
    }
}
