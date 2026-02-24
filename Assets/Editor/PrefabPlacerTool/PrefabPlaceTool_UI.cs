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
        EditorGUILayout.PropertyField(propPallete, new GUIContent("Prefab Pallete", "List of random prefabs to spawn, a custom offset can be set for each."), true);
        EditorGUILayout.Space();

        //Placement Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Placement Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(propParentContainer, new GUIContent("Parent Container", "If set, all spawned objects will be parented under this transform for organisation"));
        EditorGUILayout.PropertyField(propPlacementMask, new GUIContent("Valid Placement Layers", "The tool will only place objects on colliders on these layers."));
        matchSurfaceNormal = EditorGUILayout.Toggle(new GUIContent("Match Surface Normal", "If enabled, spawned objects will be rotated to match the surface normal of the placement surface"), matchSurfaceNormal);
        EditorGUILayout.Space();
        //Overlap prevention settings
        preventOverlap = EditorGUILayout.Toggle(new GUIContent("Prevent Overlap", "Stops spawning if there are existing colliders within the overlap radius"), preventOverlap);
        if(preventOverlap)
        {
            overlapRadius = EditorGUILayout.Slider(new GUIContent("Overlap Check Radius", "The radius to check for overlapping colliders"), overlapRadius, 0.1f, 10.0f);
            EditorGUILayout.PropertyField(propOverlapMask, new GUIContent("Overlap Check Layers", "Layers to check for collisions. Exclude your ground layer."));
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space();
        
        serializedObject.ApplyModifiedProperties();

        //Grid Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Grid Settings", EditorStyles.boldLabel);
        useGrid = EditorGUILayout.Toggle(new GUIContent("Enable Grid Snapping","If enabled, objects will snap to a grid when placed"), useGrid);
        //Grid settings only show if the grid is enabled
        if(useGrid)
        {
            gridSize = EditorGUILayout.Slider(new GUIContent("Grid Size", "The size of the grid to snap to"), gridSize, 0.1f, 10.0f);
            snapHeight = EditorGUILayout.Toggle(new GUIContent("Snap to Ground Height", "If enabled, objects will be snapped to the height of the grid"), snapHeight);
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
        randomRotation = EditorGUILayout.Toggle(new GUIContent("Random Rotation", "If enabled, spawned objects will be rotated randomly within the specified range"), randomRotation);
        if(randomRotation)
        {
            minRotation = EditorGUILayout.Vector3Field(new GUIContent("Min Rotation", "The minimum rotation to apply when randomising"), minRotation);
            maxRotation = EditorGUILayout.Vector3Field(new GUIContent("Max Rotation", "The maximum rotation to apply when randomising"), maxRotation);
        }
        randomScale = EditorGUILayout.Toggle(new GUIContent("Random Scale", "If enabled, spawned objects will be scaled randomly within the specified range"), randomScale);
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
