#!/bin/zsh
# Agent perf test: a boosted level-30 park (every upgrade, auto Pro waves on every machine, a minute at 3x speed to
# fill up), the player on the main pool's south deck, then the per-script timings of the last 10 frames.
# Run INSIDE tools/test/play_begin.sh ... play_end.sh (it never touches saves itself).
# Reads profiler frames with tools/test/perf_read.cs (WaveWater steps have their own markers).
U=~/.unity/bin/unity
DIR=${0:a:h}
$U command eval 'SurfPool.ParkLevel.Instance.Restore(SurfPool.ParkLevel.XpForLevel(30)); SurfPool.Wallet.Instance.Add(5000000, "test"); var shop = UnityEngine.Object.FindAnyObjectByType<SurfPool.UpgradeShop>(); for (int pass = 0; pass < 3; pass++) foreach (var u in shop.Upgrades) if (!u.owned) shop.Buy(u.id); shop.Close(); foreach (var m in UnityEngine.Object.FindObjectsByType<SurfPool.WaveMachine>(UnityEngine.FindObjectsSortMode.None)) { if (m.AutoInterval <= 0f) m.AutoInterval = 12f; m.Select(SurfPool.WaveLevel.Pro); } UnityEngine.Time.timeScale = 3f; return "boosted";' --result-only --timeout 120 | grep -o '"result.*'
sleep 60
$U command eval 'UnityEngine.Time.timeScale = 1f; var p = UnityEngine.Object.FindAnyObjectByType<SurfPool.FirstPersonController>(); var cc = p.GetComponent<UnityEngine.CharacterController>(); cc.enabled = false; p.transform.SetPositionAndRotation(new UnityEngine.Vector3(20f, 0.1f, -16f), UnityEngine.Quaternion.Euler(0f, 0f, 0f)); cc.enabled = true; UnityEditorInternal.ProfilerDriver.enabled = true; return "surfers=" + SurfPool.Surfer.All.Count;' --result-only | grep -o '"result.*'
sleep 4
$U command eval "$(cat $DIR/perf_read.cs)" --result-only --timeout 60 | grep -o '"result.*' | sed 's/\\n/\n/g'
$U command eval 'UnityEditorInternal.ProfilerDriver.enabled = false; return "profiler off";' --result-only | grep -o '"result.*'
