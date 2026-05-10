#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEditor;

public class ChangeFonts : MonoBehaviour
{
    public List<TextMeshPro> Texts = new List<TextMeshPro>();

    public TMP_FontAsset NewFont;

    public void ChangeFontsFunktion()
    {
        foreach (TextMeshPro t in Texts)
        {
            t.font = NewFont;
        }
    }
}

[CustomEditor(typeof(ChangeFonts))]
public class ChangeFontsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("Set To New Font"))
        {
            ChangeFonts s = (ChangeFonts)target;
            s.ChangeFontsFunktion();
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(s);
        }
    }
}

#endif