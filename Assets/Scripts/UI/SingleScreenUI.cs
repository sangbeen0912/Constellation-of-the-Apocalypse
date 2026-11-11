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
            int total = Math.Max(1,count);if(total<=1)return;
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
        void RenderFacilityScreen(Transform body){RenderShelterWorkspace(body);}
        Toggle ItemCheck(Transform parent,string label,bool value,bool available,Action<bool> change)
        {
            var line=Row(parent,56);line.name="Item checkbox "+label;
            var layout=line.GetComponent<HorizontalLayoutGroup>();layout.childForceExpandWidth=false;layout.childForceExpandHeight=false;layout.childAlignment=TextAnchor.MiddleLeft;
            var toggle=line.gameObject.AddComponent<Toggle>();toggle.interactable=available;
            var frame=Box(line,"Checkbox",Vector2.zero,Vector2.one,new Color(.12f,.16f,.18f));
            frame.GetComponent<Image>().color=Color.clear;
            var checkbox=Plate(frame,new Color(.09f,.12f,.17f));checkbox.customRim=true;checkbox.rimColor=available?ink:muted;
            var le=frame.gameObject.AddComponent<LayoutElement>();le.minWidth=28;le.preferredWidth=28;le.minHeight=28;le.preferredHeight=28;le.flexibleWidth=0;
            var mark=Positioned(frame,"✓",Vector2.zero,Vector2.one,25,gold);
            toggle.targetGraphic=frame.GetComponent<Image>();toggle.graphic=mark;toggle.SetIsOnWithoutNotify(value);
            Label(line,label,21,56,available?ink:muted).GetComponent<LayoutElement>().flexibleWidth=1;
            toggle.onValueChanged.AddListener(on=>{change(on);Render();});return toggle;
        }
        void RenderClinicScreen(Transform body)
        {
            var s=Game.S;if(s.medicine==0)healthItem=false;if(s.syringes==0)mentalItem=false;
            Positioned(body,"남은 "+(Rules.DayHours-s.hours).ToString("0.0")+"시간  ·  키트 "+s.medicine+"  ·  주사기 "+s.syringes,new Vector2(.015f,.91f),new Vector2(.99f,.99f),20,muted);
            for(int i=0;i<3;i++)
            {
                bool mental=i==1,sleep=i==2,item=i==0?healthItem:i==1&&mentalItem;
                string title=sleep?"잠자기":mental?"정신력 회복":"체력 회복";float x=i/3f;
                var card=WorkPanel(body,"Clinic "+title,new Vector2(x+.006f,.02f),new Vector2(x+.327f,.90f));
                Icon(card,sleep?7:mental?3:2,new Vector2(.06f,.83f),new Vector2(.19f,.95f),ink);
                Positioned(card,title,new Vector2(.22f,.82f),new Vector2(.96f,.97f),25,ink).fontStyle=FontStyle.Bold;
                var p=Game.PreviewTreatment(mental||sleep,sleep,item);
                var time=Box(card,"Treatment duration",new Vector2(.06f,.68f),new Vector2(.94f,.81f),new Color(.17f,.21f,.29f));
                Positioned(time,p.hours+"시간",Vector2.zero,Vector2.one,27,ink).alignment=TextAnchor.MiddleCenter;
                Positioned(card,"체력  "+Rules.Round(s.avatar.health)+" → "+p.healthAfter+"\n정신력  "+Rules.Round(s.avatar.sanity)+" → "+p.sanityAfter,new Vector2(.07f,.40f),new Vector2(.94f,.65f),23,ink);
                if(!sleep)ItemCheck(ScreenArea(card,"Treatment item",new Vector2(.07f,.24f),new Vector2(.94f,.37f)),i==0?"응급 의료 키트 사용":"정신 회복 주사기 사용",item,i==0?s.medicine>0:s.syringes>0,on=>{if(mental)mentalItem=on;else healthItem=on;});
                else Positioned(card,"두 상태를 함께 회복",new Vector2(.07f,.24f),new Vector2(.94f,.35f),22,muted);
                if(!p.available)Positioned(card,p.reason,new Vector2(.07f,.14f),new Vector2(.94f,.23f),18,muted);
                var action=PlacedButton(card,title+" 실행",new Vector2(.06f,.025f),new Vector2(.94f,.18f),()=>Act(()=>Game.Treat(mental||sleep,sleep,item)),p.available,true);
                action.GetComponentInChildren<Text>().fontSize=22;
            }
        }
        void RenderChapelScreen(Transform body)
        {
            var tabs=Row(Box(body,"Chapel sections",new Vector2(0,.90f),Vector2.one,Color.clear),58);
            foreach(string name in new[]{"후원","보급품","성유물 복원"}){string selected=name;WorkspaceTab(tabs,name,chapelSection==name,()=>{chapelSection=selected;Render();});}
            var area=ScreenArea(body,"Chapel panel",Vector2.zero,new Vector2(1,.87f));var s=Game.S;
            if(chapelSection=="보급품")
            {
                var card=Stack(area,16);Label(card,"쉘터 보급 요청",29,50,gold);Label(card,"한 번당 신위 1 · 결과는 각각 무작위",24,42);
                Label(card,"보유 신위 "+s.divinity+" · 식량 "+s.food+" · 의료 키트 "+s.medicine,23,42,muted);
                var items=Row(card,70);foreach(var supply in content.supplies)Label(items,supply.name,23,60,ink);
                var actions=Row(card,72);MakeButton(actions,"무작위 보급품 받기 · 신위 1",()=>Act(()=>Game.SupplyBatch(1)),s.phase==Phase.Morning&&s.divinity>=Rules.SupplyCost,true);
                MakeButton(actions,"5회 보급 · 신위 5",()=>Act(()=>Game.SupplyBatch(5)),s.phase==Phase.Morning&&s.divinity>=Rules.SupplyCost*5,true);
                foreach(string line in s.logs.Where(l=>l.Contains("무작위 보급")).Reverse().Take(5))Label(card,line,21,32,muted);return;
            }
            if(chapelSection=="성유물 복원")
            {
                if(restorationId!=null){RenderRestoration(Stack(area,12));return;}
                var waiting=s.relics.Where(r=>r.awaitingRestoration).ToList();restorePage=Math.Max(0,Math.Min(restorePage,Math.Max(0,(waiting.Count-1)/2)));
                for(int i=0;i<2;i++)
                {
                    var relic=waiting.Skip(restorePage*2+i).FirstOrDefault();if(relic==null)break;string id=relic.id;float y=.53f-i*.45f;
                    var card=PrepCard(area,"Waiting relic "+id,new Vector2(.01f,y),new Vector2(.99f,y+.43f));
                    RelicCard(card,relic);MakeButton(card,"복원 기록 보기 · "+relic.name,()=>{restorationId=id;restorationStep=0;Render();});
                }
                if(waiting.Count==0)Positioned(area,"복원을 기다리는 성유물이 없습니다.",new Vector2(.04f,.4f),new Vector2(.96f,.7f),26,muted);
                PageBar(PageFooter(area),restorePage,(waiting.Count+1)/2,p=>{restorePage=p;Render();});return;
            }
            var quantity=Row(Box(area,"Upgrade quantity",new Vector2(.01f,.86f),new Vector2(.99f,1),Color.clear),62);
            Label(quantity,"강화 횟수",23,54,muted);
            foreach(int n in new[]{1,5,10}){int amount=n;WorkspaceTab(quantity,"×"+n,upgradeQuantity==n,()=>{upgradeQuantity=amount;Render();});}
            int index=0;
            foreach(Stat stat in new[]{Stat.Health,Stat.Attack})
            {
                var chosen=stat;float x=index++*.5f;var card=WorkPanel(area,"Upgrade "+stat,new Vector2(x+.01f,.43f),new Vector2(x+.49f,.83f));
                int count=Game.UpgradeCount(stat),cost=Game.UpgradeBatchCost(stat,upgradeQuantity);
                Positioned(card,stat==Stat.Health?"최대 체력":"공격력",new Vector2(.05f,.78f),new Vector2(.96f,.97f),28,ink);
                Positioned(card,count+"회 강화 · 다음 1회 신위 "+Game.UpgradeCost(stat),new Vector2(.05f,.61f),new Vector2(.96f,.78f),21,muted);
                int current=stat==Stat.Health?s.avatar.MaxHealth:s.avatar.Attack;
                int next=stat==Stat.Health?Rules.Round(s.avatar.baseHealth*(1+(s.avatar.healthInvestment+upgradeQuantity*10)/100.0))+s.avatar.relicHealthBonus:
                    Rules.Round(s.avatar.baseAttack*(1+(s.avatar.attackInvestment+upgradeQuantity*10)/100.0))+s.avatar.relicAttackBonus;
                Positioned(card,current+" → "+next+"\n합계 신위 "+cost,new Vector2(.05f,.34f),new Vector2(.96f,.61f),23,gold);
                string label=upgradeQuantity==1?(stat==Stat.Health?"체력":"공격력")+" 강화 +10% · 신위 "+cost:(stat==Stat.Health?"체력":"공격력")+" "+upgradeQuantity+"회 강화 · 신위 "+cost;
                PlacedButton(card,label,new Vector2(.04f,.035f),new Vector2(.96f,.32f),()=>Act(()=>Game.UpgradeBatch(chosen,upgradeQuantity)),s.phase==Phase.Morning&&s.divinity>=cost,true);
            }
            var owned=content.constellations.Where(c=>s.permanent.unlocked.Contains(c.id)).ToList();
            var inherit=Box(area,"Inheritance",new Vector2(.01f,.01f),new Vector2(.99f,.40f),Color.clear);
            Positioned(inherit,"성좌의 계승",new Vector2(.01f,.78f),new Vector2(.98f,1),25,gold);
            if(owned.Count==0){Positioned(inherit,"획득한 성좌가 생기면 이곳에서 계승합니다.",new Vector2(.01f,.30f),new Vector2(.98f,.70f),23,muted);return;}
            var list=Scroller(ScreenArea(inherit,"Inheritance viewport",new Vector2(0,.01f),new Vector2(1,.77f)));
            foreach(var c in owned){string id=c.id;int cost=Rules.InheritCosts[c.grade];MakeButton(list,c.name+" 계승 · "+cost+(s.avatar.Has(id)?" · 계승 완료":""),()=>Act(()=>Game.Inherit(id)),s.phase==Phase.Morning&&!s.avatar.Has(id)&&s.divinity>=cost);}
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
            if(!selectable){RenderPersonnelRows(body);return;}
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
            var catalog=WorkPanel(body,"Codex catalogue",new Vector2(.015f,.02f),new Vector2(.30f,.985f));
            Positioned(catalog,"성좌 도감",new Vector2(.05f,.92f),new Vector2(.97f,.99f),29,ink);
            for(int g=0;g<4;g++)
            {
                int grade=g,count=codex.Entries(content,p,g).Count();float x=g%2*.5f,y=g<2?.83f:.74f;
                var b=PlacedButton(catalog,(codex.IsExpanded(g)?"− ":"+ ")+Rules.Grades[g]+" "+count,new Vector2(x+.025f,y),new Vector2(x+.475f,y+.08f),()=>{codex.Toggle(grade);Render();},count>0);
                b.GetComponentInChildren<Text>().fontSize=21;
            }
            var list=Scroller(ScreenArea(catalog,"Codex entries viewport",new Vector2(.025f,.02f),new Vector2(.975f,.72f)));
            var entries=Enumerable.Range(0,4).Where(g=>codex.IsExpanded(g)).SelectMany(g=>codex.Entries(content,p,g)).ToList();
            foreach(var c in entries)
            {
                string id=c.id;var b=MakeButton(list,CodexIndex.DisplayName(c),()=>{codex.Select(id,p);Render();},true,codex.SelectedId==id);
                b.GetComponent<LayoutElement>().preferredHeight=80;b.GetComponentInChildren<Text>().fontSize=21;
            }
            if(p.unlocked.Count==0)Label(list,"아직 기록된 성좌가 없습니다.",24,100,muted);
            var detail=ScreenArea(body,"Codex detail",new Vector2(.32f,.02f),new Vector2(.985f,.985f));
            if(codex.SelectedId==null||!p.unlocked.Contains(codex.SelectedId))
            {Positioned(detail,"성좌를 선택하면\n능력과 기록을 펼쳐 봅니다.",new Vector2(.10f,.35f),new Vector2(.9f,.65f),30,muted);return;}
            var star=content.Find(codex.SelectedId);
            Positioned(detail,CodexIndex.DisplayName(star),new Vector2(.015f,.88f),new Vector2(.99f,.995f),30,gold);
            Positioned(detail,"소속  "+star.affiliation,new Vector2(.015f,.81f),new Vector2(.99f,.88f),24,muted);
            var texture=constellationArt.Use(star.id);constellationArtUsed=texture!=null;
            var art=ScreenArea(detail,"Codex illustration",new Vector2(.015f,.33f),new Vector2(.54f,.80f));FixedArt(art,texture,Vector2.zero,Vector2.one);
            foreach(var fit in art.GetComponentsInChildren<AspectRatioFitter>())fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            var ability=Stack(ScreenArea(detail,"Codex ability",new Vector2(.56f,.33f),new Vector2(.995f,.80f)),8);
            Label(ability,star.ability,27,80,gold);Label(ability,star.description,24,225);
            var story=Scroller(ScreenArea(detail,"Codex story viewport",new Vector2(.015f,.015f),new Vector2(.99f,.30f)));
            var memory=p.stories.FirstOrDefault(x=>x.constellationId==star.id);Label(story,memory==null?star.title:memory.title,25,60,gold);
            var words=Label(story,memory==null?star.story:memory.body,24,0);words.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Math.Max(100,detail.rect.width-60));words.GetComponent<LayoutElement>().preferredHeight=Math.Max(180,words.preferredHeight+12);
        }
    }
}
