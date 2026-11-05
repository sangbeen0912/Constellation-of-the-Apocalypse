using System;
using System.Collections.Generic;
using System.Linq;

namespace Apocalypse
{
    // Pure C# deterministic simulation. Presentation speed is deliberately absent.
    public sealed partial class GameEngine
    {
        public readonly GameState S;
        public readonly Balance B;
        public readonly Content Content;
        public readonly RandomStream Rng;
        public event Action Checkpoint;
        public GameEngine(GameState state, Balance balance, Content content)
        {
            S = state; B = balance; Content = content; Rng = new RandomStream(S); if (S.goal != Goal.Rift) S.goal = Goal.Explore;
            if (S.permanent.stories == null) S.permanent.stories = new List<AcquiredStory>();
            if (S.rulesRevision < 2)
            {
                // Legacy visits/wins cannot be converted into per-zombie scores faithfully.
                foreach(var demon in S.demons) demon.progress = 0;
                if(S.pending != null && S.pending.title == "균열의 마지막 문")
                { var replacement=Content.activityEvents.FirstOrDefault(e=>e.id=="RF-005");if(replacement!=null)ApplyTemplate(S.pending,replacement); }
                Log("옛 방문 기록은 처치 점수로 환산하지 않고 보존했다.");
            }
            MigrateCurrentRules();InitializeNovember();
        }
        public static GameEngine New(uint seed, Balance b, Content c, PermanentData permanent = null)
        {
            var s = new GameState { rulesRevision=Rules.RulesRevision, seed = seed, rng = seed == 0 ? 1u : seed, runId = Guid.NewGuid().ToString("N"),
                food = b.initialFood, divinity = b.initialDivinity, medicine = b.initialMedicine, syringes=b.initialSyringes };
            if (permanent != null) s.permanent = permanent;
            var game = new GameEngine(s, b, c);
            for (int i = 0; i < 7; i++) s.demons.Add(new Demon { sin = (Sin)i });
            s.candidates.Add(game.CreateCharacter(false));
            game.Select(s.candidates[0].id);game.Log("한 생존자와 첫 계약을 맺었다.");
            return game;
        }
        public void Log(string message)
        {
            S.logs.Add(S.day+"일 "+Rules.Clock(S.hours)+"  "+message);
            if (S.logs.Count > 350) S.logs.RemoveAt(0);
        }
        void SavePoint() { if (Checkpoint != null) Checkpoint(); }
        string Id(string prefix) { return S.runId + ":" + prefix + ":" + (++S.eventSerial); }
        public Character CreateCharacter(bool allowMutation)
        {
            string[] first = { "서", "윤", "강", "한", "이", "정", "백", "신" };
            string[] last = { "도윤", "서연", "지후", "하린", "시온", "유진", "민서", "태오" };
            bool mutant = allowMutation && Rng.Next() < B.mutantChance;
            var c = new Character { id = Id("npc"), name = Rng.Pick(first) + Rng.Pick(last),
                baseHealth = Rng.Int(B.healthMin, B.healthMax + 1), baseAttack = Rng.Int(B.attackMin, B.attackMax + 1),
                personality = (Temperament)Rng.Int(0, 6), mutant = mutant };
            if (mutant) { c.baseAttack += 5; c.baseHealth += 10; }
            c.health = c.MaxHealth;
            c.sanity = Rng.Int(75, 101); c.hunger = Rng.Int(75, 101);
            return c;
        }
        public bool Select(string id)
        {
            if (S.phase != Phase.Selection && S.phase != Phase.Death) return false;
            var c = S.candidates.FirstOrDefault(x => x.id == id);
            if (c == null) return false;
            S.avatar = c; S.candidates.Remove(c); c.selectedDay = S.day;
            S.phase = Phase.Morning; S.hours = 0; S.dayStarted = false;
            S.controlLocked = c.hunger < 30 || c.sanity < 30;
            S.sanityCauses.Clear();
            S.nextRiftDay = S.day + 1;
            Log(c.name + "과 별의 계약을 맺었다. 오늘은 안전하게 적응한다."); SavePoint(); return true;
        }
        public bool Reward(string key, int amount)
        {
            if (S.rewardLedger.Contains(key)) return false;
            S.rewardLedger.Add(key); S.divinity += amount; S.totalEarned += amount; return true;
        }
        public bool Invest(Stat stat, int amount)
        {
            if (S.phase != Phase.Morning || amount <= 0 || S.divinity < amount || S.avatar == null) return false;
            if(stat!=Stat.Health&&stat!=Stat.Attack)return false;
            int spent=0,steps=0;
            while(UpgradeCost(stat)<=amount-spent)
            {
                int cost=UpgradeCost(stat);spent+=cost;steps++;
                if(stat==Stat.Health)S.avatar.healthInvestment+=10;else S.avatar.attackInvestment+=10;
            }
            if(steps==0)return false;
            S.divinity-=spent;
            Log("후원 신위 " + spent + " · " + (stat == Stat.Health ? "최대 체력" : "공격력") + " "+steps+"회 강화"); SavePoint(); return true;
        }
        public int UpgradeCount(Stat stat){return S.avatar==null?0:(stat==Stat.Health?S.avatar.healthInvestment:S.avatar.attackInvestment)/10;}
        public int UpgradeCost(Stat stat){return 10*(1+UpgradeCount(stat)/10);}
        public bool Inherit(string id)
        {
            if (S.phase != Phase.Morning || S.avatar == null || !S.permanent.unlocked.Contains(id) || S.avatar.Has(id)) return false;
            var c = Content.Find(id); int cost = Rules.InheritCosts[c.grade];
            if (S.divinity < cost) return false;
            S.divinity -= cost; S.avatar.inherited.Add(id); Log(c.name + "의 힘을 계승했다."); SavePoint(); return true;
        }
        public bool Supply()
        {
            if(S.phase!=Phase.Morning||S.divinity<Rules.SupplyCost||Content.supplies.Count==0)return false;
            var item=Rng.Pick(Content.supplies);S.divinity-=Rules.SupplyCost;
            if(item.id=="food")S.food+=item.amount;else if(item.id=="kit")S.medicine+=item.amount;else if(item.id=="syringe")S.syringes+=item.amount;
            else {var stack=S.additionalSupplies.FirstOrDefault(s=>s.id==item.id);if(stack==null){stack=new SupplyStack{id=item.id};S.additionalSupplies.Add(stack);}stack.amount+=item.amount;}
            Log("무작위 보급 · "+item.name+" +"+item.amount);AutoEat();SavePoint();return true;
        }



