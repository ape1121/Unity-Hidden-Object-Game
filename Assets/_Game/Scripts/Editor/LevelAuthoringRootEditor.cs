#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelAuthoringRoot))]
public class LevelAuthoringRootEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var authoringRoot = (LevelAuthoringRoot)target;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Authoring Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Apply Background From Data"))
        {
            Undo.RecordObject(authoringRoot, "Apply Background From Data");
            authoringRoot.ApplyBackgroundFromData();
            EditorUtility.SetDirty(authoringRoot);
        }

        if (GUILayout.Button("Capture Background To Data"))
        {
            if (authoringRoot.LevelData != null)
            {
                Undo.RecordObject(authoringRoot.LevelData, "Capture Background To Data");
            }

            authoringRoot.CaptureBackgroundToData();

            if (authoringRoot.LevelData != null)
            {
                EditorUtility.SetDirty(authoringRoot.LevelData);
            }
        }

        if (GUILayout.Button("Load Markers From Data"))
        {
            Undo.RegisterFullObjectHierarchyUndo(authoringRoot.gameObject, "Load Markers From Data");
            authoringRoot.LoadMarkersFromData();
            EditorUtility.SetDirty(authoringRoot.gameObject);
        }

        if (GUILayout.Button("Save Markers To Data"))
        {
            if (authoringRoot.LevelData != null)
            {
                Undo.RecordObject(authoringRoot.LevelData, "Save Markers To Data");
            }

            authoringRoot.SaveMarkersToData();

            if (authoringRoot.LevelData != null)
            {
                EditorUtility.SetDirty(authoringRoot.LevelData);
            }
        }

        if (GUILayout.Button("Refresh Marker Icons"))
        {
            authoringRoot.RefreshMarkerIcons();
            EditorUtility.SetDirty(authoringRoot.gameObject);
        }
    }
}
#endif
