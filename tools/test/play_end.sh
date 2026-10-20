#!/bin/zsh
# Agent playtest end: stops Play mode, removes the save the test made, and moves Ido's save back.
# Does nothing to any save unless play_begin.sh started a test (marker file present).
SAVE_DIR="$HOME/Library/Application Support/DefaultCompany/surf-pool-sim"
SAVE="$SAVE_DIR/surfpool_save.json"
PARKED="$SAVE_DIR/surfpool_save.ido-backup.json"
MARK="$SAVE_DIR/.agent-test-running"
U=~/.unity/bin/unity
$U command editor_stop | tail -1
sleep 1
if [ -f "$MARK" ]; then
  rm -f "$SAVE"                                   # the test's own save
  if [ -f "$PARKED" ]; then mv "$PARKED" "$SAVE"; echo "put Ido's save back"; fi
  rm -f "$MARK"
fi
$U command eval 'UnityEditor.EditorSettings.asyncShaderCompilation=true; return "ok";' --result-only >/dev/null