        public void AutoEat()
        {
            var a = S.avatar; if (a == null || a.hunger >= 50 || a.hunger <= 0 || S.food <= 0) return;
            LockControl();
            // Fractional hunger is retained internally; integer food cannot exceed the capacity.
            int used = Math.Min(S.food, (int)Math.Floor(100 - a.hunger));
            S.food -= used; a.hunger = Rules.Clamp(a.hunger + used);
            if (used > 0) Log("자동 식사 · 식량 " + used + "개 사용");
        }
        void LockControl()
        {
            if (S.avatar != null && (S.avatar.hunger < 30 || S.avatar.sanity < 30) && !S.controlLocked)
            { S.controlLocked = true; Log("공복 또는 착란 · 오늘의 개입권이 잠겼다. 화신체는 자동 행동을 계속한다."); }
        }
        public bool SpendTime(Activity activity, double hours, bool combat = false, Sin? legion = null)
        {
            if (S.avatar == null || S.phase == Phase.Death || S.phase == Phase.Victory || hours<0) return false;
            AutoEat();
            double actual = Math.Min(hours, Math.Max(0, Rules.DayHours-S.hours));
            S.hours += actual; S.avatar.hunger = Rules.Clamp(S.avatar.hunger - actual * B.hungerRates[(int)activity]);
            LockControl();
            if (CheckDeath(combat, legion)) return false;
            AutoEat(); return true;
        }
        public void Stress(double amount, Sin[] causes, bool important = false)
        {
            var a = S.avatar; if (a == null) return;
            if (important && a.Has("sejong")) amount *= 1 - B.sejongReduction;
            if (a.sanity <= 30 && a.Has("joan")) amount *= 1 - B.joanReduction;
            a.sanity = Rules.Clamp(a.sanity - Rules.Round(amount));
            if (amount > 0 && causes != null) { S.sanityCauses.Clear(); S.sanityCauses.AddRange(causes); }
            LockControl();
        }
        public bool Treat(bool mental,bool sleep=false,bool useItem=false)
        {
            var preview=PreviewTreatment(mental,sleep,useItem);if(!preview.available)return false;
            if(useItem){if(mental)S.syringes--;else S.medicine--;}
            if(!SpendTime(sleep?Activity.Sleep:Activity.Rest,preview.hours))return false;
            var a=S.avatar;a.health=preview.healthAfter;a.sanity=preview.sanityAfter;
            Log((sleep?"잠자기":mental?"정신력 회복":"체력 회복")+" · "+preview.hours+"시간 · 회복 완료");SavePoint();return true;
        }

