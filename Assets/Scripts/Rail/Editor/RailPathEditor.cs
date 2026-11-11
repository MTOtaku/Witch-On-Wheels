using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RailPath))]
public class RailPathEditor : Editor {
    private void OnEnable() => Undo.undoRedoPerformed += OnUndoRedo;
    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    private void OnUndoRedo() {
        if (target != null) ((RailPath)target).Rebuild();
    }

    private void OnSceneGUI() {
        var rail = (RailPath)target;
        Transform tr = rail.transform;

        // Arrastar os pontos
        for (int i = 0; i < rail.points.Count; i++) {
            Vector3 world = tr.TransformPoint(rail.points[i]);
            float size = HandleUtility.GetHandleSize(world) * 0.12f;

            EditorGUI.BeginChangeCheck();
            Handles.color = Color.yellow;
            Vector3 moved = Handles.FreeMoveHandle(world, size, Vector3.zero, Handles.CircleHandleCap);
            Handles.Label(world + Vector3.up * size * 2f, i.ToString());

            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(rail, "Mover ponto do trilho");
                rail.points[i] = tr.InverseTransformPoint(moved);
                rail.Rebuild();
                EditorUtility.SetDirty(rail);
            }
        }

        // Shift + clique esquerdo: adiciona um ponto no fim
        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        if (e.type == EventType.Layout) {
            HandleUtility.AddDefaultControl(controlId);
        }

        if (e.type == EventType.MouseDown && e.button == 0 && e.shift && !e.alt &&
            HandleUtility.nearestControl == controlId) {
            Vector3 clickWorld = HandleUtility.GUIPointToWorldRay(e.mousePosition).origin; // câmera 2D ortográfica
            clickWorld.z = tr.position.z;

            Undo.RecordObject(rail, "Adicionar ponto do trilho");
            rail.points.Add(tr.InverseTransformPoint(clickWorld));
            rail.Rebuild();
            EditorUtility.SetDirty(rail);
            e.Use();
        }
    }

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        var rail = (RailPath)target;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Arraste os pontos amarelos na Scene.\nShift + clique: adiciona ponto no fim.", MessageType.Info);
        EditorGUILayout.LabelField("Comprimento", rail.Length.ToString("F2") + " unidades");

        if (GUILayout.Button("Remover último ponto") && rail.points.Count > 2) {
            Undo.RecordObject(rail, "Remover ponto do trilho");
            rail.points.RemoveAt(rail.points.Count - 1);
            rail.Rebuild();
            EditorUtility.SetDirty(rail);
        }
    }
}