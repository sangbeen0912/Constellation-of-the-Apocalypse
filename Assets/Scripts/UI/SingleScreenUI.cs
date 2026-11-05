using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Apocalypse
{
    public sealed partial class GameApp
    {
        int codexPage, personnelPage, relicPage, inheritancePage, restorePage;
        string chapelSection = "후원";
        bool healthItem, mentalItem;
        Texture2D omenTexture;
        float omenSeconds;
        CanvasGroup omenWords;
        RectTransform omenStar;
        public bool OmenVisible { get { return Game != null && Game.S.phase == Phase.Running &&
            !string.IsNullOrEmpty(Game.S.scheduledConstellation) && Game.S.omenShownDay != Game.S.day; } }

        bool UpdateOmen()
        {
            if (!OmenVisible) return false;
            omenSeconds += Time.unscaledDeltaTime;
            if (omenWords != null) omenWords.alpha = Mathf.Clamp01(omenSeconds / .65f);
            if (omenStar != null) omenStar.localScale = Vector3.one * (1 + .08f * Mathf.Sin(omenSeconds * 2));
            if (omenSeconds >= 4) CloseOmen();
            return true;
        }
        void CloseOmen()
        {
            fieldMain=null;
            Game.S.omenShownDay = Game.S.day; omenSeconds = 0; clock = 0;
            ReleaseOmen(); Commit();
        }
        void ReleaseOmen()
        {
            if (omenTexture == null) return;
            if (canvas != null) foreach (var i in canvas.GetComponentsInChildren<RawImage>(true))
                if (i.texture == omenTexture) i.texture = null;
            Resources.UnloadAsset(omenTexture); omenTexture = null; omenWords = null; omenStar = null;
        }
        void RenderOmen(Transform parent)
        {
            if (!OmenVisible) { ReleaseOmen(); omenSeconds = 0; return; }
            if (omenTexture == null) omenTexture = Resources.Load<Texture2D>("Art/Environments/constellation-omen-v1");
            var screen = Box(parent,"Constellation watching scene",Vector2.zero,Vector2.one,bg);
            FixedArt(screen,omenTexture,Vector2.zero,Vector2.one);
            var words = Box(screen,"Omen words",new Vector2(.07f,.12f),new Vector2(.77f,.37f),new Color(.018f,.025f,.035f,.94f));
            omenWords = words.gameObject.AddComponent<CanvasGroup>(); omenWords.alpha = Mathf.Clamp01(omenSeconds/.65f);
            Positioned(words,"누군가의 시선",new Vector2(.035f,.68f),new Vector2(.97f,.93f),24,gold);
            Positioned(words,"한 성좌가 당신을 바라봅니다.",new Vector2(.035f,.25f),new Vector2(.97f,.72f),42,ink);
            Positioned(words,"별의 시선이 화신체의 발걸음을 따라갑니다.",new Vector2(.035f,.04f),new Vector2(.97f,.28f),23,muted);
            omenStar = Positioned(screen,"✦",new Vector2(.74f,.66f),new Vector2(.85f,.81f),64,gold).rectTransform;
            var advance = PrepCard(screen,"Omen continue",new Vector2(.79f,.12f),new Vector2(.95f,.20f));
            foreach(var button in parent.GetComponentsInChildren<Button>())button.interactable=false;
            var proceed=MakeButton(advance,"계속 진행",CloseOmen,true,true);
            proceed.GetComponent<LayoutElement>().preferredHeight=64;
            if(UnityEngine.EventSystems.EventSystem.current!=null)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(proceed.gameObject);
        }
        RectTransform ScreenArea(Transform parent,string name,Vector2 min,Vector2 max)
        { return Box(parent,name,min,max,Color.clear); }
        void PageBar(Transform parent,int page,int count,Action<int> change)
        {
            int total = Math.Max(1,count);
            var row = Row(parent,42);
            MakeButton(row,"이전 페이지",()=>change(page-1),page>0);
            Label(row,(page+1)+" / "+total,22,42,gold).alignment=TextAnchor.MiddleCenter;
            MakeButton(row,"다음 페이지",()=>change(page+1),page+1<total);
        }
        RectTransform PageFooter(Transform body)
        { return PrepCard(body,"Page controls",new Vector2(.015f,.005f),new Vector2(.985f,.085f)); }
        RectTransform FacilityBody(Transform body)
        {
            int index=Array.IndexOf(ArtCatalog.FacilityNames,tab);
            var header=PrepCard(body,"Facility navigation",new Vector2(.015f,.82f),new Vector2(.72f,.985f));
            Label(header,tab,31,42,gold);var nav=Row(header,42);
            foreach(string facility in ArtCatalog.FacilityNames){string dest=facility;MakeButton(nav,dest,()=>Navigate(dest),true,tab==dest);}
            FixedArt(body,Facility(index),new Vector2(.735f,.82f),new Vector2(.985f,.985f));
            return ScreenArea(body,"Facility content",new Vector2(.015f,.015f),new Vector2(.985f,.80f));
        }
        void RenderFacilityScreen(Transform body)
        {
            if(tab=="쉘터")
            {
                var s=Game.S;
                Positioned(body,s.phase==Phase.Report?"제7 쉘터  /  귀환":"제7 쉘터  /  새벽의 준비",new Vector2(.025f,.91f),new Vector2(.975f,.99f),34,gold);
                for(int i=0;i<4;i++)
                {
                    int index=i;string destination=ArtCatalog.FacilityNames[i];float y=.705f-i*.215f;
                    var card=Box(body,"Shelter "+destination,new Vector2(.02f,y),new Vector2(.62f,y+.185f),panel);
                    Plate(card,panel);FixedArt(card,Facility(index),new Vector2(.012f,.055f),new Vector2(.25f,.945f));
                    Positioned(card,"0"+(index+1)+"  "+destination,new Vector2(.28f,.50f),new Vector2(.97f,.87f),27,gold);
                    string detail=new[]{"회복 · 수면","보급 · 복원 · 후원","물자 · 성유물","화신체 · 생존자"}[index];
                    Positioned(card,detail,new Vector2(.28f,.17f),new Vector2(.62f,.48f),21,muted);
                    PlacedButton(card,"시설 이용 · "+destination,new Vector2(.64f,.13f),new Vector2(.98f,.45f),()=>Navigate(destination),s.phase==Phase.Morning);
                }
                var overview=Box(body,"Shelter overview",new Vector2(.645f,.06f),new Vector2(.98f,.89f),panel);Plate(overview,panel);
                FixedArt(overview,city,new Vector2(.025f,.49f),new Vector2(.975f,.98f));
                Positioned(overview,s.phase==Phase.Report?"귀환 기록":"오늘의 준비",new Vector2(.05f,.38f),new Vector2(.95f,.47f),28,gold);
                Positioned(overview,s.phase==Phase.Report?s.report:"시설에서 화신체를 정비하고\n오늘의 활동을 준비하십시오.",new Vector2(.05f,.13f),new Vector2(.95f,.37f),22,ink);
                if(s.phase==Phase.Report)PlacedButton(overview,"다음 날 아침",new Vector2(.05f,.035f),new Vector2(.95f,.115f),()=>Act(()=>Game.NextDay()),true,true);
                else PlacedButton(overview,"활동 준비",new Vector2(.05f,.035f),new Vector2(.95f,.115f),()=>Navigate("전투준비"),true,true);
                return;
            }
            var area=FacilityBody(body);
            if(tab=="의무실")RenderClinicScreen(area);
            else if(tab=="예배방")RenderChapelScreen(area);
            else if(tab=="보급창고")RenderWarehouseScreen(area);
            else RenderPersonnelScreen(area,false);
        }
        Toggle ItemCheck(Transform parent,string label,bool value,bool available,Action<bool> change)
        {
            var line=Row(parent,56);line.name="Item checkbox "+label;
            var layout=line.GetComponent<HorizontalLayoutGroup>();layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;layout.childAlignment=TextAnchor.MiddleLeft;
            var toggle=line.gameObject.AddComponent<Toggle>();toggle.interactable=available;
            var frame=Box(line,"Checkbox",Vector2.zero,Vector2.one,new Color(.12f,.16f,.18f));
            var le=frame.gameObject.AddComponent<LayoutElement>();le.minWidth=28;le.preferredWidth=28;le.minHeight=28;le.preferredHeight=28;le.flexibleWidth=0;
            var mark=Positioned(frame,"✓",Vector2.zero,Vector2.one,25,gold);
            toggle.targetGraphic=frame.GetComponent<Image>();toggle.graphic=mark;toggle.SetIsOnWithoutNotify(value);
            Label(line,label,21,56,available?ink:muted).GetComponent<LayoutElement>().flexibleWidth=1;
            toggle.onValueChanged.AddListener(on=>{change(on);Render();});return toggle;
        }
        void RenderClinicScreen(Transform body)
        {
            var s=Game.S;
            Positioned(body,"남은 시간 "+(Rules.DayHours-s.hours).ToString("0.0")+"시간   ·   의료 키트 "+s.medicine+"   ·   주사기 "+s.syringes,new Vector2(.01f,.91f),new Vector2(.99f,1),23,muted);
            if(s.medicine==0)healthItem=false;if(s.syringes==0)mentalItem=false;
            for(int i=0;i<3;i++)
            {
                bool mental=i==1,sleep=i==2,item=i==0?healthItem:i==1&&mentalItem;
                string title=i==0?"체력 회복":i==1?"정신력 회복":"잠자기";
                float y=.61f-i*.29f;var card=Box(body,"Clinic "+title,new Vector2(.01f,y),new Vector2(.99f,y+.26f),panel);Plate(card,panel);
                var p=Game.PreviewTreatment(mental||sleep,sleep,item);
                Positioned(card,title,new Vector2(.025f,.52f),new Vector2(.19f,.86f),28,gold);
                Positioned(card,p.hours+"시간",new Vector2(.025f,.16f),new Vector2(.19f,.48f),23,muted);
                Positioned(card,"체력  "+Rules.Round(s.avatar.health)+" → "+p.healthAfter+"\n정신력  "+Rules.Round(s.avatar.sanity)+" → "+p.sanityAfter,new Vector2(.21f,.40f),new Vector2(.46f,.88f),23,ink);
                Positioned(card,p.available?"회복 +"+p.health+" / +"+p.sanity:p.reason,new Vector2(.21f,.10f),new Vector2(.46f,.38f),21,p.available?muted:ink);
                var check=ScreenArea(card,"Treatment item",new Vector2(.48f,.23f),new Vector2(.77f,.77f));
                if(!sleep)ItemCheck(check,i==0?"응급 의료 키트 사용":"정신 회복 주사기 사용",item,i==0?s.medicine>0:s.syringes>0,on=>{if(mental)mentalItem=on;else healthItem=on;});
                else Positioned(check,"깊은 수면",Vector2.zero,Vector2.one,23,muted);
                PlacedButton(card,title+" 실행",new Vector2(.79f,.32f),new Vector2(.975f,.68f),()=>Act(()=>Game.Treat(mental||sleep,sleep,item)),p.available,true);
            }
        }
        void RenderChapelScreen(Transform body)
        {
            var tabs=PrepCard(body,"Chapel sections",new Vector2(0,.89f),Vector2.one);var row=Row(tabs,42);
            foreach(string name in new[]{"보급품","성유물 복원","후원"}){string section=name;MakeButton(row,name,()=>{chapelSection=section;Render();},true,name==chapelSection);}
            var area=ScreenArea(body,"Chapel panel",Vector2.zero,new Vector2(1,.87f));var s=Game.S;
            if(chapelSection=="보급품")
            {
                var card=PrepCard(area,"Supply request",new Vector2(.03f,.05f),new Vector2(.97f,.97f));
                Label(card,"쉘터 보급 요청",32,55,gold);Label(card,"보유 신위  "+s.divinity,25,45);
                var options=Row(card,150);
                foreach(var supply in content.supplies){var cell=Stack(options,10);Label(cell,supply.name,26,65,gold);}
                MakeButton(card,"무작위 보급품 받기 · 신위 1",()=>Act(()=>Game.Supply()),s.phase==Phase.Morning&&s.divinity>=Rules.SupplyCost,true);
                return;
            }
            if(chapelSection=="성유물 복원")
            {
                if(restorationId!=null){RenderRestoration(PrepCard(area,"Restoration",new Vector2(.015f,.01f),new Vector2(.985f,.99f)));return;}
                var waiting=s.relics.Where(r=>r.awaitingRestoration).ToList();restorePage=Math.Max(0,Math.Min(restorePage,Math.Max(0,(waiting.Count-1)/2)));
                for(int i=0;i<2;i++)
                {
                    var relic=waiting.Skip(restorePage*2+i).FirstOrDefault();if(relic==null)break;string id=relic.id;
                    var card=PrepCard(area,"Waiting relic "+id,new Vector2(i*.5f+.01f,.13f),new Vector2(i*.5f+.49f,.98f));
                    RelicCard(card,relic);Label(card,"계승자의 기록을 확인하고\n성유물을 복원합니다.",24,120,muted);
                    MakeButton(card,"복원 기록 보기 · "+relic.name,()=>{restorationId=id;restorationStep=0;Render();});
                }
                if(waiting.Count==0)Positioned(area,"복원을 기다리는 성유물이 없습니다.",new Vector2(.04f,.4f),new Vector2(.96f,.7f),26,muted);
                PageBar(PageFooter(area),restorePage,(waiting.Count+1)/2,p=>{restorePage=p;Render();});return;
            }
            var invest=PrepCard(area,"Investments",new Vector2(.01f,.46f),new Vector2(.41f,.98f));
            FixedArt(area,Facility(1),new Vector2(.01f,.02f),new Vector2(.41f,.43f));
            Label(invest,"화신체 후원",31,55,gold);Label(invest,"보유 신위  "+s.divinity,26,50);
            foreach(var stat in new[]{Stat.Health,Stat.Attack})
            {
                var chosen=stat;int cost=Game.UpgradeCost(stat);int count=Game.UpgradeCount(stat);
                Label(invest,(stat==Stat.Health?"최대 체력":"공격력")+" · "+count+"회 강화 / 기본값 +"+(stat==Stat.Health?s.avatar.healthInvestment:s.avatar.attackInvestment)+"%",21,36,muted);
                MakeButton(invest,(stat==Stat.Health?"체력":"공격력")+" 강화 +10% · 신위 "+cost,()=>Act(()=>Game.Invest(chosen,Game.UpgradeCost(chosen))),s.phase==Phase.Morning&&s.divinity>=cost);
            }
            var owned=content.constellations.Where(c=>s.permanent.unlocked.Contains(c.id)).ToList();
            inheritancePage=Math.Max(0,Math.Min(inheritancePage,Math.Max(0,(owned.Count-1)/6)));
            var inherit=PrepCard(area,"Inheritance",new Vector2(.43f,.13f),new Vector2(.99f,.98f));Label(inherit,"성좌의 계승",29,46,gold);
            foreach(var c in owned.Skip(inheritancePage*6).Take(6)){string id=c.id;int cost=Rules.InheritCosts[c.grade];MakeButton(inherit,c.name+" 계승 · "+cost+(s.avatar.Has(id)?" · 계승 완료":""),()=>Act(()=>Game.Inherit(id)),s.phase==Phase.Morning&&!s.avatar.Has(id)&&s.divinity>=cost);}
            if(owned.Count==0)Label(inherit,"기록된 성좌가 없습니다.",23,80,muted);
            var controls=PrepCard(area,"Inheritance pages",new Vector2(.43f,.01f),new Vector2(.99f,.12f));
            PageBar(controls,inheritancePage,(owned.Count+5)/6,p=>{inheritancePage=p;Render();});
        }
        void RenderWarehouseScreen(Transform body)
        {
            var s=Game.S;
            if(historyId!=null)
            {
                var selected=s.relics.FirstOrDefault(r=>r.id==historyId);if(selected==null){historyId=null;Render();return;}
                var pane=PrepCard(body,"Relic history",new Vector2(.01f,.12f),new Vector2(.99f,.99f));
                RelicCard(pane,selected);
                Label(pane,"전체 기록  생존 "+selected.days+"일 / 좀비 "+selected.Zombies+" / 악마 "+selected.demonKills,24,52);
                Label(pane,"이월 기록  생존 "+(selected.days-selected.paidDays)+"일 / 좀비 "+(selected.Zombies-selected.paidZombies)+"마리",23,50,muted);
                historyChapter=Math.Max(0,Math.Min(historyChapter,Math.Max(0,selected.history.Count-1)));
                if(selected.history.Count>0){Label(pane,"제"+(historyChapter+1)+"장",26,40,gold);Label(pane,ChapterText(selected.history[historyChapter]),24,105);}
                PageBar(PageFooter(body),historyChapter,selected.history.Count,p=>{historyChapter=p;Render();});return;
            }
            Positioned(body,"식량 "+s.food+"  ·  의료 키트 "+s.medicine+"  ·  주사기 "+s.syringes+"  /  성유물 최대 1개 장착",new Vector2(.01f,.9f),Vector2.one,23,muted);
            relicPage=Math.Max(0,Math.Min(relicPage,Math.Max(0,(s.relics.Count-1)/2)));
            for(int i=0;i<2;i++)
            {
                var relic=s.relics.Skip(relicPage*2+i).FirstOrDefault();if(relic==null)break;string id=relic.id;
                var card=PrepCard(body,"Stored relic "+id,new Vector2(i*.5f+.01f,.12f),new Vector2(i*.5f+.49f,.88f));
                RelicCard(card,relic);Label(card,content.relicDefinitions.First(d=>d.id==id).description,22,100,muted);
                MakeButton(card,relic.name+(s.avatar.relics.Contains(id)?" · 장착 해제":" · 장착"),()=>Act(()=>Game.Equip(id)),s.phase==Phase.Morning&&!relic.awaitingRestoration);
                MakeButton(card,"전승 역사 · "+relic.name,()=>{historyId=id;historyChapter=0;Render();});
            }
            if(s.relics.Count==0)Positioned(body,"탐사와 공략의 사건에서 성유물을 찾을 수 있습니다.",new Vector2(.04f,.4f),new Vector2(.96f,.65f),26,muted);
            PageBar(PageFooter(body),relicPage,(s.relics.Count+1)/2,p=>{relicPage=p;Render();});
        }
        void PersonnelTile(Transform parent,Character c,bool current,bool selectable)
        {
            parent.name="Personnel "+c.id;var card=Stack(parent,10);card.GetComponent<VerticalLayoutGroup>().spacing=4;
            Label(card,c.name+(current?"  [화신체]":""),27,36,gold);
            var row=Row(card,126);row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth=false;var portrait=ScreenArea(row,"Portrait cell",Vector2.zero,Vector2.one);
            var le=portrait.gameObject.AddComponent<LayoutElement>();le.minWidth=120;le.preferredWidth=120;le.flexibleWidth=0;Art(Stack(portrait,0),Portrait(c),120,true);
            var stats=Stack(row,4);stats.gameObject.AddComponent<LayoutElement>().flexibleWidth=1;stats.GetComponent<VerticalLayoutGroup>().spacing=2;
            Label(stats,Personality(c),21,28,muted);Label(stats,"체력 "+Rules.Round(c.health)+"/"+c.MaxHealth,22,28);Label(stats,"공격력 "+c.Attack,22,28);
            Label(stats,"정신력 "+Rules.Round(c.sanity)+" · 허기 "+Rules.Round(c.hunger),20,28);
            if(c.species!=Species.Human)Label(card,Passive(c),21,70,gold);
            if(selectable){string id=c.id;MakeButton(card,"계약한다",()=>Act(()=>Game.Select(id)),true,true);}
        }
        void RenderPersonnelScreen(Transform body,bool selectable)
        {
            var people=selectable?Game.S.candidates.ToList():new[]{Game.S.avatar}.Concat(Game.S.candidates).Where(c=>c!=null).ToList();
            int size=selectable?3:4;personnelPage=Math.Max(0,Math.Min(personnelPage,Math.Max(0,(people.Count-1)/size)));
            for(int i=0;i<size;i++)
            {
                var c=people.Skip(personnelPage*size+i).FirstOrDefault();if(c==null)break;
                Vector2 min,max;
                if(selectable){min=new Vector2(i/3f+.007f,.13f);max=new Vector2((i+1)/3f-.007f,.98f);}
                else {float x=i%2*.5f,y=i/2==0?.51f:.12f;min=new Vector2(x+.007f,y);max=new Vector2(x+.493f,y+.37f);}
                PersonnelTile(Box(body,"Personnel",min,max,new Color(.065f,.085f,.10f)),c,c==Game.S.avatar,selectable);
            }
            PageBar(PageFooter(body),personnelPage,(people.Count+size-1)/size,p=>{personnelPage=p;Render();});
        }
        void RenderSelectionScreen(Transform body)
        {
            Positioned(body,Game.S.phase==Phase.Death?"계약의 끝, 다음 별의 시작":"첫 화신체를 선택하십시오",new Vector2(.025f,.9f),new Vector2(.975f,.99f),34,gold);
            if(Game.S.phase==Phase.Death)Positioned(body,Game.S.lastDeath,new Vector2(.025f,.79f),new Vector2(.975f,.9f),22,muted);
            var area=ScreenArea(body,"Candidates",Vector2.zero,new Vector2(1,Game.S.phase==Phase.Death?.78f:.89f));
            RenderPersonnelScreen(area,true);
        }
        void RenderEventScreen(Transform body)
        {
            var e=Game.S.pending;bool ce=!string.IsNullOrEmpty(e.constellationId);
            var art=PrepCard(body,"Event illustration",new Vector2(.015f,.025f),new Vector2(.47f,.985f));
            if(ce)ConstellationArt(art,e.constellationId,420,280);else EventArtwork(art,e);
            if(ce){var c=content.Find(e.constellationId);Label(art,c.ability,28,58,gold);Label(art,c.description,23,100);Label(art,"소속  "+c.affiliation,22,45,muted);}
            var story=PrepCard(body,"Event story",new Vector2(.485f,.025f),new Vector2(.985f,.985f));
            Label(story,ce?"별의 기억  /  "+Rules.Grades[content.Find(e.constellationId).grade]:"운명의 갈림길",22,35,gold);
            Label(story,e.title,29,70,gold);if(!string.IsNullOrEmpty(e.context))Label(story,e.context,21,42,muted);Label(story,e.body,24,215);
            for(int i=0;i<e.choices.Length;i++)
            {
                int choice=i;string id=e.id;bool possible=Game.CanChoose(e,i,out string reason);string cost=OptionCost(e,i);
                var optionButton=MakeButton(story,e.choices[i]+(cost.Length>0?"  · "+cost:""),()=>ResolveEventChoice(id,choice),possible&&!Game.S.controlLocked,true);
                optionButton.GetComponent<LayoutElement>().preferredHeight=88;optionButton.GetComponentInChildren<Text>().fontSize=22;
                if(!possible)Label(story,reason,20,28,muted);
            }
            RenderTransitExit(story);
        }
        void RenderCodexScreen(Transform body)
        {
            var p=Game.S.permanent;
            if(codex.SelectedId!=null&&p.unlocked.Contains(codex.SelectedId))
            {
                var c=content.Find(codex.SelectedId);
                var art=PrepCard(body,"Codex illustration",new Vector2(.015f,.025f),new Vector2(.49f,.985f));
                ConstellationArt(art,c.id,460,280);
                Label(art,"소속  "+c.affiliation,24,48,muted);
                var detail=PrepCard(body,"Codex memory",new Vector2(.505f,.025f),new Vector2(.985f,.985f));
                Label(detail,CodexIndex.DisplayName(c),29,90,gold);Label(detail,c.ability,28,60,gold);Label(detail,c.description,24,105);
                var memory=p.stories.FirstOrDefault(x=>x.constellationId==c.id);
                Label(detail,memory==null?c.title:memory.title,26,70,gold);Label(detail,memory==null?c.story:memory.body,24,340);return;
            }
            var list=PrepCard(body,"Owned constellations",new Vector2(.015f,.12f),new Vector2(.985f,.985f));
            Label(list,"성좌 도감",34,58,gold);var grades=Row(list,48);
            for(int g=0;g<4;g++){int grade=g;int count=codex.Entries(content,p,g).Count();MakeButton(grades,(codex.IsExpanded(g)?"▾  ":"▸  ")+Rules.Grades[g]+"  "+count,()=>{codex.Toggle(grade);codexPage=0;Render();},count>0,true);}
            var entries=Enumerable.Range(0,4).Where(g=>codex.IsExpanded(g)).SelectMany(g=>codex.Entries(content,p,g)).ToList();
            codexPage=Math.Max(0,Math.Min(codexPage,Math.Max(0,(entries.Count-1)/6)));
            foreach(var c in entries.Skip(codexPage*6).Take(6)){string id=c.id;MakeButton(list,CodexIndex.DisplayName(c),()=>{codex.Select(id,p);Render();});}
            if(p.unlocked.Count==0)Label(list,"아직 기록된 성좌가 없습니다.",25,80,muted);
            PageBar(PageFooter(body),codexPage,(entries.Count+5)/6,page=>{codexPage=page;Render();});
        }
    }
}
