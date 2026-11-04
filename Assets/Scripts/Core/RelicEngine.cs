using System;
using System.Linq;
namespace Apocalypse
{
    public sealed partial class GameEngine
    {
        string rewardRelicId, rewardRareId;
        public bool AcquireRelic(string id)
        {
            var definition=Content.relicDefinitions.FirstOrDefault(r=>r.id==id);
            if(rewardRelicId!=id||definition==null||definition.location!=S.goal||S.avatar==null||S.phase==Phase.Death||S.relics.Any(r=>r.id==id))return false;
            S.relics.Add(new Relic{id=id,name=definition.name});Log(definition.name+" 획득 · 전승 기록은 아직 비어 있다.");return true;
        }
        public bool Equip(string id)
        {
            if(S.phase!=Phase.Morning||S.avatar==null)return false;
            var relic=S.relics.FirstOrDefault(r=>r.id==id);
            if(relic==null||relic.awaitingRestoration)return false;
            var a=S.avatar;
            if(a.relics.Contains(id))
            {
                a.relics.Clear();relic.currentUserId=null;SyncRelicStats();SavePoint();return true;
            }
            foreach(var old in S.relics.Where(r=>a.relics.Contains(r.id)))old.currentUserId=null;
            a.relics.Clear();a.relics.Add(id);relic.currentUserId=a.id;
            if(!relic.history.Any(h=>h.userId==a.id))
                relic.history.Add(new RelicChapter{userId=a.id,name=a.name,startDay=S.day});
            SyncRelicStats();Log(relic.name+" · "+a.name+"의 전승이 시작되었다.");SavePoint();return true;
        }
        void SyncRelicStats()
        {
            var a=S.avatar;if(a==null)return;
            a.relicHealthBonus=a.relicAttackBonus=0;
            var equipped=S.relics.FirstOrDefault(r=>a.relics.Contains(r.id)&&!r.awaitingRestoration);
            a.relics.Clear();
            if(equipped!=null){a.relics.Add(equipped.id);a.relicHealthBonus=equipped.healthBonus;a.relicAttackBonus=equipped.attackBonus;equipped.currentUserId=a.id;}
            a.health=Rules.Clamp(a.health,a.MaxHealth);
        }
        Relic WornRelic()
        {
            var a=S.avatar;return a==null?null:S.relics.FirstOrDefault(r=>a.relics.Contains(r.id)&&!r.awaitingRestoration);
        }
        void CreditRelicDay()
        {
            var relic=WornRelic();if(relic==null)return;
            var chapter=relic.history.FirstOrDefault(h=>h.userId==S.avatar.id);
            if(chapter==null||chapter.lastCountedDay==S.day)return;
            chapter.lastCountedDay=S.day;chapter.days++;chapter.endDay=S.day;relic.days++;
        }
        void RecordRelicKill(EnemyKind kind)
        {
            var relic=WornRelic();if(relic==null)return;
            var chapter=relic.history.First(h=>h.userId==S.avatar.id);
            if(kind==EnemyKind.Normal){relic.normalKills++;chapter.normalKills++;}
            else if(kind==EnemyKind.Special){relic.specialKills++;chapter.specialKills++;}
            else if(kind==EnemyKind.Named){relic.namedKills++;chapter.namedKills++;}
            else {relic.demonKills++;chapter.demonKills++;}
        }
        void CloseRelicLife(Character dead)
        {
            foreach(var relic in S.relics)
            {
                var chapter=relic.history.FirstOrDefault(h=>h.userId==dead.id&&!h.deceased);
                if(chapter==null)continue;
                chapter.deceased=true;chapter.endDay=S.day;relic.awaitingRestoration=true;relic.currentUserId=null;
            }
            dead.relics.Clear();dead.relicHealthBonus=dead.relicAttackBonus=0;
        }
        public RestorationPreview PreviewRestoration(string id)
        {
            var relic=S.relics.FirstOrDefault(r=>r.id==id);
            if(relic==null||!relic.awaitingRestoration)return new RestorationPreview();
            int days=Math.Max(0,relic.days-relic.paidDays),zombies=Math.Max(0,relic.Zombies-relic.paidZombies),demons=Math.Max(0,relic.demonKills-relic.paidDemons);
            return new RestorationPreview{health=days/10+3*demons,attack=zombies/10+demons,carryDays=days%10,carryZombies=zombies%10,demons=demons};
        }
        public bool RestoreRelic(string id)
        {
            var relic=S.relics.FirstOrDefault(r=>r.id==id);
            if(S.phase!=Phase.Morning||relic==null||!relic.awaitingRestoration||S.divinity<Rules.RelicCost)return false;
            var preview=PreviewRestoration(id);S.divinity-=Rules.RelicCost;
            relic.healthBonus+=preview.health;relic.attackBonus+=preview.attack;
            relic.paidDays+=(relic.days-relic.paidDays)/10*10;
            relic.paidZombies+=(relic.Zombies-relic.paidZombies)/10*10;relic.paidDemons=relic.demonKills;
            relic.restorations++;relic.awaitingRestoration=false;
            Log(relic.name+" 복원 · 최대 체력 +"+preview.health+" / 공격력 +"+preview.attack+" · 역사 "+relic.history.Count+"장");
            SavePoint();return true;
        }
    }
}

