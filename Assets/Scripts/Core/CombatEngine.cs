using System;
using System.Collections.Generic;
using System.Linq;
namespace Apocalypse
{
    public sealed partial class GameEngine
    {
        sealed class CombatEnemy { public EnemyKind kind; public int health, attack, viewIndex; public NamedZombieRecord named; }
        string EnemyName(CombatEnemy enemy,Sin legion)
        { return enemy.named!=null?"네임드 좀비 · "+enemy.named.name:enemy.kind==EnemyKind.Demon?Rules.Sins[(int)legion]+"의 악마":enemy.kind==EnemyKind.Special?"특수 좀비":"일반 좀비"; }
        CombatEnemy CreateEnemy(EnemyKind kind,Sin legion,NamedZombieRecord named=null)
        {
            var stats=GetEnemyStats(kind,named);return new CombatEnemy{kind=kind,health=stats.health,attack=stats.attack,named=named};
        }
        bool CanBattle(EnemyKind kind,Sin legion,NamedZombieRecord named)
        {
            if(S.avatar==null||S.phase!=Phase.Running||S.day==S.avatar.selectedDay)return false;
            if(kind==EnemyKind.Demon&&(S.goal!=Goal.Rift||!CanReachDemon(legion)))return false;
            if(kind==EnemyKind.Named&&(S.goal!=Goal.Rift||S.activeRift!=(int)legion||named==null||named.legion!=legion||named.defeated))return false;
            return true;
        }
        public NamedZombieRecord RollSpecialReplacement(Sin legion)
        {
            if(S.goal!=Goal.Rift||S.activeRift!=(int)legion||S.phase!=Phase.Running)return null;
            var eligible=S.permanent.named.Where(n=>n.legion==legion&&!n.defeated).ToList();
            return eligible.Count>0&&Rng.Next()<Rules.NamedReplacementChance?Rng.Pick(eligible):null;
        }
        public bool BattleFormation(int normal,int special,Sin legion,bool selectedSpecial)
        {
            if(normal<0||special<0||normal+special==0||!CanBattle(EnemyKind.Normal,legion,null))return false;
            var enemies=new List<CombatEnemy>();
            if(special>0)
            {
                var named=selectedSpecial?RollSpecialReplacement(legion):null;
                enemies.Add(CreateEnemy(named==null?EnemyKind.Special:EnemyKind.Named,legion,named));
            }
            for(int i=0;i<normal;i++)enemies.Add(CreateEnemy(EnemyKind.Normal,legion));
            return Fight(enemies,legion);
        }
        bool Fight(List<CombatEnemy> enemies,Sin legion)
        {
            var avatar=S.avatar;CombatReplay replay=null;
            if(CombatRecorded!=null)
            {
                replay=new CombatReplay{avatarName=avatar.name,species=avatar.species,health=avatar.health,maxHealth=avatar.MaxHealth,location=S.goal,legion=legion,scene=combatScene??(S.goal==Goal.Rift?"rift":"ruins"),eventTitle=combatEventTitle};
                for(int i=0;i<enemies.Count;i++){enemies[i].viewIndex=i;replay.enemies.Add(new CombatEnemyView{name=EnemyName(enemies[i],legion),kind=enemies[i].kind,health=enemies[i].health});}
            }
            bool won=false;
            try { won=ResolveFight(enemies,legion,replay);return won; }
            finally
            {
                if(replay!=null)
                {
                    replay.finalHealth=avatar.health;replay.finalSanity=avatar.sanity;replay.finalHunger=avatar.hunger;
                    replay.died=S.avatar==null;replay.outcome=won?"전투 승리":replay.died?"화신체 사망":"전투 철수";
                    replay.reason=replay.died?S.lastDeath:won?"길을 가로막던 적을 쓰러뜨렸습니다.":"더 깊이 들어가지 않고 전투를 끝냈습니다.";
                    CombatRecorded?.Invoke(replay);
                }
            }
        }
        bool ResolveFight(List<CombatEnemy> enemies,Sin legion,CombatReplay replay)
        {
            var a=S.avatar;string encounter=Id("battle");int kills=0,round=0;double battleHours=0;bool recovered=false;
            int initialCount=enemies.Count;
            foreach(var enemy in enemies)
            {
                if(enemy.named!=null){S.lastNamedName=enemy.named.name;S.lastNamedDay=S.day;}
                Log("전투 시작 · "+EnemyName(enemy,legion)+" × 1");
            }
            var highest=enemies[0].kind;
            Stress(highest==EnemyKind.Demon?B.demonStress:highest==EnemyKind.Named?B.namedStress:B.normalStress,new[]{legion});
            if(CheckDeath(true,legion))return false;
            Action recover=()=>{
                if(!recovered&&a.species==Species.HalfUndead&&a.health>0&&a.health<=a.MaxHealth*.3)
                {
                    recovered=true;double before=a.health;a.health=Rules.Clamp(a.health+Rules.Round(a.MaxHealth*.1),a.MaxHealth);
                    replay?.moments.Add(new CombatMoment{kind=CombatMomentKind.Recovery,amount=Rules.Round(a.health-before),health=a.health});
                    Log("불완전한 생명 · 최대 체력10% 회복 · 이번 전투1회");
                }
            };
            recover();
            while(enemies.Count>0&&round<B.maxCombatRounds)
            {
                if(S.hours>=24){Log("밤이 깊어 전투를 중단하고 철수했다.");return false;}
                var target=enemies[0];round++;double attack=a.Attack+S.equipment,multiplier=1;
                if(target.kind==EnemyKind.Normal&&a.Has("alexander"))multiplier*=1+B.alexanderDamage;
                if(initialCount==1&&a.Has("musashi"))multiplier*=1+B.musashiDamage;
                if(S.normalWins>=3&&a.Has("genghis"))multiplier*=1+B.genghisDamage;
                if(a.hunger>=50&&a.Has("heracles"))multiplier*=1+B.heraclesDamage;
                if(target.kind==EnemyKind.Named&&a.Has("artemis"))multiplier*=1+B.artemisDamage;
                if(target.kind==EnemyKind.Demon&&a.Has("arthur"))multiplier*=1+B.arthurDamage;
                double damage=attack*multiplier;
                if(target.kind==EnemyKind.Demon&&round==1&&a.Has("zeus"))damage=attack*B.zeusFirst;
                if(round==1&&a.species==Species.Beastkin)damage*=1.2;
                if(a.Has("tesla")&&Rng.Next()<B.teslaChance)damage+=attack*B.teslaDamage;
                if(a.Has("curie")&&Rng.Next()<B.curieChance)damage+=attack*B.curieDamage;
                int hitOut=Math.Max(1,Rules.Round(damage));target.health-=hitOut;
                replay?.moments.Add(new CombatMoment{kind=CombatMomentKind.AvatarAttack,target=target.viewIndex,amount=hitOut,health=Math.Max(0,target.health)});
                Log("공격 "+hitOut+" · 적 잔여 체력 "+Math.Max(0,target.health));
                battleHours+=B.combatRoundHours;
                if(!SpendTime(target.kind==EnemyKind.Demon?Activity.Rift:Activity.Combat,B.combatRoundHours,true,legion))return false;
                if(target.health<=0)
                {
                    int reward=target.kind==EnemyKind.Demon?100:target.kind==EnemyKind.Special?15:target.kind==EnemyKind.Named?Rules.Round(30*(a.Has("hades")?1+B.hadesReward:1)):1;
                    if(Reward(encounter+":enemy:"+kills,reward)){S.totalKills++;AddRiftProgress(target.kind,legion);RecordRelicKill(target.kind);}
                    kills++;if(target.named!=null)target.named.defeated=true;enemies.RemoveAt(0);
                    Log("적 처치 · "+EnemyName(target,legion)+" · 신위 +"+reward);
                    if(enemies.Count==0)break;
                }
                double incoming=enemies.Sum(e=>e.attack);
                if(target.kind==EnemyKind.Normal&&a.Has("napoleon"))incoming*=1-B.napoleonReduction;
                if(a.health<=a.MaxHealth*.3&&a.Has("leonidas"))incoming*=1-B.leonidasReduction;
                if(enemies.Count>=3&&a.Has("yi"))incoming*=1-B.yiReduction;
                if(a.species==Species.Machine)incoming*=.9;
                int hit=a.species==Species.Fishfolk&&Rng.Next()<.12?0:Math.Max(1,Rules.Round(incoming));
                a.health=Rules.Clamp(a.health-hit,a.MaxHealth);Log(hit==0?"유연한 움직임 · 공격 회피":"피격 "+hit+" · 화신체 체력 "+Rules.Round(a.health));
                replay?.moments.Add(new CombatMoment{kind=CombatMomentKind.GroupAttack,amount=hit,health=a.health});
                recover();if(CheckDeath(true,legion))return false;
            }
            if(enemies.Count>0){Log("전투 제한 시간 · 전술적으로 철수했다.");return false;}
            if(battleHours<B.minBattleHours&&!SpendTime(highest==EnemyKind.Demon?Activity.Rift:Activity.Combat,B.minBattleHours-battleHours,true,legion))return false;
            S.lastBattle="전투 승리 · "+round+"턴 · 신위 보상 정산 완료";
            if(highest==EnemyKind.Normal)S.normalWins++;
            if(highest==EnemyKind.Demon)Seal(legion,encounter+":rift");
            SavePoint();return true;
        }
    }
}