        public bool StartDay(Goal goal)
        {
            if(S.phase!=Phase.Morning||S.avatar==null||S.hours>=Rules.DayHours)return false;
            S.goal=goal==Goal.Rift&&S.activeRift>=0&&S.day>S.avatar.selectedDay?Goal.Rift:Goal.Explore;S.phase=Phase.Running;
            S.exploration.current=S.goal==Goal.Rift?Area.Rift:S.exploration.selected;S.exploration.routeOffered=false;
            S.exploration.resumeTransit=false;
            if(!S.dayStarted)
            {
                S.dayStarted=true;S.normalWins=0;S.eventSlots=0;S.returnRequested=false;
                S.eventTarget=DailyEventCount();S.nextImportantHour=S.hours+(Rules.DayHours-S.hours)/(S.eventTarget+1);
                if(S.avatar.personality==Temperament.Defiant&&Rng.Next()<B.refusalChance){S.goal=Goal.Explore;Log("고집 센 화신체가 공략을 거부하고 탐사를 택했다.");}
                RollConstellation();
            }
            SavePoint();return true;
        }

        public int[] AvailableGradeWeights()
        {
            return Enumerable.Range(0, 4).Select(g => Content.constellations.Any(c => c.grade == g && !S.permanent.unlocked.Contains(c.id)) ? Rules.GradeWeights[g] : 0).ToArray();
        }
        public bool RollConstellation()
        {
            if(S.constellationRolledDay==S.day)return false;
            S.constellationRolledDay=S.day;S.scheduledConstellation=null;
            int[] weights=AvailableGradeWeights();if(weights.Sum()==0||Rng.Next()>=Rules.ConstellationChance)return false;
            int grade=Rng.Weighted(weights);
            S.scheduledConstellation=Rng.Pick(Content.constellations.Where(c=>c.grade==grade&&!S.permanent.unlocked.Contains(c.id)).ToList()).id;
            S.constellationMeetingHour=Math.Min(Rules.DayHours,S.hours+B.constellationMeetDelay);
            Log(S.avatar!=null&&S.avatar.personality==Temperament.Cautious?"누군가의 시선이 느껴진다. 한 성좌가 조심스러운 발걸음을 지켜본다.":"한 성좌가 당신과 당신의 화신체를 흥미롭게 바라봅니다.");return true;
        }




