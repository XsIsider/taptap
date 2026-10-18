using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        private void Journal(int category=-1,int target=0,bool directed=false)
        {
            if(category>=0 && category!=S.JournalCategory){S.JournalPages[S.JournalCategory]=S.JournalPage;S.JournalCategory=category;S.JournalPage=S.JournalPages[category];}
            if(target!=0)
            {
                S.JournalSelection=target;
                int index=JournalIds().ToList().IndexOf(target);S.JournalPage=Mathf.Max(0,index/JournalSize());
            }
            else if(!directed)S.JournalSelection=0;
            Push("Journal");JournalRender();
        }
        private int JournalSize()=>S.JournalCategory==2?6:4;
        private int[] JournalIds()
        {
            if(S.JournalCategory==0)return S.Entries.Select(_data.EntryById).Where(x=>x.Type=="info").SelectMany(x=>x.Owner).Distinct().ToArray();
            if(S.JournalCategory==1)return S.Entries.Where(x=>{var t=_data.EntryById(x).Type;return t=="item"||t=="tape";}).ToArray();
            return S.Entries.Where(x=>_data.EntryById(x).Type=="event").ToArray();
        }
        private void JournalRender()
        {
            var v=V("Journal");Btn(v,"Close",Pop);
            for(int i=0;i<3;i++){int tab=i;Btn(v,"Tab"+i,()=>{S.JournalPages[S.JournalCategory]=S.JournalPage;S.JournalCategory=tab;S.JournalPage=S.JournalPages[tab];S.JournalSelection=0;JournalRender();});}
            int[] ids=JournalIds();int size=JournalSize(),pages=Math.Max(1,(ids.Length+size-1)/size);S.JournalPage=Mathf.Clamp(S.JournalPage,0,pages-1);
            int[] current=ids.Skip(S.JournalPage*size).Take(size).ToArray();if(!current.Contains(S.JournalSelection))S.JournalSelection=current.FirstOrDefault();
            Txt(v,"PageNumber",(S.JournalPage+1)+" / "+pages);Txt(v,"Category",new[]{"人物","道具","记录"}[S.JournalCategory]);Txt(v,"Empty",ids.Length==0?"尚未收录":"");
            var left=v.Get<RectTransform>("LeftCards");var right=v.Get<RectTransform>("RightCards");Clear(left);Clear(right);
            Show(v,"Details",S.JournalCategory!=1);Show(v,"RightCards",S.JournalCategory==1);
            var grid=left.GetComponent<GridLayoutGroup>();grid.constraintCount=S.JournalCategory==0?2:1;grid.cellSize=S.JournalCategory==0?new Vector2(230,205):S.JournalCategory==1?new Vector2(486,205):new Vector2(486,64);
            for(int i=0;i<current.Length;i++)
            {
                int id=current[i];var entry=S.JournalCategory==0?null:_data.EntryById(id);
                string name=entry?.Name??Engine.CharacterName(id);
                string body=S.JournalCategory==1?entry.Text:"点击查看详情";
                if(entry?.Type=="tape")
                {
                    var tapes=S.Tapes.Where(x=>x.Entry==id).ToArray();
                    body+="\n"+(tapes.Length==1?"当前："+(_data.RecordById(tapes[0].Record)?.Name??"空白磁带"):"共 "+tapes.Length+" 盘，点击选择");
                }
                var card=Card(S.JournalCategory==1&&i>=2?right:left,name,body,()=>
                {
                    S.JournalSelection=id;
                    if(S.JournalCategory==1)UseEntry(id);else JournalRender();
                },false,S.JournalCategory==0?PersonCardPrefab:S.JournalCategory==1?ItemCardPrefab:null);
                if(S.JournalCategory==0){var portrait=card.Get<Image>("Portrait");portrait.sprite=id==200003?ButlerPortrait:id==200004?MerchantPortrait:null;portrait.gameObject.SetActive(portrait.sprite!=null);card.Get<Text>("Body").text=portrait.sprite?"":"头像待补充";}
                card.Get<Text>("Title").fontSize=S.JournalCategory==2?18:22;
                if(S.JournalCategory==2)card.Get<Text>("Body").text="";
            }
            if(S.JournalCategory==0 && S.JournalSelection!=0)
            {
                Txt(v,"DetailTitle",Engine.CharacterName(S.JournalSelection));
                Txt(v,"DetailBody",string.Join("\n\n",S.Entries.Select(_data.EntryById).Where(x=>x.Type=="info"&&x.Owner.Contains(S.JournalSelection)).Select(x=>x.Text)));
            }
            else if(S.JournalCategory==2 && S.JournalSelection!=0)
            {
                var entry=_data.EntryById(S.JournalSelection);Txt(v,"DetailTitle",entry.Name);Txt(v,"DetailBody",entry.Text);
            }
            else {Txt(v,"DetailTitle","");Txt(v,"DetailBody","");}
            Btn(v,"Previous",()=>StartCoroutine(Flip(-1)),enabled:S.JournalPage>0);Btn(v,"Next",()=>StartCoroutine(Flip(1)),enabled:S.JournalPage+1<pages);
            if(v.TryGet<Button>("RecordFilter",out var filter))filter.gameObject.SetActive(false);
            if(v.TryGet<Button>("Source",out var source))source.gameObject.SetActive(false);
            var timeline=_data.Puzzle.First(x=>x.Type=="timeline");
            Show(v,"Timeline",S.JournalCategory==2);Btn(v,"Timeline",()=>{_puzzle=timeline.Id;CloseAll();Page("Timeline");},"重构事件时间线");
            var path=_data.Puzzle.FirstOrDefault(x=>x.Type=="person_path"&&x.Target==S.JournalSelection);
            Show(v,"CharacterPath",S.JournalCategory==0&&path!=null);
            Btn(v,"CharacterPath",()=>{_puzzle=path.Id;Push("Path",JournalRender);PathRender(V("Path"));},"人物路径",path!=null);
            Save();
        }
        private string PuzzleRequirements(V24Puzzle puzzle)
        {
            var missing=puzzle.Need.Where(x=>!Engine.Need(new[]{x})).Select(x=>
            {
                var pair=x.Split(':');int id=int.Parse(pair[1]);
                if(pair[0]=="entry")return _data.EntryById(id)?.Name??x;
                if(pair[0]=="puzzle")return "完成“"+_data.PuzzleById(id).Name+"”";
                if(pair[0]=="goal")return "完成“"+(_data.Event.FirstOrDefault(e=>e.Goal==id)?.Name??x)+"”";
                return x;
            }).ToArray();
            return missing.Length>0?"提交前还需："+string.Join("、",missing):Engine.Busy?"请先完成当前对白与奖励，再提交。":"条件已满足，排列完成后可提交。";
        }
        private IEnumerator Flip(int direction)
        {
            var sheet=V("Journal").Get<RectTransform>("FlipSheet");sheet.gameObject.SetActive(true);
            for(float t=0;t<.25f;t+=Time.unscaledDeltaTime){sheet.localScale=new Vector3(Mathf.Abs(1-t/.125f),1,1);yield return null;}
            sheet.localScale=Vector3.one;sheet.gameObject.SetActive(false);S.JournalPage+=direction;S.JournalSelection=0;JournalRender();
        }
        private void UseEntry(int id)
        {
            var entry=_data.EntryById(id);
            if(entry.Type=="tape")
            {
                var tapes=S.Tapes.Where(x=>x.Entry==id).ToArray();
                if(tapes.Length==1){UseTape(tapes[0].Instance);return;}
                Push("TapeSelect");var v=V("TapeSelect");Clear(v.Get<RectTransform>("List"));Btn(v,"Close",Pop);
                foreach(var tape in tapes){int instance=tape.Instance;Card(v.Get<RectTransform>("List"),entry.Name+" #"+instance,_data.RecordById(tape.Record)?.Name??"空白磁带",()=>{Pop();UseTape(instance);});}
                return;
            }
            if(id==_data.GameSettings.MapEntry){Map();return;}
            string result=Engine.UseItem(id);
            if(result=="使用成功"){CloseAll();Refresh();Toast(result);}
            else if(id==_data.InvitationEntry){Push("Letter");Txt(V("Letter"),"Body",entry.Text);Btn(V("Letter"),"Close",Pop);Toast(result);}
            else Toast(result);
        }
        private void UseTape(int instance)
        {
            if(S.SleepPending){Toast("即将休息，请先完成对白");return;}
            var tape=S.Tapes.First(x=>x.Instance==instance);_selectedTape=instance;
            if(S.Room==_data.GameSettings.ControlRoom)
            {
                if(tape.Record!=0)Confirm("是否覆盖这盘磁带？取消或关闭文件选择会保留原录音。",BurnSelect);else BurnSelect();
            }
            else if(S.Room==_data.GameSettings.HomeRoom)
            {
                if(tape.Record==0){Toast("磁带为空");return;}
                var record=_data.RecordById(tape.Record);
                if(record.AccessType=="tape"&&!Engine.Need(record.Need)){Toast("尚未满足播放条件");return;}
                _playTape=instance;S.PlayingTape=instance;_record=record.Id;_position=0;_playing=true;CloseAll();Page("Tape");
            }
            else Toast("此处无法使用磁带");
        }
        private void BurnSelect()
        {
            _burnRecord=0;Push("Burn");var v=V("Burn");Btn(v,"Close",Pop);Clear(v.Get<RectTransform>("List"));
            var records=_data.Record.Where(Engine.Accessible).OrderBy(x=>x.Device).ToArray();
            Txt(v,"Details","选择文件后点击刻录");
            Txt(v,"Heading",records.Length==0?"没有可刻录文件":"选择刻录文件");
            foreach(var group in records.GroupBy(x=>x.Device))
            {
                int device=group.Key;bool expanded=S.ExpandedDevices.Contains(device);
                Card(v.Get<RectTransform>("List"),(expanded?"▾ ":"▸ ")+Engine.DeviceName(device),group.Count()+" 个文件",()=>{if(expanded)S.ExpandedDevices.Remove(device);else S.ExpandedDevices.Add(device);Pop();BurnSelect();});
                if(!expanded)continue;
                foreach(var record in group){int id=record.Id;Card(v.Get<RectTransform>("List"),record.Name,"点击选择文件",()=>{_burnRecord=id;Txt(v,"Details",record.Name);Btn(v,"Burn",BurnNow,"刻录",true);});}
            }
            Btn(v,"Burn",BurnNow,"刻录",false);
        }
        private void BurnNow(){Engine.Burn(_selectedTape,_burnRecord);Pop();JournalRender();Toast("刻录成功");}
    }
}

