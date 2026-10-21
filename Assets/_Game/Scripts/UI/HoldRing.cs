using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>A ring around the crosshair that fills while you hold E (blending, shaping, repairing).</summary>
    [UxmlElement]
    public partial class HoldRing : VisualElement
    {
        float progress;

        public float Progress
        {
            get => progress;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, progress)) return;
                progress = value;
                MarkDirtyRepaint();
            }
        }

        public HoldRing()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) / 2f - 4f;
            var painter = context.painter2D;
            painter.lineWidth = 6f;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = new Color(0.04f, 0.18f, 0.23f, 0.55f);
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();
            if (progress <= 0f) return;
            painter.strokeColor = progress >= 1f ? new Color(1f, 0.78f, 0.34f) : new Color(0.08f, 0.72f, 0.65f);
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * progress));
            painter.Stroke();
        }
    }
}
