using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using Castle.V24;

public static class VerifyInteractions20261021
{
    private static int _checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        _checks++;
    }

    public static void Main()
    {
        var json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue, RecursionLimit = 200 };
        var data = json.Deserialize<V24Data>(File.ReadAllText("Assets/Castle/Resources/Castle/V24/Content.json"));
        foreach (var gate in data.DialogueInteractionGateConfig)
        {
            var state = new V24State { Started=true, Day=1, Room=100002, Page="Explore",
                Now=new DateTime(2026,10,21,17,50,0).Ticks, DayBase=new DateTime(2026,10,21).Ticks,
                LastCompletedEvent=gate.SourceEvent, DeferredEvent=gate.ResumeEvent };
            state.CompletedEvents.Add(gate.SourceEvent);
            state.Shown.Add(gate.SourcePlot);
            state.Entries.Add(data.InvitationEntry);
            var engine = new V24Engine(data,state);
            engine.EndChain();
            Check(state.ActiveEvent==0 && state.DeferredEvent==gate.ResumeEvent,"automatic continuation blocked");
            Check(engine.CanOpenInteraction(gate.Id),"entry retained after source completion");
            Check(!engine.SelectInteractionRoom(100005),"unrelated room does not complete gate");
            Check(!engine.UseInteractionItem(300003),"unrelated item does not complete gate");
            state=json.Deserialize<V24State>(json.Serialize(state));
            engine=new V24Engine(data,state);
            Check(engine.CanOpenInteraction(gate.Id),"waiting entry survives save roundtrip");
            bool selected=gate.Id==1500003 ? engine.SelectInteractionRoom(100001) : engine.UseInteractionItem(300001);
            Check(selected,"required explicit interaction accepted");
            Check(state.ActiveEvent==gate.ResumeEvent && state.DeferredEvent==0,"original route resumed once");
            Check(engine.Now.ToString("HH:mm")== (gate.Id==1500003?"18:10":"17:57"),"original story clock preserved");
            Check(state.ConsumedActions.Count(x=>x==gate.Id)==1,"gate completion persisted once");
            Check(!engine.UseInteractionItem(300001)&&!engine.SelectInteractionRoom(100001),"duplicate input ignored");
            Check(state.Entries.Contains(300001),"invitation not consumed");
        }
        // 先操作、后结束对白：不能跳过源事件；关闭弹窗和回放不产生使用信号。
        var live=new V24State {ActiveEvent=920001,Now=new DateTime(2026,10,21,17,50,0).Ticks,DayBase=new DateTime(2026,10,21).Ticks};
        live.Entries.Add(300001);live.Shown.Add(520006);
        var liveEngine=new V24Engine(data,live);
        Check(liveEngine.UseInteractionItem(300001),"live-context item click recorded");
        Check(live.ActiveEvent==920001,"item click does not skip unfinished source dialogue");
        Check(live.InteractionSignals.Count==1,"one usage signal");
        var playback = new V24State();playback.Shown.Add(520006);playback.Entries.Add(300001);
        Check(!new V24Engine(data,playback).UseInteractionItem(300001),"playback cannot use scene gate");
        live.InteractionSignals=null;
        Check(liveEngine.UseInteractionItem(300001),"old save without new list accepts interaction");
        Check(live.InteractionSignals.Count==1,"old save initializes interaction list once");
        var names=new V24Engine(data,new V24State());
        names.NewGame();
        Check(!names.PersonLinkResolved("entry:320200"),"locked name keeps unlock link");
        names.Grant(320200,"verify-name",false);
        Check(names.PersonLinkResolved("entry:320200"),"unlocked profile becomes plain text");
        Check(names.PersonLinkResolved("character:200002"),"character link also becomes plain text");
        Check(!names.PersonLinkResolved("room:100001"),"address remains clickable");
        var inspiration=data.Entry.First(x=>x.Type=="info"&&!names.IsCharacterProfile(x.Id));
        Check(!names.PersonLinkResolved("entry:"+inspiration.Id),"inspiration link remains collectible");
        var candidates=data.Puzzle.First(x=>x.Type=="timeline").Candidates;
        var original=(int[])candidates.Clone();
        for(int seed=0;seed<100;seed++)
        {
            var order=V24Engine.ShuffleTimelineClues(candidates,new Random(seed));
            Check(order.OrderBy(x=>x).SequenceEqual(candidates.OrderBy(x=>x)),"shuffle preserves candidate IDs");
            Check(!order.SequenceEqual(candidates),"shuffle never exposes source answer order");
        }
        Check(original.SequenceEqual(candidates),"shuffle leaves answer/candidate source intact");
        var playbackEngine=new V24Engine(data,new V24State());playbackEngine.NewGame();
        playbackEngine.State.Room=data.GameSettings.ControlRoom;
        playbackEngine.State.PlaybackGate=920016;playbackEngine.State.LastCompletedEvent=920016;
        Check(playbackEngine.ClosePlaybackGate(),"first control-room recording can close early");
        Check(playbackEngine.State.PlaybackGate==0,"early exit clears playback gate");
        Check(playbackEngine.State.ActiveEvent==920017||playbackEngine.State.DeferredEvent==920017,"early exit retains next event");
        int transcriptCount=playbackEngine.State.Transcript.Count;
        Check(playbackEngine.ClosePlaybackGate()&&playbackEngine.State.Transcript.Count==transcriptCount,"early exit continuation is not repeated");
        var gatePolicy=data.PlaybackGates.First(x=>x.Event==920016);
        gatePolicy.MustFinish=true;gatePolicy.AllowEarlyExit=false;
        var requiredEngine=new V24Engine(data,new V24State{PlaybackGate=920016});
        Check(!requiredEngine.ClosePlaybackGate(),"explicit mandatory gates still enforced");
        Console.WriteLine("PASS: "+_checks+" interaction checks (actual V24Engine; isolated state, no player save).");
    }
}
