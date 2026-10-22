// Agent perf helper (eval via tools/test/perf_park.sh): averages the last 10 profiler frames and lists the slowest
// script calls ("[Invoke]") and WaveWater steps.
int last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
if (last < 0) return "no frames";
var totals = new System.Collections.Generic.Dictionary<string, double>();
int frames = 0;
for (int f = System.Math.Max(UnityEditorInternal.ProfilerDriver.firstFrameIndex, last - 9); f <= last; f++) {
  using (var view = UnityEditorInternal.ProfilerDriver.GetHierarchyFrameDataView(f, 0, UnityEditor.Profiling.HierarchyFrameDataView.ViewModes.MergeSamplesWithTheSameName, UnityEditor.Profiling.HierarchyFrameDataView.columnTotalTime, false)) {
    if (view == null || !view.valid) continue;
    frames++;
    var stack = new System.Collections.Generic.List<int>();
    var kids = new System.Collections.Generic.List<int>();
    stack.Add(view.GetRootItemID());
    while (stack.Count > 0) {
      int id = stack[stack.Count - 1]; stack.RemoveAt(stack.Count - 1);
      view.GetItemChildren(id, kids);
      foreach (var k in kids) {
        string name = view.GetItemName(k);
        if (name.EndsWith("[Invoke]") || name.StartsWith("SurfPool") || name.StartsWith("WaveWater.")) {
          double ms = view.GetItemColumnDataAsDouble(k, UnityEditor.Profiling.HierarchyFrameDataView.columnTotalTime);
          totals[name] = (totals.TryGetValue(name, out var v) ? v : 0) + ms;
        }
        stack.Add(k);
      }
    }
  }
}
var rows = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, double>>(totals);
rows.Sort((a, b) => b.Value.CompareTo(a.Value));
var sb = new System.Text.StringBuilder("frames=" + frames + "\n");
for (int i = 0; i < System.Math.Min(20, rows.Count); i++) sb.Append(rows[i].Key + ": " + (rows[i].Value / frames).ToString("0.00") + " ms\n");
return sb.ToString();
