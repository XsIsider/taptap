using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Castle.Editor
{
    public static class CastleProjectTools
    {
        const string ScenePath = "Assets/Castle/Scenes/CastleEditable.unity";
        static int checkCount;

        [MenuItem("Tools/Castle/Open Editable Scene")]
        public static void OpenScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (!File.Exists(ScenePath)) Setup();
            else EditorSceneManager.OpenScene(ScenePath);
        }

        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Castle/Scenes");
            var db = AssetDatabase.LoadAssetAtPath<CastleDatabase>("Assets/Castle/Resources/Castle/Database.asset");
            if (!db)
            {
                db = ScriptableObject.CreateInstance<CastleDatabase>();
                AssetDatabase.CreateAsset(db, "Assets/Castle/Resources/Castle/Database.asset");
            }
            CastleSceneUiBuilder.CreateIfMissing();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Castle/Validate Game Flow")]
        public static void Validate()
        {
            checkCount=0;
            Setup();
            var db = AssetDatabase.LoadAssetAtPath<CastleDatabase>("Assets/Castle/Resources/Castle/Database.asset");
            var s = new CastleState { started = true, invitation = true, room = "101", minutes = 1130 };
            Require(CastleRules.CanVisit(s, db.Room("110")) != null, "中控室必须有许可");
            for (int i = 0; i < 5; i++) CastleRules.AdvanceDialogue(s);
            Require(s.mapUnlocked && s.controlUnlocked && s.recorded && s.minutes == 1132, "对话解锁并仅推进两分钟");
            CastleRules.AdvanceDialogue(s);
            Require(s.minutes == 1132, "完成对白后不能重复推进时间");
            Require(CastleRules.CanVisit(s, db.Room("110")) != null, "开放前不能进入中控室");
            s.minutes = 1140;
            Require(CastleRules.Travel(s, db.Room("110")) == null && s.minutes == 1142, "等待后可进入，移动耗时两分钟");
            Require(CastleRules.CanVisit(s, db.Room("201")) != null, "未发现房间不可进入");
            Require(CastleRules.Align(s) != null, "无锚点不能校正");
            s.anchorA=s.anchorB=s.pairA=s.pairB=true;
            Require(CastleRules.Align(s) != null, "错误校正量必须失败");
            CastleRules.ChangeTime(s,180);
            Require(CastleRules.Align(s)==null, "正确的三分钟偏移应成立");
            s.source="餐厅";s.door=DoorState.Open;s.doorEvidence=true;s.pathSaved=true;
            Require(CastleRules.VerifyEvent(s)!=null, "开门假设与闷响冲突");
            s.door=DoorState.Closed;s.doorEvidence=false;
            Require(CastleRules.VerifyEvent(s)!=null, "正确猜测仍需要证据");
            s.doorEvidence=true;
            Require(CastleRules.VerifyEvent(s)==null, "完整声音证据应通过");
            CastleRules.ChangeTime(s,0);
            Require(!s.aligned && !s.pathSaved && s.confirmedEvent.offset==180, "草稿失效不应覆盖已确认事件");
            s.candidate="陈先生";s.identityEvidence=true;
            Require(CastleRules.VerifyIdentity(s)!=null, "错误人物必须被拒绝");
            s.candidate="林女士";
            Require(CastleRules.VerifyIdentity(s)==null, "证词可以关联正确人物");
            s.route=new[]{"书房","休息室","餐厅"};s.routeEvidence=true;
            Require(CastleRules.VerifyRoute(s)!=null, "错误路线必须失败");
            s.route=new[]{"书房","餐厅","休息室"};
            Require(CastleRules.VerifyRoute(s)==null, "正确路线应通过");
            s.route[0]="餐厅";
            Require(s.confirmedRoute[0]=="书房", "已确认路线必须独立保存");
            Require(CastleRules.Submit(s)!=null, "最终关系需要物证");
            s.finalEvidence=true;
            Require(CastleRules.Submit(s)==null && s.finalOK, "完整流程可以进入真相重建");
            var restored=JsonUtility.FromJson<CastleState>(JsonUtility.ToJson(s));
            Require(restored.confirmedRoute[0]=="书房" && restored.confirmedEvent.offset==180 && restored.finalOK, "存档序列化保留证据快照");
            Require(db.rooms.Select(r=>r.id).Distinct().Count()==db.rooms.Length, "房间 ID 唯一");
            foreach(var name in new[]{"Chinese","ControlRoom","Title","Invitation","Floor1","Floor2","Outside","Lounge","EmptyRoom"})
                Require(Resources.Load("Castle/"+name)!=null, "资源可加载: "+name);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/castle-validation.txt", "PASS: "+checkCount+" game-flow, evidence, serialization and content checks.\n"+DateTime.UtcNow.ToString("O"));
            Debug.Log("CASTLE VALIDATION PASSED");
        }

        static void Require(bool condition,string message)
        { checkCount++; if(!condition) throw new Exception("CASTLE VALIDATION FAILED: "+message); }

        [MenuItem("Tools/Castle/Build Windows Prototype")]
        public static void Build()
        {
            Validate();
            Directory.CreateDirectory("Builds/Castle");
            var report=BuildPipeline.BuildPlayer(new[]{ScenePath},"Builds/Castle/Castle.exe",BuildTarget.StandaloneWindows64,BuildOptions.Development);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Castle build failed: "+report.summary.result);
        }
    }

    public sealed class CastleTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Castle/Resources/Castle/")) return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Default;
            importer.mipmapEnabled=false;
            importer.maxTextureSize=2048;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;
        }
    }
}
