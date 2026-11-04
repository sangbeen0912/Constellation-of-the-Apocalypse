using System;
using System.Collections.Generic;
namespace Apocalypse
{
    public enum CombatMomentKind { AvatarAttack, GroupAttack, Recovery, EnemyAttack, Summon, Absorb, Blocked, Reflect }
    public sealed class CombatEnemyView
    {
        public string name;
        public EnemyKind kind;
        public int health;
    }
    [Serializable] public sealed class CombatMoment
    {
        public CombatMomentKind kind;
        public int target, amount, actor=-1;
        public string message;
        public double health;
    }
    // Transient observation data. It contains no Unity types and is never a second simulation.
    public sealed class CombatReplay
    {
        public string avatarName, outcome, reason, scene, eventTitle;
        public Species species;
        public Goal location;
        public Sin legion;
        public double health, maxHealth, finalHealth, finalSanity, finalHunger;
        public bool died;
        public readonly List<CombatEnemyView> enemies=new List<CombatEnemyView>();
        public readonly List<CombatMoment> moments=new List<CombatMoment>();
    }
    public sealed partial class GameEngine
    {
        public event Action<CombatReplay> CombatRecorded;
        string combatScene, combatEventTitle;
    }
}