        public void Recruit()
        {
            double chance = B.scoutSuccess + (S.avatar.Has("cleopatra") ? B.cleopatraScout : 0);
            if (Rng.Next() >= chance) { Log("구조한 생존자는 다른 쉘터로 향했다."); return; }
            var npc = CreateCharacter(true);
            if (S.candidates.Count + (S.avatar != null ? 1 : 0) < Rules.MaxNpc) { S.candidates.Add(npc); Log(npc.name + (npc.mutant ? " · 돌연변이 생존자 합류" : " · 생존자 합류")); }
            else { S.administrators++; Log("활성 인원 10명 · 새 생존자는 행정 업무로 전환했다."); }
        }
        public void Tick()
        {
            if(S.phase!=Phase.Running||S.avatar==null)return;
            LockControl();if(CheckDeath(false,null))return;AutoEat();
            if(S.returnRequested){ContinueReturn();return;}
            if(S.hours>=Rules.DayHours-.00001){if(!EncounterConstellation())EndDay();return;}
            if(!string.IsNullOrEmpty(S.scheduledConstellation)&&S.hours>=S.constellationMeetingHour){EncounterConstellation();return;}
            if(S.exploration.resumeTransit&&S.exploration.current==Area.Subway){OfferTransit();return;}
            bool bossReady=S.goal==Goal.Rift&&S.activeRift>=0&&S.day>S.avatar.selectedDay&&CanReachDemon((Sin)S.activeRift);
            if(S.goal==Goal.Explore&&S.exploration.current==Area.Subway&&!S.exploration.routeOffered&&Rng.Next()<.35){OfferTransit();return;}
            if(S.eventSlots<S.eventTarget&&(S.hours>=S.nextImportantHour||bossReady&&S.eventSlots==0)){ShowGeneralEvent();return;}
            bool safe=S.day==S.avatar.selectedDay;
            if(ShouldReturn()){RequestReturn("상태와 남은 시간을 살펴 조기 귀환을 결정했다.");return;}
            if(S.goal==Goal.Rift&&!safe&&S.activeRift>=0&&CanReachDemon((Sin)S.activeRift)&&!S.resolvedEvents.Contains("boss-offer:"+S.day))
            {ShowBossEvent();return;}
            bool assault=S.goal==Goal.Rift&&!safe&&S.activeRift>=0;
            int[] weights={S.food<80?55:assault?10:42,assault?5:38,safe||S.activeRift<0?0:assault?B.riftCombatWeight:B.exploreCombatWeight,10};
            if(S.avatar.personality==Temperament.Brave&&!safe&&S.activeRift>=0)weights[2]+=20;
            if(S.avatar.personality==Temperament.Cold)weights[1]+=20;
            if(S.avatar.personality==Temperament.Cautious)weights[3]+=20;
            if(S.exploration.current==Area.Residence&&Branch("HOM-01",1))weights[2]=Rules.Round(weights[2]*B.november.noiseMultiplier);
            int action=Rng.Weighted(weights);
            if(action==0){if(!SpendTime(Activity.Search,Math.Min(Rules.ActionHours(3),Rules.DayHours-S.hours)))return;SearchFood();}
            else if(action==1){if(!SpendTime(Activity.Scout,Math.Min(Rules.ActionHours(3)*(S.avatar.Has("zhuge")?1-B.zhugeTime:1),Rules.DayHours-S.hours)))return;Log("폐허 정찰 · 생존 신호를 탐색한다.");}
            else if(action==2){if(!SpendTime(Activity.Travel,Math.Min(Rules.ActionHours(1),Rules.DayHours-S.hours)))return;Battle(EnemyKind.Normal,Rng.Int(1,4),(Sin)S.activeRift);}
            else {if(!SpendTime(Activity.Rest,Math.Min(Rules.ActionHours(2),Rules.DayHours-S.hours)))return;Log("잠시 멈춰 주변을 살피고 호흡을 가다듬었다.");}
            S.totalActions++;SavePoint();
        }


        public int ActiveSealCount { get { return S.demons.Count(d=>d.remaining>0); } }
        public double EnemyHealthMultiplier { get { return 1 + Rules.HealthPerSeal*ActiveSealCount; } }
        public double EnemyAttackMultiplier { get { return 1 + Rules.AttackPerSeal*ActiveSealCount; } }
        public EnemyStats GetEnemyStats(EnemyKind kind, NamedZombieRecord named = null, Sin? legion = null)
        {
            int sin=(int)(legion??(S.activeRift>=0?(Sin)S.activeRift:Sin.Wrath));
            int health=kind==EnemyKind.Demon?B.november.demonHp[sin]:kind==EnemyKind.Special?B.november.specialHp[sin]:kind==EnemyKind.Named&&named!=null?named.health:B.normalHp;
            int attack=kind==EnemyKind.Demon?B.november.demonAttack[sin]:kind==EnemyKind.Special?B.november.specialAttack[sin]:kind==EnemyKind.Named&&named!=null?named.attack:B.normalAttack;
            return new EnemyStats{health=Rules.Round(health*EnemyHealthMultiplier),attack=Rules.Round(attack*EnemyAttackMultiplier)};
        }
        void AddRiftProgress(EnemyKind kind, Sin legion)
        {
            if(kind==EnemyKind.Demon||S.activeRift!=(int)legion||S.demons[(int)legion].remaining>0)return;
            int points=kind==EnemyKind.Special?Rules.SpecialProgress:kind==EnemyKind.Named?Rules.NamedProgress:Rules.NormalProgress;
            S.demons[(int)legion].progress+=points;
            Log("군단 봉쇄 돌파 +"+points+" · "+S.demons[(int)legion].progress+"/"+Rules.RiftProgressRequired);
        }
        public bool Battle(EnemyKind kind,int count,Sin legion,NamedZombieRecord named=null)
        {
            if(!CanBattle(kind,legion,named)||count<1)return false;
            if(kind!=EnemyKind.Normal)count=1;
            var enemies=new List<BattleEnemy>();for(int i=0;i<count;i++)enemies.Add(CreateEnemy(kind,legion,named));
            return Fight(enemies,legion);
        }

