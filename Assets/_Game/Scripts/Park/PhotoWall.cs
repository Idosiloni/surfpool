using UnityEngine;

namespace SurfPool
{
    /// <summary>The Wall of Fame by the photo kiosk: your last six S-grade surf photos, newest top left.</summary>
    public class PhotoWall : MonoBehaviour
    {
        [Tooltip("The photo quads, in reading order.")]
        [SerializeField] Renderer[] frames;

        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        readonly Texture2D[] shown = new Texture2D[6];

        public void Add(Texture2D photo)
        {
            if (frames == null || frames.Length == 0) return;
            int n = Mathf.Min(frames.Length, shown.Length);
            var oldest = shown[n - 1];
            for (int i = n - 1; i > 0; i--) shown[i] = shown[i - 1];
            shown[0] = photo;
            if (oldest != null && System.Array.IndexOf(shown, oldest) < 0) Destroy(oldest);
            var block = new MaterialPropertyBlock();
            for (int i = 0; i < n; i++)
            {
                if (frames[i] == null) continue;
                frames[i].gameObject.SetActive(shown[i] != null);
                if (shown[i] == null) continue;
                block.Clear();
                block.SetTexture(BaseMap, shown[i]);
                frames[i].SetPropertyBlock(block);
            }
        }
    }
}
