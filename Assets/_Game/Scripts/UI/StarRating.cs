using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>Five stars drawn with Painter2D: gold for the rating (half stars allowed), dark for the rest.</summary>
    public class StarRating : VisualElement
    {
        static readonly Color Gold = new(1f, 0.74f, 0.2f);
        static readonly Color Empty = new(0.04f, 0.18f, 0.23f, 0.15f);
        static readonly Color EmptyOnDark = new(1f, 0.97f, 0.93f, 0.2f);
        static readonly Color Ink = new(0.04f, 0.18f, 0.23f, 0.85f);

        float stars;

        /// <summary>Drawn on a dark (ink) pill: the empty stars are a faint cream instead of a faint ink.</summary>
        public bool OnDark { get; set; }

        public StarRating()
        {
            AddToClassList("star-rating");
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        /// <summary>0 to 5, in half steps.</summary>
        public float Stars
        {
            get => stars;
            set
            {
                if (Mathf.Approximately(stars, value)) return;
                stars = value;
                MarkDirtyRepaint();
            }
        }

        void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            float size = Mathf.Min(contentRect.height, contentRect.width / 5f);
            for (int i = 0; i < 5; i++)
            {
                var center = new Vector2(size * (i + 0.5f), contentRect.height / 2f);
                float radius = size * 0.48f;
                FillStar(painter, center, radius, OnDark ? EmptyOnDark : Empty, false);
                if (stars >= i + 1) FillStar(painter, center, radius, Gold, false);
                else if (stars >= i + 0.5f) FillStar(painter, center, radius, Gold, true);
                if (stars >= i + 0.5f) OutlineStar(painter, center, radius);
            }
        }

        /// <summary>Ink outline around a lit star, to match the cartoon outlines of the rest of the UI.</summary>
        static void OutlineStar(Painter2D painter, Vector2 center, float radius)
        {
            painter.strokeColor = Ink;
            painter.lineWidth = Mathf.Max(1.5f, radius * 0.14f);
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            for (int p = 0; p < 10; p++)
            {
                float r = p % 2 == 0 ? radius : radius * 0.45f;
                float angle = Mathf.Deg2Rad * (-90f + p * 36f);
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                if (p == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.ClosePath();
            painter.Stroke();
        }

        /// <summary>A five-pointed star; leftHalf clips it to its left half for half stars.</summary>
        static void FillStar(Painter2D painter, Vector2 center, float radius, Color color, bool leftHalf)
        {
            painter.fillColor = color;
            painter.BeginPath();
            for (int p = 0; p < 10; p++)
            {
                float r = p % 2 == 0 ? radius : radius * 0.45f;
                float angle = Mathf.Deg2Rad * (-90f + p * 36f);
                var point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                if (leftHalf) point.x = Mathf.Min(point.x, center.x);
                if (p == 0) painter.MoveTo(point);
                else painter.LineTo(point);
            }
            painter.ClosePath();
            painter.Fill();
        }
    }
}