        public bool Seal(Sin sin, string rewardId)
        {
            var d = S.demons[(int)sin];
            if (d.remaining > 0 || S.activeRift != (int)sin || S.avatar == null) return false;
            d.remaining = S.avatar.Has("yog") ? Rules.EnhancedSealDays : Rules.SealDays; d.sealedDay = S.day;
            d.yogApplied=S.avatar.Has("yog");d.sealDuration=d.remaining;
            d.progress=0;
            Reward(rewardId, 300); S.totalSeals++; S.activeRift = -1; S.nextRiftDay = S.day + 1;
            Log(Rules.Sins[(int)sin] + " 봉인 성공 · " + d.remaining + "일");
            if (S.demons.All(x => x.remaining > 0)) { S.phase = Phase.Victory; S.permanent.victories++; Log("일곱 봉인이 동시에 빛났다. 종말의 문이 닫혔다."); }
            SavePoint(); return true;
        }
        public bool CanReachDemon(Sin sin)
        {
            var d = S.demons[(int)sin];
            return S.activeRift == (int)sin && d.remaining == 0 && d.progress >= Rules.RiftProgressRequired;
        }
        public bool CheckDeath(bool combat, Sin? legion)
        {
            var a = S.avatar; if (a == null) return S.phase == Phase.Death;
            if (a.health > 0 && a.hunger > 0 && a.sanity > 0) return false;
            string cause = a.hunger <= 0 ? "굶주림에 따른 자결" : a.sanity <= 0 ? "정신 붕괴에 따른 자결" : "치명상";
            Sin assigned;
            if (combat) assigned = legion ?? (S.activeRift >= 0 ? (Sin)S.activeRift : Sin.Wrath);
            else if (a.hunger <= 0) assigned = Sin.Gluttony;
            else assigned = S.sanityCauses.Count > 0 ? Rng.Pick(S.sanityCauses.Distinct().ToList()) : Sin.Sloth;
            S.permanent.named.Add(new NamedZombieRecord { id = Id("named"), name = a.name, health = a.MaxHealth, attack = a.Attack, legion = assigned });
            S.permanent.deaths.Add(new DeathRecord { name = a.name, cause = cause, combat = combat, day = S.day, selectedDay = a.selectedDay, legion = assigned });
            S.lastDeath = a.name + " · " + (combat ? "전투 사망" : "비전투 사망") + "\n" + cause + "\n" + Rules.Sins[(int)assigned] + " 군단의 네임드 좀비로 기록되었다.";
            CreditRelicDay(); CloseRelicLife(a);
            S.scheduledConstellation=null; S.pending=null; S.returnRequested=false;
            S.plot=new PlotState();S.exploration=new ExplorationState{selected=S.exploration.selected};
            if (S.activeRift >= 0) { S.demons[S.activeRift].progress=0;Log(Rules.Sins[S.activeRift] + " 군단 철수 · 균열 폐쇄 · 진행도 초기화"); }
            S.activeRift = -1; Log(S.lastDeath.Replace('\n', ' ')); S.avatar = null;
            S.settledDay = S.day; AdvanceCalendar(); S.phase = Phase.Death; S.nextRiftDay = S.day + 1;
            if (S.candidates.Count == 0) { S.candidates.Add(CreateCharacter(false)); Log("정부가 일반 생존자 1명을 지원했다."); }
            SavePoint(); return true;
        }
        public void EndDay()
        {
            if (S.phase != Phase.Running || S.settledDay == S.day || S.avatar == null) return;
            S.settledDay = S.day;
            CreditRelicDay();
            S.report = S.day + "일 정산\n" + S.avatar.name + " 생존 · 활동 " + S.hours.ToString("0.0") + "/15시간\n" +
                "신위 " + S.divinity + " · 식량 " + S.food + " · 봉인 " + S.demons.Count(d => d.remaining > 0) + "/7\n" +
                "허기 " + Rules.Round(S.avatar.hunger) + " · 정신력 " + Rules.Round(S.avatar.sanity);
            S.exploration.current=Area.Shelter;S.exploration.rejected.Clear();S.phase = Phase.Report; SavePoint();
        }
        void AdvanceCalendar()
        {
            S.day++; S.hours = 0; S.dayStarted = false; S.pending = null;
            S.scheduledConstellation=null;S.returnRequested=false;S.eventTarget=0;
            foreach (var d in S.demons) if (d.remaining > 0) d.remaining--;
            S.controlLocked = S.avatar != null && (S.avatar.hunger < 30 || S.avatar.sanity < 30);
            S.eventSlots = 0; S.normalWins = 0;
        }
        public bool NextDay()
        {
            if (S.phase != Phase.Report) return false;
            AdvanceCalendar(); S.phase = Phase.Morning; OpenRift(); SavePoint(); return true;
        }
        public void OpenRift()
        {
            if (S.activeRift >= 0 || S.day < S.nextRiftDay || S.avatar == null || S.day <= S.avatar.selectedDay) return;
            var eligible = S.demons.Where(x => x.remaining == 0).ToList();
            if (eligible.Count == 0) return;
            S.activeRift = (int)Rng.Pick(eligible).sin; S.nextRiftDay = int.MaxValue;
            S.demons[S.activeRift].progress=0;
            foreach(var record in S.permanent.named.Where(n=>(int)n.legion==S.activeRift))record.defeated=false;
            Log(Rules.Sins[S.activeRift] + " 군단의 균열이 열렸다.");
        }
        public void Validate()
        {
            if (S.day < 1 || S.hours < 0 || S.hours > Rules.DayHours+.00001 || S.divinity < 0 || S.food < 0 || S.medicine<0 || S.syringes<0 || S.activeRift < -1 || S.activeRift > 6) throw new InvalidOperationException("Invalid world state");
            if(S.eventSlots<0||S.eventSlots>4||S.eventTarget<0||S.eventTarget>4)throw new InvalidOperationException("Invalid daily event count");
            if(S.relics.Select(r=>r.id).Distinct().Count()!=S.relics.Count||S.relics.Any(r=>r.paidDays>r.days||r.paidZombies>r.Zombies||r.paidDemons>r.demonKills)||S.avatar!=null&&S.avatar.relics.Count>1)throw new InvalidOperationException("Invalid heirloom record");
            if (S.permanent.unlocked.Distinct().Count() != S.permanent.unlocked.Count || S.rewardLedger.Distinct().Count() != S.rewardLedger.Count) throw new InvalidOperationException("Duplicate transaction");
            if (S.candidates.Count + (S.avatar == null ? 0 : 1) > Rules.MaxNpc) throw new InvalidOperationException("NPC capacity exceeded");
            if (S.activeRift >= 0 && S.demons[S.activeRift].remaining > 0) throw new InvalidOperationException("Sealed demon has active rift");
            if(S.demons.Any(d=>d.progress<0||d.remaining<0))throw new InvalidOperationException("Invalid rift progress/seal");
            if (S.avatar != null && (S.avatar.health < 0 || S.avatar.health > S.avatar.MaxHealth || S.avatar.hunger < 0 || S.avatar.hunger > 100 || S.avatar.sanity < 0 || S.avatar.sanity > 100)) throw new InvalidOperationException("Invalid character range");
        }
    }
}
