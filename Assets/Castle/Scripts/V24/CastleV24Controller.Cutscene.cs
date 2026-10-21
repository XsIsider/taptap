using System.Collections;
using UnityEngine;
namespace Castle.V24
{
    public sealed partial class CastleV24Controller
    {
        private bool _sleepTransition;
        private float _endCardReady;
        private void Cutscene()
        {
            var view=V("Cutscene");
            Txt(view,"Body",S.ActiveEvent!=0?_data.Lines(_data.EventById(S.ActiveEvent).Plot)[S.Line].Text:"");
            Btn(view,"Surface",()=>{if(S.ActiveEvent!=0){Engine.Advance();Refresh();}},enabled:S.ActiveEvent!=0);
            if(S.ActiveEvent==0&&!_sleepTransition)StartCoroutine(SleepBlackout());
        }
        private IEnumerator SleepBlackout()
        {
            _sleepTransition=true;
            for(float elapsed=0;elapsed<.6f;)
            { if(_page=="Cutscene"&&_modal.Count==0)elapsed+=Time.unscaledDeltaTime;yield return null; }
            Engine.ResumeSleep();_sleepTransition=false;Refresh();
        }
        private void EndCard()
        {
            _endCardReady=Time.unscaledTime+.4f;
            var view=V("EndCard");Txt(view,"Body",_data.FinaleConfig.EndCard.Text);
            Btn(view,"Surface",()=>{if(Time.unscaledTime>=_endCardReady){CloseAll();Page("Title");}});
        }
    }
}
