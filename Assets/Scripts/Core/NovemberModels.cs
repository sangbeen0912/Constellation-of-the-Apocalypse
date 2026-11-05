using System;
using System.Collections.Generic;

namespace Apocalypse
{
    public enum Area { Shelter, Residence, Subway, Museum, Mart, Military, Sanctuary, ThemePark, Heritage, Rift }
    public enum BattleCommand { Attack, Defend, Retreat }
    [Serializable] public sealed class ExplorationState
    {
        public Area current=Area.Shelter, selected=Area.Residence;
        public int destination=-1;
        public bool routeOffered;
        public bool resumeTransit;
        public List<int> rejected=new List<int>(2);
    }
    [Serializable] public sealed class PlotState
    {
        public List<string> viewed=new List<string>(), partial=new List<string>(), completed=new List<string>();
    }
    [Serializable] public sealed class BattleEnemy
    {
        public string id,name;
        public EnemyKind kind;
        public int hp,maxHp,attack,hits,attacks,spawnTurn=-1,group;
        public NamedZombieRecord named;
        public bool enraged;
        public int received,envyDamage,envyStart=-1;
        public bool envyUsed;
    }
    [Serializable] public sealed class BattleState
    {
        public string id,scene,title,avatarName,outcome;
        public Sin legion;
        public Phase resume=Phase.Running;
        public int turn,enemyIndex=-1,avatarActions,lockedTarget=-1,serial,groupSerial;
        public bool waiting,defending,recovered,ended,won,retreated;
        public string prompt;
        public List<string> prompts=new List<string>();
        public List<BattleEnemy> enemies=new List<BattleEnemy>();
        public ChoiceEvent source;
        public int choice;
        public CombatMoment lastMoment,queuedMoment;
    }
    [Serializable] public sealed class NovemberConfig
    {
        // User delegated unresolved detail decisions on November 4. Centralized for tuning.
        public int[] dailyWeights={1,1};
        public int[] demonHp={300,300,260,310,360,270,330}, demonAttack={9,10,9,10,8,12,8};
        public int[] specialHp={70,55,50,55,75,45,80}, specialAttack={7,9,8,9,7,11,6};
        public double martFood=.70, otherFood=.02, supplyDiscovery=.60;
        public double residenceFood=.30, rerollEventChance=.25;
        public int residenceFoodEventWeight=2;
        public double crisisHp=.30, enemyCrisisHp=.25, boundaryLow=30,boundaryHigh=40;
        public double optionalEncounterChance=.35, noiseMultiplier=.8;
        public double injuryChance=.25;
        public int injuryHp=8, mentalCost=8, defaultEventHours=2;
        public bool envyIncludesTriggerTurn=true, spawnIncludesCreationTurn=false;
    }
    public static class AreaRules
    {
        public static readonly string[] Names={"쉘터","반파된 거주지","침수된 지하철역","박물관","폐마트","군사시설","종교시설","테마파크","문화유산","균열"};
        public static readonly int[] TravelHours={0,0,0,4,2,3,4,3,3,0};
        public static readonly Area[] Destinations={Area.Museum,Area.Mart,Area.Military,Area.Sanctuary,Area.ThemePark,Area.Heritage};
        public static string Name(Area area){return Names[(int)area];}
        public static string Scene(Area area){return area==Area.Rift?"rift":area==Area.Sanctuary?"chapel":area==Area.Residence?"ruins":"area:"+(int)area;}
    }
}
