using System;
using System.Collections.Generic;
using System.Linq;
using StorageSorter;

int fails = 0;
void Check(bool cond, string name) { Console.WriteLine((cond ? "PASS " : "FAIL ") + name); if (!cond) fails++; }

// products: group = pid / 10 ; max per box = 10
Func<int, int> group = p => p / 10;
Func<int, int> max = p => 10;

List<SlotInfo> Shelf(int order, int? cat, params (int pid, int cnt)[] slots)
    => slots.Select((s, i) => new SlotInfo { ShelfOrder = order, SlotIndex = i, Category = cat, Pid = s.pid, Count = s.cnt }).ToList();
string Dump(PlanResult r, IEnumerable<SlotInfo> slots) => string.Join(" ", slots.Select(s => r.Target.TryGetValue(s, out var v) ? v.ToString() : "LOCK"));

// 1. basic sort + merge across two shelves
{
    var a = Shelf(0, null, (21, 5), (-1, -1), (3, 10));
    var b = Shelf(1, null, (21, 7), (3, 4), (12, -1)); // (12,-1) = label only
    var all = a.Concat(b).ToList();
    var r = StoragePlanner.Plan(all, group, max, merge: true);
    Console.WriteLine("  " + Dump(r, all));
    Check(r.Error == null, "basic: no error");
    Check(r.UnitsBefore == 26 && r.UnitsAfter == 26, "basic: units conserved");
    Check(r.BoxesBefore == 4 && r.BoxesAfter == 4, "basic: 4 boxes (3:10+4 -> 10+4, 21:5+7 -> 10+2)");
    Check(r.Target[a[0]].Equals(new SlotContent(3, 10)) && r.Target[a[1]].Equals(new SlotContent(3, 4))
          && r.Target[a[2]].Equals(new SlotContent(21, 10)) && r.Target[b[0]].Equals(new SlotContent(21, 2)), "basic: order category->pid->fuller first");
    Check(r.Target[b[2]].IsEmpty, "basic: label-only slot becomes empty");
}
// 2. merge reduces box count
{
    var s = Shelf(0, null, (5, 3), (5, 3), (5, 3), (5, 3));
    var r = StoragePlanner.Plan(s, group, max, true);
    Console.WriteLine("  " + Dump(r, s));
    Check(r.BoxesAfter == 2 && r.UnitsAfter == 12, "merge: 4x3 -> 10+2");
    var r2 = StoragePlanner.Plan(s, group, max, false);
    Check(r2.BoxesAfter == 4 && r2.ChangedSlots == 0, "no-merge: unchanged");
}
// 3. category shelves take their own group first, overflow to untagged, then other-tagged
{
    var tagged1 = Shelf(0, 1, (-1, -1), (-1, -1));          // wants group 1 (pids 10-19)
    var untagged = Shelf(1, null, (11, 10), (12, 10), (2, 10));
    var tagged0 = Shelf(2, 0, (13, 10), (-1, -1));           // wants group 0 (pids 0-9)
    var all = tagged1.Concat(untagged).Concat(tagged0).ToList();
    var r = StoragePlanner.Plan(all, group, max, true);
    Console.WriteLine("  " + Dump(r, all));
    Check(r.Error == null && r.UnitsAfter == 40, "cat: units conserved");
    Check(tagged1.All(x => group(r.Target[x].Pid) == 1), "cat: group-1 shelf filled with group 1");
    Check(group(r.Target[tagged0[0]].Pid) == 0, "cat: group-0 shelf gets the group-0 box");
    Check(r.Target[untagged[0]].Pid == 13, "cat: leftover group-1 box goes to untagged shelf");
}
// 4. overflow onto other-category shelf when nothing else has space
{
    var t0 = Shelf(0, 0, (11, 10), (12, 10));   // tagged group 0 but holding group 1 boxes
    var r = StoragePlanner.Plan(t0, group, max, true);
    Check(r.Error == null && r.Target.Values.Count(v => !v.IsEmpty) == 2, "overflow: boxes kept on wrong-category shelf");
}
// 5. locked slot is untouched and not counted
{
    var s = Shelf(0, null, (999, 5), (3, 1), (-1, -1));
    s[0].Locked = true;
    var r = StoragePlanner.Plan(s, group, max, true);
    Check(!r.Target.ContainsKey(s[0]) && r.UnitsAfter == 1, "locked: excluded");
}
// 6. empty boxes (x0) are kept
{
    var s = Shelf(0, null, (4, 0), (4, 6), (-1, -1));
    var r = StoragePlanner.Plan(s, group, max, true);
    Console.WriteLine("  " + Dump(r, s));
    Check(r.BoxesAfter == 2 && r.Target[s[0]].Equals(new SlotContent(4, 6)) && r.Target[s[1]].Equals(new SlotContent(4, 0)), "empty box kept, after full ones");
}
// 7. fuzz: conservation & capacity
{
    var rng = new Random(1);
    bool ok = true;
    for (int it = 0; it < 3000 && ok; it++)
    {
        var all = new List<SlotInfo>();
        int shelves = rng.Next(1, 8);
        for (int sh = 0; sh < shelves; sh++)
        {
            int? cat = rng.Next(3) == 0 ? rng.Next(0, 4) : (int?)null;
            int n = rng.Next(1, 9);
            for (int i = 0; i < n; i++)
            {
                int kind = rng.Next(4);
                var (pid, cnt) = kind switch { 0 => (-1, -1), 1 => (rng.Next(40), -1), _ => (rng.Next(40), rng.Next(0, 11)) };
                all.Add(new SlotInfo { ShelfOrder = sh, SlotIndex = i, Category = cat, Pid = pid, Count = cnt });
            }
        }
        bool merge = rng.Next(2) == 0;
        var r = StoragePlanner.Plan(all, group, max, merge);
        var before = all.Where(s => s.HasBox).GroupBy(s => s.Pid).ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        var after = r.Target.Values.Where(v => !v.IsEmpty).GroupBy(v => v.Pid).ToDictionary(g => g.Key, g => g.Sum(x => x.Count));
        bool perProduct = before.Count == after.Count && before.All(kv => after.TryGetValue(kv.Key, out var v) && v == kv.Value);
        bool noGrowth = r.BoxesAfter <= r.BoxesBefore;
        bool noLabels = r.Target.Values.All(v => v.IsEmpty || v.Count >= 0);
        bool sameMultiset = merge || all.Where(s => s.HasBox).Select(s => (s.Pid, s.Count)).OrderBy(x => x).SequenceEqual(
                                      r.Target.Values.Where(v => !v.IsEmpty).Select(v => (v.Pid, v.Count)).OrderBy(x => x));
        ok = r.Error == null && perProduct && noGrowth && noLabels && sameMultiset;
        if (!ok) Console.WriteLine($"  fuzz fail at {it}: err={r.Error} perProduct={perProduct} noGrowth={noGrowth} noLabels={noLabels} multiset={sameMultiset}");
    }
    Check(ok, "fuzz: 3000 random storages keep per-product units, never add boxes");
}
Console.WriteLine(fails == 0 ? "ALL PASS" : $"{fails} FAILED");
return fails;
