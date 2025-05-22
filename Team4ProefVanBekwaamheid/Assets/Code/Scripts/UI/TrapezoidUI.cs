using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class TrapezoidImage : Image
{
    [Header("Corner Offsets (relative to RectTransform size)")]
    public Vector2 topLeftOffset = Vector2.zero;
    public Vector2 topRightOffset = Vector2.zero;
    public Vector2 bottomLeftOffset = Vector2.zero;
    public Vector2 bottomRightOffset = Vector2.zero;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();

        Vector2 bl = new Vector2(rect.xMin, rect.yMin) + bottomLeftOffset;
        Vector2 tl = new Vector2(rect.xMin, rect.yMax) + topLeftOffset;
        Vector2 tr = new Vector2(rect.xMax, rect.yMax) + topRightOffset;
        Vector2 br = new Vector2(rect.xMax, rect.yMin) + bottomRightOffset;

        UIVertex vert = UIVertex.simpleVert;
        vert.color = color;

        vert.position = bl; vert.uv0 = new Vector2(0, 0); vh.AddVert(vert);
        vert.position = tl; vert.uv0 = new Vector2(0, 1); vh.AddVert(vert);
        vert.position = tr; vert.uv0 = new Vector2(1, 1); vh.AddVert(vert);
        vert.position = br; vert.uv0 = new Vector2(1, 0); vh.AddVert(vert);

        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(2, 3, 0);
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        SetVerticesDirty();
    }
#endif
}
