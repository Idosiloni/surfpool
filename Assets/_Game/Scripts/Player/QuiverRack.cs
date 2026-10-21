using UnityEngine;

namespace SurfPool
{
    /// <summary>
    /// Your own board rack by the main pool's surf peg: press E to switch to the next board in your quiver
    /// (<see cref="SurfQuiver"/>). The board you have picked hangs out of the rack (the others stay in it).
    /// </summary>
    public class QuiverRack : MonoBehaviour, IInteractable
    {
        [Tooltip("The display boards on the rack, by BoardKind. The one you're using is hidden (it's with you).")]
        [SerializeField] GameObject[] displays;

        public string Prompt => $"Switch board: {SurfQuiver.Current} -> {SurfQuiver.Next}";

        public void Interact()
        {
            SurfQuiver.Choose(SurfQuiver.Next);
            Sfx.Play(Sound.Plop);
            Notifications.Post(SurfQuiver.Blurb(SurfQuiver.Current), Notice.Info);
        }

        void OnEnable()
        {
            SurfQuiver.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => SurfQuiver.Changed -= Refresh;

        void Refresh()
        {
            if (displays == null) return;
            for (int i = 0; i < displays.Length; i++)
                if (displays[i] != null) displays[i].SetActive(i != (int)SurfQuiver.Current);
        }
    }
}
