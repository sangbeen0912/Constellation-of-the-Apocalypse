using System;
using System.Collections.Generic;
namespace Apocalypse
{
    // One authoritative, serializable battle. Rendering never recalculates combat.
    public sealed partial class GameEngine
    {
        public bool InteractiveCombat { get; set; }
        static readonly double[,] CommandRates={{85,70,35},{70,90,85},{85,90,80},{50,45,35},{95,35,15},{45,90,95}};
        CombatReplay observedBattle;
        ChoiceEvent battleSource;
        int battleChoice;
        public BattleState CurrentCombat { get { return S.combat; } }
        BattleEnemy CreateEnemy(EnemyKind kind,Sin legion,NamedZombieRecord named=null)
        {
            var stats=GetEnemyStats(kind,named,legion);
            return new BattleEnemy{kind=kind,hp=stats.health,maxHp=stats.health,attack=stats.attack,named=named,
                name=named!=null?"네임드 좀비 · "+named.name:kind==EnemyKind.Demon?Rules.Sins[(int)legion]+"의 악마":
                kind==EnemyKind.Special?SpecialNames[(int)legion]:"일반 좀비"};
        }
        static readonly string[] SpecialNames={"왕관 좀비","약탈 좀비","몽환 좀비","그림자 좀비","비대 좀비","광전 좀비","둔중 좀비"};
        bool CanBattle(EnemyKind kind,Sin legion,NamedZombieRecord named)
        {
            if(S.avatar==null||S.phase!=Phase.Running||S.combat!=null||S.day==S.avatar.selectedDay)return false;
            if(kind==EnemyKind.Demon&&(S.goal!=Goal.Rift||!CanReachDemon(legion)))return false;
            if(kind==EnemyKind.Named&&(S.goal!=Goal.Rift||S.activeRift!=(int)legion||named==null||named.legion!=legion||named.defeated))return false;
            return true;
        }
        public NamedZombieRecord RollSpecialReplacement(Sin legion)
        {
            if(S.goal!=Goal.Rift||S.activeRift!=(int)legion||S.phase!=Phase.Running)return null;
            var eligible=new List<NamedZombieRecord>();
            foreach(var n in S.permanent.named)if(n.legion==legion&&!n.defeated)eligible.Add(n);
            return eligible.Count>0&&Rng.Next()<Rules.NamedReplacementChance?Rng.Pick(eligible):null;
        }
        public bool BattleFormation(int normal,int special,Sin legion,bool selectedSpecial)
        {
            if(normal<0||special<0||normal+special==0||!CanBattle(EnemyKind.Normal,legion,null))return false;
            var enemies=new List<BattleEnemy>(normal+1);
            if(special>0){var named=selectedSpecial?RollSpecialReplacement(legion):null;enemies.Add(CreateEnemy(named==null?EnemyKind.Special:EnemyKind.Named,legion,named));}
            for(int i=0;i<normal;i++)enemies.Add(CreateEnemy(EnemyKind.Normal,legion));
            return Fight(enemies,legion);
        }
        bool Fight(List<BattleEnemy> enemies,Sin legion)
        {
            var a=S.avatar;
            var b=new BattleState{id=Id("battle"),legion=legion,enemies=enemies,avatarName=a.name,
                scene=combatScene??(S.goal==Goal.Rift?"rift":AreaRules.Scene(S.exploration.current==Area.Shelter?Area.Residence:S.exploration.current)),title=combatEventTitle,
                source=battleSource,choice=battleChoice};
            for(int i=0;i<enemies.Count;i++)
            {
                enemies[i].id=b.id+":"+i;b.serial++;
                if(enemies[i].named!=null){S.lastNamedName=enemies[i].named.name;S.lastNamedDay=S.day;}
                Log("전투 시작 · "+enemies[i].name+(enemies[i].kind==EnemyKind.Special?" · 특수 좀비":"")+" × 1");
            }
            S.combat=b;S.phase=Phase.Combat;
            if(!InteractiveCombat&&CombatRecorded!=null)
            {
                observedBattle=new CombatReplay{avatarName=a.name,species=a.species,health=a.health,maxHealth=a.MaxHealth,location=S.goal,legion=legion,scene=b.scene,eventTitle=b.title};
                foreach(var e in enemies)observedBattle.enemies.Add(new CombatEnemyView{name=e.name,kind=e.kind,health=e.hp});
            }
            var highest=enemies[0].kind;
            Stress(highest==EnemyKind.Demon?B.demonStress:highest==EnemyKind.Named?B.namedStress:B.normalStress,new[]{legion});
            if(CheckDeath(true,legion)){FinishCombat(b,false,"화신체 사망");return false;}
            SavePoint();
            if(InteractiveCombat)return false;
            while(S.combat!=null){if(b.waiting)ContinueCombat();else StepCombat();}
            return b.won;
        }
        public void ContinueCombat()
        {
            if(S.combat==null)return;
            S.combat.waiting=false;S.combat.prompt=null;SavePoint();
        }
        public double CommandAcceptance(BattleCommand command,int target=-1)
        {
            if(S.avatar==null||S.combat==null)return 0;
            // Persisted values: brave/cautious/cold/defiant/aggressive/fearful.
            var a=S.avatar;double chance=CommandRates[(int)a.personality,(int)command];
            if(a.health<=a.MaxHealth*.3){if(command==BattleCommand.Defend)chance+=10;if(command==BattleCommand.Retreat)chance+=15;}
            if(a.health<=a.MaxHealth*.1&&command==BattleCommand.Retreat)chance+=15;
            if(command==BattleCommand.Attack&&target>=0&&target<S.combat.enemies.Count)
            {var e=S.combat.enemies[target];if(e.hp<=e.maxHp*.25)chance+=10;}
            if(command==BattleCommand.Defend&&StrongAttackPending())chance+=15;
            if(command==BattleCommand.Retreat&&((a.hunger>=30&&a.hunger<=40)||(a.sanity>=30&&a.sanity<=40)))chance+=15;
            return Math.Min(95,chance)/100;
        }
        public bool IssueCombatCommand(BattleCommand command,int target=-1)
        {
            var b=S.combat;
            if(b==null||!b.waiting||S.avatar==null||(int)command<0||(int)command>2)return false;
            LockControl();
            if(S.controlLocked||S.divinity<10)return false;
            if(command==BattleCommand.Attack&&(target<0||target>=b.enemies.Count||b.enemies[target].hp<=0))return false;
            double acceptance=CommandAcceptance(command,target);
            S.divinity-=10;b.waiting=false;b.prompt=null;
            if(Rng.Next()>=acceptance){Log("전투 명령 거부 · 신위 10 소모");SavePoint();return true;}
            Log("전투 명령 수락 · "+command+" · 신위 10 소모");
            if(command==BattleCommand.Attack)b.lockedTarget=target;
            else if(command==BattleCommand.Defend)b.defending=true;
            else
            {
                double chance=1;
                foreach(var e in b.enemies)if(e.hp>0){if(e.kind==EnemyKind.Demon)chance=Math.Min(chance,.5);else if(e.kind==EnemyKind.Named)chance=Math.Min(chance,.7);}
                if(Rng.Next()<chance){b.retreated=true;FinishCombat(b,false,"전투 퇴각 성공");return true;}
                Log("퇴각 실패 · 대기 중이던 적 행동을 계속한다.");
            }
            SavePoint();return true;
        }
        bool StrongAttackPending()
        {
            var b=S.combat;if(b==null)return false;
            foreach(var e in b.enemies)if(e.hp>0&&e.kind==EnemyKind.Demon&&
                ((b.legion==Sin.Greed||b.legion==Sin.Lust)&&(e.attacks+1)%3==0||
                 b.legion==Sin.Envy&&e.envyStart>=0&&b.turn-e.envyStart>=3))return true;
            return false;
        }
        void PromptCombat(string forced=null)
        {
            var b=S.combat;if(b==null||S.avatar==null)return;
            LockControl();if(S.controlLocked)return;
            string reason=forced;
            var a=S.avatar;
            if(reason==null)
            {
                if(a.health/a.MaxHealth<=B.november.crisisHp&&!b.prompts.Contains("hp"))reason="hp";
                else if(a.hunger>=30&&a.hunger<=40||a.sanity>=30&&a.sanity<=40){if(!b.prompts.Contains("boundary"))reason="boundary";}
                if(reason==null&&StrongAttackPending()&&!b.prompts.Contains("strong:"+b.turn))reason="strong:"+b.turn;
                if(reason==null)for(int i=0;i<b.enemies.Count;i++){var e=b.enemies[i];if(e.hp>0&&e.hp<=e.maxHp*B.november.enemyCrisisHp&&!b.prompts.Contains("low:"+e.id)){reason="low:"+e.id;break;}}
            }
            if(reason==null||b.prompts.Contains(reason))return;
            b.prompts.Add(reason);b.prompt=forced!=null?"폭식의 소환 · 전투 개입 기회":reason=="hp"?"화신체 체력 위기":reason=="boundary"?"허기·정신력 경계":reason.StartsWith("strong")?"강한 공격 예고":"적 체력 약화";
            b.waiting=InteractiveCombat;SavePoint();
        }
        int AutoTarget(BattleState b)
        {
            int chosen=-1;var p=S.avatar.personality;
            for(int i=0;i<b.enemies.Count;i++)if(b.enemies[i].hp>0)
            {
                if(chosen<0){chosen=i;continue;}
                var e=b.enemies[i];var best=b.enemies[chosen];
                if(p==Temperament.Aggressive||p==Temperament.Cold){if(e.attack>best.attack)chosen=i;}
                else if(p==Temperament.Defiant)continue;
                else if(e.hp<best.hp)chosen=i;
            }
            return chosen;
        }
        void Moment(BattleState b,CombatMomentKind kind,int amount,double health,int target=-1,int actor=-1,string message=null)
        {
            b.lastMoment=new CombatMoment{kind=kind,amount=amount,health=health,target=target,actor=actor,message=message};
            observedBattle?.moments.Add(b.lastMoment);
        }
        void Recover(BattleState b)
        {
            var a=S.avatar;if(a==null||b.recovered||a.species!=Species.HalfUndead||a.health<=0||a.health>a.MaxHealth*.3)return;
            b.recovered=true;double before=a.health;a.health=Rules.Clamp(a.health+Rules.Round(a.MaxHealth*.1),a.MaxHealth);
            Moment(b,CombatMomentKind.Recovery,Rules.Round(a.health-before),a.health,message:"불완전한 생명");
            Log("불완전한 생명 · 최대 체력10% 회복 · 이번 전투1회");
        }
        void RecoverAfterHit(BattleState b)
        {
            var hit=b.lastMoment;Recover(b);
            if(b.lastMoment!=hit&&b.lastMoment!=null&&b.lastMoment.kind==CombatMomentKind.Recovery){b.queuedMoment=b.lastMoment;b.lastMoment=hit;}
        }
        public bool StepCombat()
        {
            var b=S.combat;if(b==null||b.waiting)return false;
            if(S.avatar==null){FinishCombat(b,false,"화신체 사망");return true;}
            b.lastMoment=null;
            if(b.queuedMoment!=null){b.lastMoment=b.queuedMoment;b.queuedMoment=null;PromptCombat();SavePoint();return true;}
            if(CheckDeath(true,b.legion)){FinishCombat(b,false,"화신체 사망");return true;}
            Recover(b);
            if(b.lastMoment!=null&&b.lastMoment.kind==CombatMomentKind.Recovery){SavePoint();return true;}
            if(b.enemyIndex<0)
            {
                if(S.hours>=24||b.turn>=B.maxCombatRounds){FinishCombat(b,false,"시간 부족 · 전투 철수");return true;}
                b.defending=false;
                int target=b.lockedTarget>=0&&b.lockedTarget<b.enemies.Count&&b.enemies[b.lockedTarget].hp>0?b.lockedTarget:AutoTarget(b);
                if(target<0){FinishCombat(b,true,"전투 승리");return true;}
                bool sloth=false;foreach(var e in b.enemies)if(e.hp>0&&e.kind==EnemyKind.Demon&&b.legion==Sin.Sloth)sloth=true;
                b.avatarActions++;
                if(sloth&&b.avatarActions%4==0){Moment(b,CombatMomentKind.Blocked,0,S.avatar.health,message:"나태 · 행동 봉쇄");Log("나태의 속박 · 화신체 행동 1회 봉쇄");}
                else {b.lockedTarget=-1;AvatarStrike(b,target);}
                if(S.avatar==null){FinishCombat(b,false,"화신체 사망");return true;}
                if(!SpendTime(S.goal==Goal.Rift?Activity.Rift:Activity.Combat,B.combatRoundHours,true,b.legion)){FinishCombat(b,false,"화신체 사망");return true;}
                if(AutoTarget(b)<0){FinishCombat(b,true,"전투 승리");return true;}
                b.enemyIndex=0;PromptCombat();
            }
            else
            {
                while(b.enemyIndex<b.enemies.Count&&(b.enemies[b.enemyIndex].hp<=0||b.enemies[b.enemyIndex].spawnTurn==b.turn))b.enemyIndex++;
                if(b.enemyIndex<b.enemies.Count)
                {
                    int at=b.enemyIndex++;EnemyAct(b,at);
                    if(S.avatar==null){FinishCombat(b,false,"화신체 사망");return true;}
                    RecoverAfterHit(b);
                    if(CheckDeath(true,b.legion)){FinishCombat(b,false,"화신체 사망");return true;}
                    if(!b.waiting&&b.queuedMoment==null)PromptCombat();
                }
                else
                {
                    foreach(var e in b.enemies)if(e.hp>0&&e.kind==EnemyKind.Demon&&e.envyStart>=0&&b.turn-e.envyStart>=3)
                    {
                        int hit=Rules.Round(e.envyDamage*.5*(b.defending?.4:1));e.envyStart=-1;
                        S.avatar.health=Rules.Clamp(S.avatar.health-hit,S.avatar.MaxHealth);
                        Moment(b,CombatMomentKind.Reflect,hit,S.avatar.health,message:"질투 · 축적 피해 반사");Log("질투 · 축적 피해 반사 "+hit);
                        RecoverAfterHit(b);
                        if(CheckDeath(true,b.legion)){FinishCombat(b,false,"화신체 사망");return true;}
                    }
                    b.turn++;b.enemyIndex=-1;
                }
            }
            SavePoint();return true;
        }
        void AvatarStrike(BattleState b,int target)
        {
            var a=S.avatar;var e=b.enemies[target];double attack=a.Attack+S.equipment,m=1;
            if(e.kind==EnemyKind.Normal&&a.Has("alexander"))m*=1+B.alexanderDamage;
            if(b.enemies.Count==1&&a.Has("musashi"))m*=1+B.musashiDamage;
            if(S.normalWins>=3&&a.Has("genghis"))m*=1+B.genghisDamage;
            if(a.hunger>=50&&a.Has("heracles"))m*=1+B.heraclesDamage;
            if(e.kind==EnemyKind.Named&&a.Has("artemis"))m*=1+B.artemisDamage;
            if(e.kind==EnemyKind.Demon&&a.Has("arthur"))m*=1+B.arthurDamage;
            double damage=attack*m;
            if(e.kind==EnemyKind.Demon&&b.avatarActions==1&&a.Has("zeus"))damage=attack*B.zeusFirst;
            if(b.avatarActions==1&&a.species==Species.Beastkin)damage*=1.2;
            if(a.Has("tesla")&&Rng.Next()<B.teslaChance)damage+=attack*B.teslaDamage;
            if(a.Has("curie")&&Rng.Next()<B.curieChance)damage+=attack*B.curieDamage;
            if(e.kind==EnemyKind.Demon)
            {
                if(b.legion==Sin.Pride)damage*=e.hits<5?.6:e.hits<7?1.1:1;
                if(b.legion==Sin.Wrath&&e.enraged)damage*=1.2;
            }
            int hit=Math.Max(1,Rules.Round(damage)),actual=Math.Min(hit,e.hp);e.hp=Math.Max(0,e.hp-hit);e.hits++;e.received+=actual;
            if(e.kind==EnemyKind.Demon)
            {
                if(b.legion==Sin.Wrath&&e.hp<=e.maxHp*.5)e.enraged=true;
                if(b.legion==Sin.Envy)
                {
                    if(!e.envyUsed&&e.hp<=e.maxHp*.5){e.envyUsed=true;e.envyStart=b.turn+(B.november.envyIncludesTriggerTurn?0:1);}
                    if(e.envyStart>=0&&b.turn>=e.envyStart)e.envyDamage+=actual;
                }
            }
            Moment(b,CombatMomentKind.AvatarAttack,hit,e.hp,target);
            Log("공격 "+hit+" · "+e.name+" 잔여 체력 "+e.hp);
            if(e.hp==0)
            {
                int reward=e.kind==EnemyKind.Demon?100:e.kind==EnemyKind.Special?15:e.kind==EnemyKind.Named?Rules.Round(30*(a.Has("hades")?1+B.hadesReward:1)):1;
                if(Reward(e.id,reward)){S.totalKills++;AddRiftProgress(e.kind,b.legion);RecordRelicKill(e.kind);}
                if(e.named!=null)e.named.defeated=true;
                if(e.kind==EnemyKind.Demon)foreach(var summoned in b.enemies)if(summoned.spawnTurn>=0)summoned.hp=0;
                Log("적 처치 · "+e.name+" · 신위 +"+reward);
            }
        }
        void EnemyAct(BattleState b,int at)
        {
            var e=b.enemies[at];var a=S.avatar;
            if(e.kind==EnemyKind.Demon&&b.legion==Sin.Gluttony&&e.received>=e.maxHp*.3)
            {
                int alive=0;foreach(var z in b.enemies)if(z.hp>0&&z.spawnTurn>=0)alive++;
                int count=alive==0?2:1,group=++b.groupSerial;
                for(int i=0;i<count;i++){var z=CreateEnemy(EnemyKind.Normal,b.legion);z.id=b.id+":"+b.serial++;z.spawnTurn=b.turn;z.group=group;b.enemies.Add(z);}
                e.received=0;Moment(b,CombatMomentKind.Summon,count,e.hp,at,at,"폭식 · 일반 좀비 소환");Log("폭식 · 일반 좀비 "+count+"마리 소환");
                PromptCombat("spawn:"+group);return;
            }
            e.attacks++;double damage=e.attack;
            if(e.kind==EnemyKind.Demon&&b.legion==Sin.Wrath&&e.enraged)damage*=1.35;
            if(e.kind==EnemyKind.Normal&&a.Has("napoleon"))damage*=1-B.napoleonReduction;
            if(a.health<=a.MaxHealth*.3&&a.Has("leonidas"))damage*=1-B.leonidasReduction;
            int living=0;foreach(var z in b.enemies)if(z.hp>0)living++;
            if(living>=3&&a.Has("yi"))damage*=1-B.yiReduction;
            if(a.species==Species.Machine)damage*=.9;
            if(b.defending)damage*=.4;
            int hit=a.species==Species.Fishfolk&&Rng.Next()<.12?0:Math.Max(1,Rules.Round(damage));
            double actual=Math.Min(a.health,hit);a.health=Rules.Clamp(a.health-hit,a.MaxHealth);
            Moment(b,CombatMomentKind.EnemyAttack,hit,a.health,actor:at,message:e.name);
            Log(hit==0?"유연한 움직임 · 공격 회피":e.name+" 공격 "+hit+" · 화신체 체력 "+Rules.Round(a.health));
            if(e.kind==EnemyKind.Demon)
            {
                if(b.legion==Sin.Greed&&e.attacks%3==0){int heal=Rules.Round(actual*.5);e.hp=Math.Min(e.maxHp,e.hp+heal);Log("탐욕 · 흡혈 회복 "+heal);}
                if(b.legion==Sin.Lust&&e.attacks%3==0){a.sanity=Rules.Clamp(a.sanity-15);LockControl();Log("색욕 · 정신력 -15");}
                if(b.legion==Sin.Gluttony)
                {
                    int absorbed=0;
                    foreach(var z in b.enemies)if(z.hp>0&&z.spawnTurn>=0&&b.turn-z.spawnTurn>=(B.november.spawnIncludesCreationTurn?1:2)){z.hp=0;absorbed++;}
                    if(absorbed>0){e.hp=Math.Min(e.maxHp,e.hp+Rules.Round(e.maxHp*.05)*absorbed);Log("폭식 · 소환 좀비 "+absorbed+"마리 흡수");}
                }
            }
        }
        void FinishCombat(BattleState b,bool won,string outcome)
        {
            if(b.ended)return;b.ended=true;b.won=won;b.outcome=outcome;b.waiting=false;
            S.combat=null;
            if(S.avatar!=null)
            {
                S.phase=Phase.Running;S.lastBattle=outcome+" · "+(b.turn+1)+"턴";Log(S.lastBattle);
                if(won)
                {
                    bool demon=false;foreach(var e in b.enemies)if(e.kind==EnemyKind.Demon)demon=true;
                    if(demon)Seal(b.legion,b.id+":seal");else S.normalWins++;
                }
                if(b.source!=null)CompleteEventOption(b.source,b.choice,won);
                if(S.returnRequested&&S.phase==Phase.Running)ContinueReturn();
            }
            if(observedBattle!=null)
            {
                observedBattle.finalHealth=S.avatar==null?0:S.avatar.health;observedBattle.died=S.avatar==null;
                observedBattle.outcome=outcome;observedBattle.reason=outcome;
                var replay=observedBattle;observedBattle=null;CombatRecorded?.Invoke(replay);
            }
            SavePoint();
        }
    }
}

