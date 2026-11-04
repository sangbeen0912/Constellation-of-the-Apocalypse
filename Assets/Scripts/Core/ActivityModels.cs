using System;
using System.Collections.Generic;
namespace Apocalypse
{
    [Serializable] public sealed class EventCost
    {
        public string id;
        public double[] hours, hunger, sanity;
        public int[] food, kits, normal, special;
    }
    [Serializable] public sealed class EventOption
    {
        public string text, relicId, rareNpcId, chance;
        public double hours, hunger, sanity;
        public int food, kits, syringes, normal, special;
        public bool recruit, discoverFood, divinity, returnHome, boss;
    }
    [Serializable] public sealed class ActivityEvent
    {
        public string id, title, body, relicId, rareNpcId, scene;
        public Goal location;
        public bool divinityEvent;
        public EventOption[] options;
        public Sin[] tags;
    }
    [Serializable] public sealed class RelicDefinition { public string id, name, description; public Goal location; }
    [Serializable] public sealed class RareNpcDefinition { public string id, name, passive; public Species species; public int health, attack; }
    [Serializable] public sealed class SupplyDefinition { public string id, name; public int amount; }
    [Serializable] public sealed class SupplyStack { public string id; public int amount; }
    [Serializable] public sealed class LegacyRelicRecord { public string id, name; public int previousAttackBonus; }
    [Serializable] public sealed class RelicChapter
    {
        public string userId, name;
        public int startDay, endDay, days, normalKills, specialKills, namedKills, demonKills, lastCountedDay;
        public bool deceased;
    }
    public struct RestorationPreview { public int health, attack, carryDays, carryZombies, demons; }
    public struct TreatmentPreview { public double hours, health, sanity, healthAfter, sanityAfter; public bool available; public string reason; }
}
