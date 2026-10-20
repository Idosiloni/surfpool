using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// Pause menu (Tab): controls cheat sheet, park stats, sound volumes, and Resume / Save / New game.
    /// New game deletes the save and reloads the scene. (UI: Assets/_Game/UI/Pause.uxml)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] SaveSystem saveSystem;
        [SerializeField] SurferSpawner spawner;

        const string Controls =
            "WASD  walk  ·  Shift  sprint  ·  Space  jump\n" +
            "E  use whatever you look at (register, computer, wave console, boards...)\n" +
            "B  build mode: decorate the park (1-0 pick, R rotate, click place, X sell back)\n" +
            "Esc  free the mouse  ·  Tab  this menu\n\n" +
            "Surfing: E on your board by a wave console. W paddle, Space stand up when a wave reaches you, " +
            "A/D carve, W/S up and down the face, Space air, F send a wave, Q get out.";

        VisualElement root, stats;
        Label subtitle;
        bool open;
        int openedFrame;

        void OnEnable()
        {
            var doc = GetComponent<UIDocument>().rootVisualElement;
            root = doc.Q("pauseRoot");
            stats = doc.Q("pauseStats");
            subtitle = doc.Q<Label>("pauseSubtitle");
            doc.Q<Label>("pauseControls").text = Controls;
            doc.Q<Button>("pauseResume").clicked += Close;
            doc.Q<Button>("pauseSave").clicked += () =>
            {
                saveSystem.Save();
                Notifications.Post("Game saved.");
            };
            doc.Q<Button>("pauseNew").clicked += NewGame;
            doc.Q<Button>("pauseSettings").clicked += SettingsScreen.Open;
            doc.Q<Button>("pauseMenu").clicked += QuitToMenu;

            var music = doc.Q<Slider>("volMusic");
            var effects = doc.Q<Slider>("volEffects");
            var ambience = doc.Q<Slider>("volAmbience");
            music.SetValueWithoutNotify(Sfx.MusicVolume);
            effects.SetValueWithoutNotify(Sfx.EffectsVolume);
            ambience.SetValueWithoutNotify(Sfx.AmbienceVolume);
            EventCallback<ChangeEvent<float>> changed = _ => Sfx.SetVolumes(music.value, effects.value, ambience.value);
            music.RegisterValueChangedCallback(changed);
            effects.RegisterValueChangedCallback(changed);
            ambience.RegisterValueChangedCallback(changed);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (open)
            {
                if (UnityEngine.Cursor.lockState == CursorLockMode.Locked) FirstPersonController.LockCursor(false);
                if (Time.frameCount != openedFrame && keyboard.tabKey.wasPressedThisFrame) Close();
                return;
            }
            if (keyboard.tabKey.wasPressedThisFrame && !InputBlocker.Blocked) Open();
        }

        void Open()
        {
            open = true;
            openedFrame = Time.frameCount;
            Time.timeScale = 0f;
            InputBlocker.Block();
            FirstPersonController.LockCursor(false);
            int day = DayCycle.Instance != null ? DayCycle.Instance.Day : 1;
            subtitle.text = $"Day {day}  ·  {MoneyText.Format(Wallet.Instance.Money)} in the bank  ·  {PoolRegistry.OpenPools} of {PoolRegistry.TotalPools} pools open";
            stats.Clear();
            AddStat("Happy surfers (all time)", GameStats.SurfersServed.ToString());
            AddStat("Lost customers", spawner.Lost.ToString());
            AddStat("Tickets sold", GameStats.TicketsSold.ToString());
            AddStat("Waves sent", GameStats.WavesSent.ToString());
            AddStat("Boards rented", GameStats.BoardsRented.ToString());
            if (GameStats.SnacksSold > 0) AddStat("Snacks sold", GameStats.SnacksSold.ToString());
            if (GameStats.BeachGuests > 0) AddStat("Beach guests", GameStats.BeachGuests.ToString());
            AddStat("Park beauty", $"{Decoration.Beauty}  (+{MoneyText.Format(Decoration.FairBonus)} fair price)");
            AddStat("Tricks / tube rides", $"{GameStats.TricksLanded} / {GameStats.TubeRides}");
            AddStat("Your own rides", GameStats.PlayerRides.ToString());
            AddStat("Total earned", MoneyText.Format(GameStats.TotalEarned));
            if (ParkLevel.Instance != null) AddStat("Park level", $"{ParkLevel.Instance.Level}  ({ParkLevel.Instance.XpToNext:#,0} XP to next)");
            foreach (var worker in StaffMember.Active)
                AddStat(worker.JobTitle, StaffMember.StarText(worker.Stars));
            root.RemoveFromClassList("hidden");
        }

        void AddStat(string name, string value)
        {
            var row = new VisualElement();
            row.AddToClassList("stat-row");
            var n = new Label(name);
            n.AddToClassList("report-name");
            var v = new Label(value);
            v.AddToClassList("report-value");
            row.Add(n);
            row.Add(v);
            stats.Add(row);
        }

        void Close()
        {
            if (!open) return;
            open = false;
            root.AddToClassList("hidden");
            Time.timeScale = 1f;
            InputBlocker.Release();
            FirstPersonController.LockCursor(true);
        }

        void NewGame()
        {
            SaveSystem.DeleteSave();
            GameStats.Reset();
            ParkEffects.ResetStatics();
            DailyEvents.ResetStatics();
            MachineService.ResetStatics();
            saveSystem.enabled = false; // don't save the old park on the way out
            Time.timeScale = 1f;
            InputBlocker.ResetAll();
            MainMenu.SkipNextTime = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Saves and goes back to the title screen.</summary>
        void QuitToMenu()
        {
            saveSystem.Save();
            saveSystem.enabled = false; // already saved
            GameStats.Reset();
            ParkEffects.ResetStatics();
            DailyEvents.ResetStatics();
            MachineService.ResetStatics();
            Time.timeScale = 1f;
            InputBlocker.ResetAll();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
