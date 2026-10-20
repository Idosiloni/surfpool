#!/bin/zsh
# Agent playtest start: moves Ido's save aside (never deletes it), enters Play mode, closes the title screen and letter.
# Always pair with play_end.sh. Safe to run twice: it only moves the save aside if no test is already running.
SAVE_DIR="$HOME/Library/Application Support/DefaultCompany/surf-pool-sim"
SAVE="$SAVE_DIR/surfpool_save.json"
PARKED="$SAVE_DIR/surfpool_save.ido-backup.json"
MARK="$SAVE_DIR/.agent-test-running"
U=~/.unity/bin/unity
if [ ! -f "$MARK" ]; then
  touch "$MARK"
  if [ -f "$SAVE" ]; then mv "$SAVE" "$PARKED"; echo "moved Ido's save aside"; fi
fi
$U command eval 'UnityEditor.EditorSettings.asyncShaderCompilation=false; return "ok";' --result-only >/dev/null
$U command editor_focus >/dev/null
$U command editor_play | tail -1
sleep 5
$U command eval 'var mm = UnityEngine.Object.FindAnyObjectByType<SurfPool.MainMenu>(); if (SurfPool.MainMenu.Showing) typeof(SurfPool.MainMenu).GetMethod("Close", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(mm, null); var ss = UnityEngine.Object.FindAnyObjectByType<SurfPool.StoryScreen>(); if (ss != null && ss.IsOpen) ss.Close(); return "menu+story closed";' --result-only | grep -o '"result.*'
