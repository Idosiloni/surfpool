using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Sets import settings for the generated WAVs (tools/audio/make_sounds.py) and fills the SoundBank asset
    /// that <see cref="Sfx"/> plays from. Called by the scene builder.
    /// </summary>
    public static class AudioSetup
    {
        const string Folder = "Assets/_Game/Audio";
        const string BankPath = Folder + "/SoundBank.asset";

        public static SoundBank Bank()
        {
            ConfigureImports();
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(BankPath);
            if (bank == null)
            {
                bank = ScriptableObject.CreateInstance<SoundBank>();
                AssetDatabase.CreateAsset(bank, BankPath);
            }

            bank.entries = new List<SoundBank.Entry>
            {
                Ui(Sound.Click, 0.35f, "ui_click"),
                Ui(Sound.Error, 0.5f, "ui_error"),
                Ui(Sound.Buy, 0.7f, "ui_buy"),
                Ui(Sound.Coin, 0.45f, "coin_1", "coin_2", "coin_3"),
                Ui(Sound.CashBig, 0.6f, "cash_big"),
                Ui(Sound.Mission, 0.8f, "mission_complete"),
                Ui(Sound.DayEnd, 0.7f, "day_end"),
                World(Sound.WaveMachine, 0.8f, 6f, 45f, 0.08f, "wave_machine"),
                World(Sound.CrashSmall, 0.75f, 8f, 55f, 0.08f, "wave_crash_small"),
                World(Sound.CrashMedium, 0.85f, 10f, 65f, 0.08f, "wave_crash_medium"),
                World(Sound.CrashBig, 1f, 12f, 80f, 0.08f, "wave_crash_big"),
                World(Sound.Splash, 0.6f, 4f, 30f, 0.08f, "splash_1", "splash_2"),
                Ui(Sound.SplashBig, 0.7f, "splash_big"),
                World(Sound.Cheer, 0.45f, 8f, 50f, 1.5f, "cheer_1", "cheer_2"),
                World(Sound.Whistle, 0.5f, 6f, 50f, 3f, "whistle"),
                World(Sound.Seagull, 0.3f, 10f, 60f, 2f, "seagull_1", "seagull_2", "seagull_3"),
                World(Sound.PaChime, 0.55f, 6f, 70f, 2f, "pa_chime"),
                World(Sound.PaVoice, 0.6f, 6f, 70f, 2f, "pa_voice_1", "pa_voice_2", "pa_voice_3"),
                Ui(Sound.FanfareLevel, 0.85f, "fanfare_level"),
                Ui(Sound.FanfarePool, 0.9f, "fanfare_pool"),
                World(Sound.FireworkLaunch, 0.5f, 15f, 140f, 0.05f, "firework_launch_1", "firework_launch_2"),
                World(Sound.FireworkBang, 0.9f, 30f, 260f, 0.05f, "firework_bang_1", "firework_bang_2", "firework_bang_3"),
                World(Sound.Plop, 0.5f, 2f, 15f, 0.03f, "plop_1", "plop_2", "plop_3"),
                World(Sound.Blender, 0.5f, 3f, 20f, 1f, "blender"),
                World(Sound.ShopBell, 0.55f, 3f, 25f, 0.1f, "shop_bell"),
                World(Sound.ScanBeep, 0.45f, 2f, 15f, 0.05f, "scan_beep"),
                World(Sound.Planer, 0.5f, 3f, 25f, 0.9f, "planer"),
                World(Sound.SprayHiss, 0.45f, 3f, 20f, 0.5f, "spray_hiss"),
                World(Sound.AirBlast, 0.8f, 8f, 70f, 0.05f, "air_blast"),
            };
            bank.surfLoop = Clip("amb_surf_loop");
            bank.crowdLoop = Clip("amb_crowd_loop");
            bank.crowdBigLoop = Clip("amb_crowd_big_loop");
            bank.windLoop = Clip("amb_wind_loop");
            bank.music = new[] { Clip("music_sunny"), Clip("music_lazy"), Clip("music_sunset") };
            bank.partyMusic = new[] { Clip("music_party") };
            bank.jetSkiLoop = Clip("loop_jetski");
            bank.heliLoop = Clip("loop_heli");
            EditorUtility.SetDirty(bank);
            AssetDatabase.SaveAssets();
            return bank;
        }

        static SoundBank.Entry Ui(Sound id, float volume, params string[] names) =>
            new() { id = id, volume = volume, spatial = false, clips = names.Select(Clip).ToArray() };

        static SoundBank.Entry World(Sound id, float volume, float min, float max, float gap, params string[] names) =>
            new() { id = id, volume = volume, spatial = true, minDistance = min, maxDistance = max, minGap = gap, clips = names.Select(Clip).ToArray() };

        static AudioClip Clip(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}.wav");
            if (clip == null) Debug.LogWarning($"[AudioSetup] Missing {name}.wav - run tools/audio/make_sounds.py");
            return clip;
        }

        /// <summary>Long beds and music stream from disk; short effects load decompressed for instant playback.</summary>
        static void ConfigureImports()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is not AudioImporter importer) continue;
                bool longSound = path.Contains("/music_") || path.Contains("/amb_");
                var settings = importer.defaultSampleSettings;
                settings.loadType = longSound ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                settings.compressionFormat = longSound ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                settings.quality = 0.7f;
                if (settings.loadType == importer.defaultSampleSettings.loadType &&
                    settings.compressionFormat == importer.defaultSampleSettings.compressionFormat) continue;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
        }
    }
}
