using UnityEditor;

[CustomEditor(typeof(NPCController))]
public class NPCControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty quest = serializedObject.FindProperty("hasQuestToGiveDialog");
        SerializedProperty waitingToFinishQuestDialog = serializedObject.FindProperty("waitingToFinishQuestDialog");
        SerializedProperty questCompleteDialog = serializedObject.FindProperty("questCompletedDialog");
        SerializedProperty preRequisite = serializedObject.FindProperty("preRequisiteQuestToShowQuest");
        DrawPropertiesExcluding(
            serializedObject,
            "waitingToFinishQuestDialog",
            "questCompletedDialog",
            "preRequisiteQuestToShowQuest"
        );

        if (quest.objectReferenceValue != null)
        {
            EditorGUILayout.PropertyField(waitingToFinishQuestDialog);
            EditorGUILayout.PropertyField(questCompleteDialog);
            EditorGUILayout.PropertyField(preRequisite);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
