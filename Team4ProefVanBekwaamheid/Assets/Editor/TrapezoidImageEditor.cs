using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(TrapezoidImage))]
public class TrapezoidImageEditor : ImageEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TrapezoidImage ti = (TrapezoidImage)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Trapezoid Corner Offsets", EditorStyles.boldLabel);
        ti.topLeftOffset = EditorGUILayout.Vector2Field("Top Left", ti.topLeftOffset);
        ti.topRightOffset = EditorGUILayout.Vector2Field("Top Right", ti.topRightOffset);
        ti.bottomLeftOffset = EditorGUILayout.Vector2Field("Bottom Left", ti.bottomLeftOffset);
        ti.bottomRightOffset = EditorGUILayout.Vector2Field("Bottom Right", ti.bottomRightOffset);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(ti);
            ti.SetVerticesDirty();
        }
    }
}
