using System.Collections.Generic;
using System.Linq;

namespace Apocalypse
{
    public sealed class CodexIndex
    {
        readonly bool[] expanded = { true, true, true, true };
        public string SelectedId { get; private set; }
        public bool IsExpanded(int grade) { return expanded[grade]; }
        public void Toggle(int grade) { expanded[grade] = !expanded[grade]; }
        public IEnumerable<Constellation> Entries(Content content, PermanentData permanent, int grade)
        { return content.constellations.Where(x => x.grade == grade && permanent.unlocked.Contains(x.id)); }
        public bool Select(string id, PermanentData permanent)
        { if (!permanent.unlocked.Contains(id)) return false; SelectedId = id; return true; }
        public void Back() { SelectedId = null; }
        public static string DisplayName(Constellation c)
        { return c.name + "  " + (string.IsNullOrEmpty(c.constellationName) ? c.title : c.constellationName) + " / " + Rules.Grades[c.grade]; }
    }
}
