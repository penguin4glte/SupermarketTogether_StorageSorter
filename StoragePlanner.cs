using System;
using System.Collections.Generic;
using System.Linq;

namespace StorageSorter
{
    /// <summary>One storage slot as seen by the planner. No Unity types, so it can be unit-tested.</summary>
    public sealed class SlotInfo
    {
        public int ShelfOrder;      // 0 = nearest shelf
        public int SlotIndex;       // row index inside the shelf
        public int? Category;       // OAO category tag of the shelf, null = untagged
        public int Pid;             // productInfoArray[i*2]
        public int Count;           // productInfoArray[i*2+1]; -1 = no box (Pid>=0 then means "label only")
        public bool Locked;         // slot we must not touch (unknown product id etc.)
        public object Tag;          // caller data (container + index)

        public bool HasBox => Pid >= 0 && Count >= 0;
    }

    public struct SlotContent : IEquatable<SlotContent>
    {
        public int Pid, Count;
        public SlotContent(int pid, int count) { Pid = pid; Count = count; }
        public static readonly SlotContent Empty = new SlotContent(-1, -1);
        public bool IsEmpty => Pid == -1 && Count == -1;
        public bool Equals(SlotContent o) => Pid == o.Pid && Count == o.Count;
        public override bool Equals(object o) => o is SlotContent s && Equals(s);
        public override int GetHashCode() => Pid * 397 ^ Count;
        public override string ToString() => IsEmpty ? "(empty)" : $"P{Pid}x{Count}";
    }

    public sealed class PlanResult
    {
        public Dictionary<SlotInfo, SlotContent> Target = new Dictionary<SlotInfo, SlotContent>();
        public int BoxesBefore, BoxesAfter, UnitsBefore, UnitsAfter, ChangedSlots;
        public string Error;
    }

    public static class StoragePlanner
    {
        private sealed class Box
        {
            public int Pid, Count, Group;
        }

        /// <param name="groupOf">product id -> category group (or int.MaxValue if none)</param>
        /// <param name="maxPerBox">product id -> max items per box</param>
        public static PlanResult Plan(IList<SlotInfo> slots, Func<int, int> groupOf, Func<int, int> maxPerBox, bool merge)
        {
            var result = new PlanResult();
            var usable = slots.Where(s => !s.Locked).ToList();

            // 1. Collect every box currently in storage.
            var boxes = new List<Box>();
            foreach (var s in usable.Where(s => s.HasBox))
                boxes.Add(new Box { Pid = s.Pid, Count = s.Count, Group = groupOf(s.Pid) });
            result.BoxesBefore = boxes.Count;
            result.UnitsBefore = boxes.Sum(b => b.Count);

            // 2. Optionally merge partial boxes of the same product. Empty boxes (x0) are kept as-is.
            if (merge)
            {
                var merged = new List<Box>();
                foreach (var g in boxes.GroupBy(b => b.Pid))
                {
                    int max = Math.Max(1, maxPerBox(g.Key));
                    int total = g.Where(b => b.Count > 0).Sum(b => b.Count);
                    int group = g.First().Group;
                    int nonEmptyBefore = g.Count(b => b.Count > 0);
                    int needed = (total + max - 1) / max;
                    if (needed > nonEmptyBefore)
                    {
                        // Some boxes were over the limit; never create extra boxes — keep originals.
                        merged.AddRange(g.Where(b => b.Count > 0));
                    }
                    else
                    {
                        while (total > 0)
                        {
                            int c = Math.Min(max, total);
                            merged.Add(new Box { Pid = g.Key, Count = c, Group = group });
                            total -= c;
                        }
                    }
                    merged.AddRange(g.Where(b => b.Count == 0));
                }
                boxes = merged;
            }

            // 3. Sort: category -> product id -> fuller boxes first (empty boxes last within product).
            boxes = boxes.OrderBy(b => b.Group).ThenBy(b => b.Pid).ThenByDescending(b => b.Count).ToList();
            result.BoxesAfter = boxes.Count;
            result.UnitsAfter = boxes.Sum(b => b.Count);

            // 4. Place.
            var shelves = usable.GroupBy(s => s.ShelfOrder).OrderBy(g => g.Key)
                .Select(g => g.OrderBy(s => s.SlotIndex).ToList()).ToList();
            foreach (var s in usable) result.Target[s] = SlotContent.Empty;

            var remaining = new LinkedList<Box>(boxes);
            var freeSlots = new Dictionary<List<SlotInfo>, int>(); // next free index per shelf
            foreach (var sh in shelves) freeSlots[sh] = 0;

            void Put(List<SlotInfo> shelf, Box b)
            {
                int i = freeSlots[shelf]++;
                result.Target[shelf[i]] = new SlotContent(b.Pid, b.Count);
            }

            // 4a. Tagged shelves take boxes of their own category first.
            foreach (var sh in shelves.Where(sh => sh[0].Category.HasValue))
            {
                int cat = sh[0].Category.Value;
                for (var n = remaining.First; n != null && freeSlots[sh] < sh.Count;)
                {
                    var next = n.Next;
                    if (n.Value.Group == cat) { Put(sh, n.Value); remaining.Remove(n); }
                    n = next;
                }
            }
            // 4b. Untagged shelves, then 4c. leftover space on other-category shelves.
            foreach (var pass in new[] { false, true })
            {
                foreach (var sh in shelves.Where(sh => sh[0].Category.HasValue == pass))
                {
                    while (remaining.Count > 0 && freeSlots[sh] < sh.Count)
                    {
                        Put(sh, remaining.First.Value);
                        remaining.RemoveFirst();
                    }
                }
            }

            if (remaining.Count > 0)
            {
                result.Error = $"{remaining.Count} box(es) did not fit (this should not happen).";
                return result;
            }
            if (result.UnitsAfter != result.UnitsBefore)
            {
                result.Error = $"Unit count mismatch {result.UnitsBefore} -> {result.UnitsAfter}.";
                return result;
            }

            result.ChangedSlots = usable.Count(s => !result.Target[s].Equals(new SlotContent(s.Pid, s.Count)));
            return result;
        }
    }
}
