using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public partial class PrefabPlaceTool : EditorWindow
{
   //The UI Window with all the options we can change
    void OnGUI()
    {
        GUILayout.Label("Prefab Placer Tool", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck(); //If a change happens, tells ghost object to instantly update

        //Button to activate/deactivate the tool
        GUI.backgroundColor = isToolActive ? Color.green : Color.red;
        if (GUILayout.Button(isToolActive ? "Deactivate Tool" : "Activate Tool"))
        {
            isToolActive = !isToolActive;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space();
        
        //Prefab Pallete 
        serializedObject.Update();
        EditorGUILayout.PropertyField(propPallete, new GUIContent("Prefab Pallete"), true);
        EditorGUILayout.Space();

        //Placement Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Placement Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(propParentContainer, new GUIContent("Parent Container", "If set, all spawned objects will be parented under this transform for organisation"));
        EditorGUILayout.PropertyField(propPlacementMask, new GUIContent("Valid Placement Layers"));
        matchSurfaceNormal = EditorGUILayout.Toggle("Match Surface Normal", matchSurfaceNormal);
        EditorGUILayout.Space();
        //Overlap prevention settings
        preventOverlap = EditorGUILayout.Toggle(new GUIContent("Prevent Overlap", "Stops spawning if there are existing colliders within the overlap radius"), preventOverlap);
        if(preventOverlap)
        {
            overlapRadius = EditorGUILayout.Slider("Overlap Check Radius", overlapRadius, 0.1f, 10.0f);
            EditorGUILayout.PropertyField(propOverlapMask, new GUIContent("Overlap Check Layers", "Layers to check for collisions. Exclude your ground layer."));
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space();
        
        serializedObject.ApplyModifiedProperties();

        //Grid Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
        useGrid = EditorGUILayout.Toggle("Enable Grid Snapping", useGrid);
        //Grid settings only show if the grid is enabled
        if(useGrid)
        {
            gridSize = EditorGUILayout.Slider("Grid Size", gridSize, 0.1f, 10.0f);
            snapHeight = EditorGUILayout.Toggle("Snap to Ground Height", snapHeight);
            //Preset buttons
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("0.5m")) gridSize = 0.5f;
            if(GUILayout.Button("1.0m")) gridSize = 1.0f;
            if(GUILayout.Button("2.0m")) gridSize = 2.0f;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space();

        //Randomisation Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Randomisation Settings", EditorStyles.boldLabel);
        randomRotation = EditorGUILayout.Toggle("Random Rotation", randomRotation);
        if(randomRotation)
        {
            minRotation = EditorGUILayout.Vector3Field("Min Rotation", minRotation);
            maxRotation = EditorGUILayout.Vector3Field("Max Rotation", maxRotation);
        }
        randomScale = EditorGUILayout.Toggle("Random Scale", randomScale);
        if(randomScale)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale Range");
            minScale = EditorGUILayout.FloatField(minScale, GUILayout.MaxWidth(50));
            EditorGUILayout.MinMaxSlider(ref minScale, ref maxScale, 0.1f, 3.0f);
            maxScale = EditorGUILayout.FloatField(maxScale, GUILayout.MaxWidth(50));
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space();

        //Prefab Placer
        GUILayout.BeginVertical("box");
        GUILayout.Label("Prefab Placer", EditorStyles.boldLabel);
        if(GUILayout.Button("Replace Selected Objects with Random Prefab"))
        {
            ReplaceSelectedObjects();
        }
        GUILayout.EndVertical();
    
        EditorGUILayout.HelpBox("Scene View Controls:\nSPACE = Spawn Object\n(Ensure objects have colliders to snap to!)", MessageType.Info);

        if(EditorGUI.EndChangeCheck() && isToolActive)
        {
            //If any settings changed, update the ghost object to reflect those changes
            PrepareNextSpawn();
        }
    }
}
