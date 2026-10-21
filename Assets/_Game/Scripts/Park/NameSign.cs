using TMPro;
using UnityEngine;

namespace SurfPool
{
    /// <summary>Sign text that shows the park's name (<see cref="ParkName"/>) and repaints when you rename the park.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class NameSign : MonoBehaviour
    {
        [Tooltip("{0} is replaced by the park name, e.g. \"WELCOME TO {0}\".")]
        [SerializeField] string format = "{0}";
        [SerializeField] bool upperCase = true;

        TMP_Text text;

        public void Setup(string newFormat, bool upper)
        {
            format = newFormat;
            upperCase = upper;
        }

        void OnEnable()
        {
            ParkName.Changed += Refresh;
            Refresh();
        }

        void OnDisable() => ParkName.Changed -= Refresh;

        void Refresh()
        {
            if (text == null) text = GetComponent<TMP_Text>();
            string name = upperCase ? ParkName.Name.ToUpperInvariant() : ParkName.Name;
            text.text = string.Format(format, name);
        }
    }
}
