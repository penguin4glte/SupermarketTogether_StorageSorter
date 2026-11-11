using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StorageSorter
{
    internal static class StorageSortRunner
    {
        public static bool IsSorting { get; private set; }

        private sealed class SlotRef
        {
            public Data_Container Container;
            public int Index;
        }

        public static IEnumerator Run()
        {
            IsSorting = true;
            List<KeyValuePair<SlotInfo, SlotContent>> toFill = null;
            PlanResult plan = null;
            bool proceed = false;
            var cleared = new List<SlotInfo>();
            try
            {
                var slots = Snapshot();
                if (slots.Count == 0)
                {
                    Notifier.Show(Loc.T("no_shelves"));
                    yield break;
                }

                var groups = BuildProductGroups();
                var products = ProductListing.Instance.productsData;
                plan = StoragePlanner.Plan(slots,
                    pid => groups.TryGetValue(pid, out var g) ? g : int.MaxValue,
                    pid => products[pid].maxItemsPerBox,
                    Plugin.MergeBoxes.Value);

                if (plan.Error != null)
                {
                    Plugin.Log.LogError("Sort aborted: " + plan.Error);
                    Notifier.Show(Loc.T("aborted"));
                    yield break;
                }
                if (plan.ChangedSlots == 0)
                {
                    Notifier.Show(Loc.T("already_sorted"));
                    yield break;
                }

                // Phase 1: clear every slot whose content changes. (The game only rebuilds the box
                // model when a slot goes empty -> filled, so a direct swap would leave a wrong-looking box.)
                toFill = new List<KeyValuePair<SlotInfo, SlotContent>>();
                foreach (var kv in plan.Target)
                {
                    var s = kv.Key;
                    var cur = new SlotContent(s.Pid, s.Count);
                    if (kv.Value.Equals(cur)) continue;
                    if (!cur.IsEmpty) { cleared.Add(s); Write(s, SlotContent.Empty); }
                    if (!kv.Value.IsEmpty) toFill.Add(kv);
                }
                proceed = true;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Sort failed: " + ex);
                // Put back anything we already emptied so no boxes are lost.
                foreach (var s in cleared)
                {
                    try { Write(s, new SlotContent(s.Pid, s.Count)); }
                    catch (Exception ex2) { Plugin.Log.LogError("Restore failed: " + ex2.Message); }
                }
                Notifier.Show(Loc.T("error"));
            }
            finally
            {
                if (!proceed) IsSorting = false;
            }
            if (!proceed) yield break;

            // Give the host and every client time to destroy the old box models.
            yield return new WaitForSecondsRealtime(0.3f);

            try
            {
                // Phase 2: fill. If someone sneaked a box into a cleared slot, put ours elsewhere.
                var homeless = new List<SlotContent>();
                foreach (var kv in toFill)
                {
                    if (Read(kv.Key).IsEmpty) Write(kv.Key, kv.Value);
                    else homeless.Add(kv.Value);
                }
                int lost = 0;
                if (homeless.Count > 0)
                {
                    var spare = plan.Target.Keys.Where(s => Read(s).IsEmpty).ToList();
                    foreach (var box in homeless)
                    {
                        if (spare.Count == 0) { lost++; continue; }
                        Write(spare[0], box);
                        spare.RemoveAt(0);
                    }
                    Plugin.Log.LogWarning($"{homeless.Count} box(es) re-homed because their slot was taken during the sort; {lost} could not be placed.");
                }

                string merged = plan.BoxesAfter < plan.BoxesBefore ? Loc.T("sorted.merged", plan.BoxesBefore, plan.BoxesAfter) : "";
                string cat = Plugin.UseCategoryShelves.Value && OaoBridge.Available ? Loc.T("sorted.category") : "";
                Notifier.Show(Loc.T("sorted", plan.ChangedSlots, cat, merged));
                Plugin.Log.LogInfo($"Sorted storage: boxes {plan.BoxesBefore}->{plan.BoxesAfter}, units {plan.UnitsBefore}, changed {plan.ChangedSlots}, lost {lost}");
                if (lost > 0)
                    Notifier.Show(Loc.T("lost", lost));
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Sort failed during refill: " + ex);
                Notifier.Show(Loc.T("error"));
            }
            finally
            {
                IsSorting = false;
            }
        }

        private static List<SlotInfo> Snapshot()
        {
            var list = new List<SlotInfo>();
            var npc = NPC_Manager.Instance;
            var root = npc.storageOBJ.transform;
            var refPoint = npc.storageDistanceReferencePoint;
            int productCount = ProductListing.Instance.productsData.Length;
            bool useCats = Plugin.UseCategoryShelves.Value;

            var shelves = new List<Data_Container>();
            for (int i = 0; i < root.childCount; i++)
            {
                var c = root.GetChild(i).GetComponent<Data_Container>();
                if (c != null && c.containerClass == 69 && c.transform.Find("BoxContainer") != null)
                    shelves.Add(c);
            }
            shelves = shelves.OrderBy(c => Vector3.Distance(refPoint, c.transform.position)).ToList();

            for (int order = 0; order < shelves.Count; order++)
            {
                var c = shelves[order];
                int? cat = useCats ? OaoBridge.GetCategory(c) : null;
                var arr = c.productInfoArray;
                var boxContainer = c.transform.Find("BoxContainer");
                int n = Math.Min(arr.Length / 2, boxContainer.childCount);
                for (int i = 0; i < n; i++)
                {
                    int pid = arr[i * 2], count = arr[i * 2 + 1];
                    list.Add(new SlotInfo
                    {
                        ShelfOrder = order,
                        SlotIndex = i,
                        Category = cat,
                        Pid = pid,
                        Count = count,
                        Locked = pid >= productCount || pid < -1,
                        Tag = new SlotRef { Container = c, Index = i },
                    });
                }
            }
            return list;
        }

        /// <summary>product id -> category group, using the same tier table the ordering UI uses.</summary>
        private static Dictionary<int, int> BuildProductGroups()
        {
            var map = new Dictionary<int, int>();
            var pl = ProductListing.Instance;
            if (pl.tiers == null || pl.productGroups == null) return map;
            for (int i = 0; i < pl.tiers.Length && i < pl.productGroups.Length; i++)
            {
                var parts = (pl.tiers[i] ?? "").Split('-');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int a) || !int.TryParse(parts[1], out int b)) continue;
                int g = pl.productGroups[i];
                if (g < 0) continue;
                for (int p = a; p <= b; p++) map[p] = g;
            }
            return map;
        }

        private static SlotContent Read(SlotInfo s)
        {
            var r = (SlotRef)s.Tag;
            if (r.Container == null) return new SlotContent(int.MinValue, int.MinValue);
            var arr = r.Container.productInfoArray;
            return new SlotContent(arr[r.Index * 2], arr[r.Index * 2 + 1]);
        }

        private static void Write(SlotInfo s, SlotContent v)
        {
            var r = (SlotRef)s.Tag;
            if (r.Container == null) return;
            // Server-side setter used by the game's own employees; syncs to all clients via RPC.
            r.Container.EmployeeUpdateArrayValuesStorage(r.Index * 2, v.Pid, v.Count);
        }
    }
}
