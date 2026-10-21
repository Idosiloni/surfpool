using UnityEngine;

namespace SurfPool.EditorTools
{
    /// <summary>
    /// Agent helper: renders the scene from any point to a PNG (works without a Scene View window, in edit or play mode).
    /// Call from eval: <c>DevShots.Shot(path, new Vector3(...), new Vector3(...))</c>.
    /// </summary>
    public static class DevShots
    {
        public static string Shot(string path, Vector3 from, Vector3 lookAt, float fov = 50f, int width = 1280, int height = 800)
        {
            var go = new GameObject("Dev Shot Camera");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 1000f;
            go.transform.position = from;
            go.transform.LookAt(lookAt);
            var rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(tex);
            return path;
        }
    }
}
