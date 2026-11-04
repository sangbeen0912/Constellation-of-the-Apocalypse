using System;
using System.Collections.Generic;
using System.Linq;

namespace Apocalypse
{
    public enum Sin { Pride, Greed, Lust, Envy, Gluttony, Wrath, Sloth }
    public enum Phase { Selection, Morning, Running, Event, Report, Death, Victory, Combat }
    // Keep persisted numeric IDs from v1. Removed goals 1/2 migrate to Explore.
    public enum Goal { Explore = 0, Rift = 3 }
    public enum EnemyKind { Normal, Special, Named, Demon }
    public enum Activity { Sleep, Rest, Travel, Search, Scout, Combat, Rift }
    public enum Temperament { Brave=0, Cautious=1, Cold=2, Defiant=3, Aggressive=4, Fearful=5 }
    public enum Stat { Health, Attack }
    public enum Species { Human, Machine, Fishfolk, HalfUndead, Beastkin }

    [Serializable] public sealed class Balance
    {
        // All values in this class are provisional except the separately declared Rules.
        public int initialFood = 220, initialDivinity = 80, initialMedicine = 4;
        public int healthMin = 85, healthMax = 115, attackMin = 16, attackMax = 24;
        public double[] hungerRates = { 1, 2, 3, 4, 5, 7, 8 };
        public double combatRoundHours = .15, minBattleHours = .5;
        public int maxCombatRounds = 200, normalHp = 30, specialHp = 55, demonHp = 220;
        public int normalAttack = 5, specialAttack = 7, demonAttack = 13;
        public double normalStress = 3, namedStress = 8, demonStress = 15;
        public double constellationStoryHours = 0;
        public double scoutSuccess = .60, mutantChance = .12, refusalChance = .18;
        // Unconfirmed distribution: no implicit uniform roll. Configurable minimum fallback.
        public int[] dailyEventCountWeights = new int[0];
        public int dailyEventCountFallback = 1, initialSyringes = 0;
        public double foodDiscoveryChance = .6, divineSuccessChance = .6, constellationMeetDelay = 3;
        public double returnHealthRatio = .3, returnSanity = 35, returnHunger = 30, returnHour = 18, returnRiskPerSeal = .02;
        public double[] returnPersonalityOffsets = { -.05, .1, 0, -.1 };
        public NovemberConfig november = new NovemberConfig();
        public EventCost[] eventCosts = new EventCost[0];
        public double sejongReduction = .15, joanReduction = .25, napoleonReduction = .10;
        public double zhugeTime = .10, leonardoLoot = .15, alexanderDamage = .12;
        public double teslaChance = .10, teslaDamage = .30, huaHeal = .15, leonidasReduction = .15;
        public double musashiDamage = .15, curieChance = .10, curieDamage = .40, cleopatraScout = .15;
        public double yiReduction = .30, genghisDamage = .30, artemisDamage = .35;
        public double heraclesDamage = .40, arthurDamage = .35, zeusFirst = 2.50, hadesReward = 1;
        public int riftCombatWeight = 80, exploreCombatWeight = 8;
    }
    public static class Rules
    {
        public const int SaveVersion = 1, RulesRevision = 4, MaxNpc = 10, SealDays = 100, EnhancedSealDays = 120, RelicCost = 100, SupplyCost = 1;
        public const int RiftProgressRequired = 50, NormalProgress = 1, SpecialProgress = 3, NamedProgress = 3;
        public const double NamedReplacementChance = .30;
        public const double HealthPerSeal = .5, AttackPerSeal = .2;
        public const double ConstellationChance = .3;
        public static readonly int[] GradeWeights = { 50, 35, 10, 5 };
        public static readonly int[] InheritCosts = { 30, 70, 150, 300 };
        public static readonly string[] Grades = { "위인급", "설화급", "신화급", "외신급" };
        public static readonly string[] Sins = { "오만", "탐욕", "색욕", "질투", "폭식", "분노", "나태" };
        public static int Round(double x) { return (int)Math.Round(x, MidpointRounding.AwayFromZero); }
        public static double Clamp(double x, double max = 100) { return Math.Max(0, Math.Min(max, x)); }
    }
    [Serializable] public sealed class Character
    {
        public string id, name;
        public int baseHealth, baseAttack, healthInvestment, attackInvestment, selectedDay;
        public double health, hunger = 100, sanity = 100;
        public Temperament personality;
        public bool mutant;
        public Species species;
        public int relicHealthBonus, relicAttackBonus;
        public List<string> inherited = new List<string>();
        public List<string> relics = new List<string>();
        public int MaxHealth { get { return Rules.Round(baseHealth * (1 + healthInvestment / 100.0)) + relicHealthBonus; } }
        public int Attack { get { return Rules.Round(baseAttack * (1 + attackInvestment / 100.0)) + relicAttackBonus; } }
        public bool Has(string id) { return inherited.Contains(id); }
    }
    [Serializable] public sealed class Constellation
    {
        public string id, eventId, name, constellationName, affiliation, ability, description, title, story;
        public int grade;
    }
    [Serializable] public sealed class ChoiceEvent
    {
        public string id, title, body, constellationId;
        public string[] choices;
        public Sin[] tags;
        public string templateId, scene, context;
        public EventOption[] options;
    }
    [Serializable] public sealed class Demon
    {
        public Sin sin;
        public int remaining, sealedDay;
        public int progress;
        public bool yogApplied;
        public int sealDuration;
    }
    public struct EnemyStats { public int health, attack; }
    [Serializable] public sealed class Relic
    {
        public string id, name;
        public int healthBonus, attackBonus, days, normalKills, specialKills, namedKills, demonKills;
        public int paidDays, paidZombies, paidDemons, restorations;
        public bool awaitingRestoration;
        public string currentUserId;
        public List<RelicChapter> history = new List<RelicChapter>();
        public int Zombies { get { return normalKills+specialKills+namedKills; } }
    }
    [Serializable] public sealed class NamedZombieRecord
    {
        public string id, name;
        public int health, attack;
        public Sin legion;
        public bool defeated;
    }
    [Serializable] public sealed class DeathRecord
    {
        public string name, cause;
        public int day, selectedDay;
        public bool combat;
        public Sin legion;
    }
    [Serializable] public sealed class PermanentData
    {
        public List<string> unlocked = new List<string>();
        public List<string> completedStories = new List<string>();
        public List<NamedZombieRecord> named = new List<NamedZombieRecord>();
        public List<DeathRecord> deaths = new List<DeathRecord>();
        public List<AcquiredStory> stories = new List<AcquiredStory>();
        public int victories;
    }
    [Serializable] public sealed class AcquiredStory
    {
        public string constellationId, title, body;
        public int day;
    }
    [Serializable] public sealed class GameState
    {
        public int version = Rules.SaveVersion;
        public int rulesRevision;
        public uint rng, seed;
        public string runId;
        public PermanentData permanent = new PermanentData();
        public Phase phase = Phase.Selection;
        public int day = 1, divinity, food, medicine, syringes, equipment, administrators;
        public int activeRift = -1, nextRiftDay = 2, settledDay, constellationRolledDay;
        public int eventSlots, eventSerial, normalWins, totalKills, totalEarned, totalSeals, totalActions;
        public int eventTarget, divineEventDay, omenShownDay;
        public string scheduledConstellation, returnReason;
        public double constellationMeetingHour;
        public bool returnRequested;
        public double hours, nextImportantHour = 6;
        public bool controlLocked, dayStarted;
        public Goal goal;
        public Character avatar;
        public List<Character> candidates = new List<Character>();
        public List<Demon> demons = new List<Demon>();
        public List<Relic> relics = new List<Relic>();
        public List<LegacyRelicRecord> legacyRelics = new List<LegacyRelicRecord>();
        public List<string> recruitedRare = new List<string>();
        public List<SupplyStack> additionalSupplies = new List<SupplyStack>();
        public List<string> rewardLedger = new List<string>();
        public List<string> resolvedEvents = new List<string>();
        public List<string> logs = new List<string>();
        public List<Sin> sanityCauses = new List<Sin>();
        public ChoiceEvent pending;
        public string report = "", lastDeath = "", lastBattle = "";
        public string lastNamedName;
        public int lastNamedDay;
        public ExplorationState exploration = new ExplorationState();
        public PlotState plot = new PlotState();
        public List<string> uniqueRecruits = new List<string>();
        public BattleState combat;
    }
    [Serializable] public sealed class Content
    {
        public List<Constellation> constellations = new List<Constellation>();
        public List<ActivityEvent> activityEvents = new List<ActivityEvent>();
        public List<RelicDefinition> relicDefinitions = new List<RelicDefinition>();
        public List<RareNpcDefinition> rareNpcs = new List<RareNpcDefinition>();
        public List<SupplyDefinition> supplies = new List<SupplyDefinition>();
        public Constellation Find(string id) { return constellations.First(x => x.id == id); }
    }
    public sealed class RandomStream
    {
        readonly GameState state;
        public RandomStream(GameState s) { state = s; }
        public double Next() { uint x = state.rng; x ^= x << 13; x ^= x >> 17; x ^= x << 5; state.rng = x; return x / 4294967296.0; }
        public int Int(int min, int maxExclusive) { return min + (int)(Next() * (maxExclusive - min)); }
        public T Pick<T>(IList<T> values) { return values[Int(0, values.Count)]; }
        public int Weighted(int[] weights)
        {
            int roll = Int(0, weights.Sum());
            for (int i = 0; i < weights.Length; i++) { roll -= weights[i]; if (roll < 0) return i; }
            throw new InvalidOperationException("Empty weight table");
        }
    }
}
