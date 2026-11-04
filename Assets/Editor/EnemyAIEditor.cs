using UnityEditor;

//Tutorial for script found: https://www.youtube.com/watch?v=xFtFWmiW7IE
[CustomEditor(typeof(EnemyAI), true)]
public class EnemyAIEditor : Editor
{
    #region SerializedProperties
        SerializedProperty debugAttacks;
        SerializedProperty entityTemplate;
        SerializedProperty actionCooldowns;
        SerializedProperty actionStateTimes;
        SerializedProperty turnThreshold;
        SerializedProperty wanderSpeedMult;
        SerializedProperty searchTurnTime;
        SerializedProperty searchSpeedMult;
        SerializedProperty wanderRadius;
        SerializedProperty searchRadius;
        SerializedProperty searchPoints;
        SerializedProperty lookAroundTime;
        SerializedProperty lookAroundAngle;
        SerializedProperty chaseSpeedMult;
        SerializedProperty chaseReactionTime;
        SerializedProperty chaseAccelTime;
        SerializedProperty chaseTurnSpeed;
        SerializedProperty loseTargetTime;
        SerializedProperty chaseStopDistance;
        SerializedProperty giveUpDistance;
        SerializedProperty maxLeadTime;
        SerializedProperty trackingTime;
        SerializedProperty attacks;
        SerializedProperty senseRadius;
        SerializedProperty followThroughDistance;
        SerializedProperty terrain;
        SerializedProperty mesh;

        bool chaseStateGroup, wanderStateGroup, searchStateGroup, actionsGroup = false;
    #endregion
    void OnEnable()
    {
        debugAttacks = serializedObject.FindProperty("debugAttacks");
        entityTemplate = serializedObject.FindProperty("entityTemplate");
        actionCooldowns = serializedObject.FindProperty("actionCooldowns");
        actionStateTimes = serializedObject.FindProperty("actionStateTimes");
        turnThreshold = serializedObject.FindProperty("turnThreshold");
        wanderSpeedMult = serializedObject.FindProperty("wanderSpeedMult");
        searchTurnTime = serializedObject.FindProperty("searchTurnTime");
        searchSpeedMult = serializedObject.FindProperty("searchSpeedMult");
        wanderRadius = serializedObject.FindProperty("wanderRadius");
        searchRadius = serializedObject.FindProperty("searchRadius");
        searchPoints = serializedObject.FindProperty("searchPoints");
        lookAroundTime = serializedObject.FindProperty("lookAroundTime");
        lookAroundAngle = serializedObject.FindProperty("lookAroundAngle");
        chaseSpeedMult = serializedObject.FindProperty("chaseSpeedMult");
        chaseReactionTime = serializedObject.FindProperty("chaseReactionTime");
        chaseAccelTime = serializedObject.FindProperty("chaseAccelTime");
        chaseTurnSpeed = serializedObject.FindProperty("chaseTurnSpeed");
        loseTargetTime = serializedObject.FindProperty("loseTargetTime");
        chaseStopDistance = serializedObject.FindProperty("chaseStopDistance");
        giveUpDistance = serializedObject.FindProperty("giveUpDistance");
        maxLeadTime = serializedObject.FindProperty("maxLeadTime");
        trackingTime = serializedObject.FindProperty("trackingTime");
        attacks = serializedObject.FindProperty("attacks");
        senseRadius = serializedObject.FindProperty("senseRadius");
        followThroughDistance = serializedObject.FindProperty("followThroughDistance");
        terrain = serializedObject.FindProperty("terrain");
        mesh = serializedObject.FindProperty("mesh");
    }
    public override void OnInspectorGUI()
    {
        EnemyAI enemyAI = (EnemyAI)target;

        serializedObject.Update();

        EditorGUILayout.PropertyField(entityTemplate);

        if (entityTemplate != null) {
            EditorGUILayout.PropertyField(terrain);
            EditorGUILayout.PropertyField(mesh);
        }

        EditorGUILayout.Space(5);

        if (attacks.arraySize > 0) EditorGUILayout.PropertyField(debugAttacks);
        EditorGUILayout.PropertyField(attacks);

        actionsGroup = EditorGUILayout.BeginFoldoutHeaderGroup(actionsGroup, "General State Parameters");
        if (actionsGroup) {
            EditorGUILayout.PropertyField(actionStateTimes);
            EditorGUILayout.PropertyField(actionCooldowns);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        chaseStateGroup = EditorGUILayout.BeginFoldoutHeaderGroup(chaseStateGroup, "Chase State Parameters");
        if (chaseStateGroup) {
            EditorGUILayout.PropertyField(chaseSpeedMult);
            EditorGUILayout.PropertyField(chaseReactionTime);
            EditorGUILayout.PropertyField(chaseAccelTime);
            EditorGUILayout.PropertyField(chaseTurnSpeed);
            EditorGUILayout.PropertyField(loseTargetTime);
            EditorGUILayout.PropertyField(chaseStopDistance);
            EditorGUILayout.PropertyField(giveUpDistance);
            EditorGUILayout.PropertyField(maxLeadTime);
            EditorGUILayout.PropertyField(trackingTime);
            EditorGUILayout.PropertyField(senseRadius);
            EditorGUILayout.PropertyField(followThroughDistance);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        searchStateGroup = EditorGUILayout.BeginFoldoutHeaderGroup(searchStateGroup, "Search State Parameters");
        if (searchStateGroup) {
            EditorGUILayout.PropertyField(searchRadius);
            EditorGUILayout.PropertyField(searchPoints);
            EditorGUILayout.PropertyField(lookAroundTime);
            EditorGUILayout.PropertyField(lookAroundAngle);
            EditorGUILayout.PropertyField(searchTurnTime);
            EditorGUILayout.PropertyField(searchSpeedMult);
            EditorGUILayout.PropertyField(turnThreshold);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        wanderStateGroup = EditorGUILayout.BeginFoldoutHeaderGroup(wanderStateGroup, "Wander State Parameters");
        if (wanderStateGroup) {
            EditorGUILayout.PropertyField(wanderSpeedMult);
            EditorGUILayout.PropertyField(wanderRadius);
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        serializedObject.ApplyModifiedProperties();
        //base.OnInspectorGUI();
    }
}
