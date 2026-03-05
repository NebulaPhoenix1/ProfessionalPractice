using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public partial class PrefabPlaceTool : EditorWindow
{
    Vector2 scrollPosition;

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
        
        //Start scroll view for all settings
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        //Prefab Pallete 
        serializedObject.Update();
        EditorGUILayout.PropertyField(propPallete, new GUIContent("Prefab Pallete", "List of random prefabs to spawn, a custom offset can be set for each."), true);
        EditorGUILayout.Space();

        //Placement Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Placement Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(propParentContainer, new GUIContent("Parent Container", "If set, all spawned objects will be parented under this transform for organisation"));
        EditorGUILayout.PropertyField(propPlacementMask, new GUIContent("Valid Placement Layers", "The tool will only place objects on colliders on these layers."));
        EditorGUILayout.PropertyField(propEraseMask, new GUIContent("Eraseable Layers", "The tool will only erase objects on colliders on these layers."));
        matchSurfaceNormal = EditorGUILayout.Toggle(new GUIContent("Match Surface Normal", "If enabled, spawned objects will be rotated to match the surface normal of the placement surface"), matchSurfaceNormal);
        EditorGUILayout.Space();

        //Override prefab layer 
        overridePrefabLayer = EditorGUILayout.Toggle(new GUIContent("Override Prefab Layer", "If enabled, all spawned objects will be set to the specified layer"), overridePrefabLayer);
        if(overridePrefabLayer)
        {
            spawnLayer = EditorGUILayout.LayerField(new GUIContent("Spawn Layer", "The layer to set spawned objects to"), spawnLayer);
            EditorGUILayout.Space();
        }

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
            gridSize = EditorGUILayout.Slider(new GUIContent("Grid Size", "The size of the grid to snap to"), gridSize, 0.1f, PrefabPlaceToolSettings.MaxGridSize);
            //Preset buttons
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(PrefabPlaceToolSettings.GridSizePreset1.ToString()+"m")) gridSize = PrefabPlaceToolSettings.GridSizePreset1;
            if(GUILayout.Button(PrefabPlaceToolSettings.GridSizePreset2.ToString()+"m")) gridSize = PrefabPlaceToolSettings.GridSizePreset2;
            if(GUILayout.Button(PrefabPlaceToolSettings.GridSizePreset3.ToString()+"m")) gridSize = PrefabPlaceToolSettings.GridSizePreset3;
            GUILayout.EndHorizontal();
            snapHeight = EditorGUILayout.Toggle(new GUIContent("Snap to Ground Height", "If enabled, objects will be snapped to the height of the grid"), snapHeight);
            //Check if grid size is greater than max grid size, if so, set it to max grid size. This prevents errors with the grid drawing function.
            if(gridSize > PrefabPlaceToolSettings.MaxGridSize) gridSize = PrefabPlaceToolSettings.MaxGridSize;
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space();
        //Snap Rotation Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Rotation Snapping Settings", EditorStyles.boldLabel);
        //Mutually exclusive logic for snap rotation and random rotation (they can't both be on at the same time)
        bool previousSnap = snapRotation;
        snapRotation = EditorGUILayout.Toggle(new GUIContent("Enable Rotation Snapping", "If enabled, objects will snap to specific rotation angles when placed"), snapRotation);
        if(snapRotation && !previousSnap)
        {
            randomRotation = false; //Turn off random rotation if snap rotation is enabled
        }
        if(snapRotation)
        {
            snapAngle = EditorGUILayout.FloatField(new GUIContent("Snap Angle", "The angle (degrees) increments to snap rotation to"), snapAngle);
            if(snapAngle < 0.1f) snapAngle = 0.1f; //Minimum snap angle of 0.1 degrees to prevent divide by zero errors
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(PrefabPlaceToolSettings.RotationSnappingPreset1.ToString()+"°")) snapAngle = PrefabPlaceToolSettings.RotationSnappingPreset1;
            if(GUILayout.Button(PrefabPlaceToolSettings.RotationSnappingPreset2.ToString()+"°")) snapAngle = PrefabPlaceToolSettings.RotationSnappingPreset2;
            if(GUILayout.Button(PrefabPlaceToolSettings.RotationSnappingPreset3.ToString()+"°")) snapAngle = PrefabPlaceToolSettings.RotationSnappingPreset3;
            if(GUILayout.Button(PrefabPlaceToolSettings.RotationSnappingPreset4.ToString()+"°")) snapAngle = PrefabPlaceToolSettings.RotationSnappingPreset4;
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space();


        //Randomisation Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Randomisation Settings", EditorStyles.boldLabel);
        //Toggle for random selectio of prefabs
        randomSelection = EditorGUILayout.Toggle(new GUIContent("Random Prefab Selection", "If enabled, a random prefab from the pallete will be selected each time you spawn an object. If disabled, you can cycle through prefabs with the up arrow and down arrow keys"), randomSelection);


        //Mutually exclusive logic for snap rotation and random rotation 
        bool previousRandomRot = randomRotation;
        randomRotation = EditorGUILayout.Toggle(new GUIContent("Random Rotation", "If enabled, spawned objects will be rotated randomly within the specified range"), randomRotation);
        if(randomRotation && !previousRandomRot)
        {
            snapRotation = false; //Turn off snap rotation if random rotation is enabled
        }
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
            EditorGUILayout.MinMaxSlider(ref minScale, ref maxScale, 0.1f, PrefabPlaceToolSettings.MaxScaleLimit);
            maxScale = EditorGUILayout.FloatField(maxScale, GUILayout.MaxWidth(50));
            GUILayout.EndHorizontal();
            //Check if maxScale is greater than max scale limit, if so, set it to max scale limit. This prevents errors with the random scale function.
            if(maxScale > PrefabPlaceToolSettings.MaxScaleLimit) maxScale = PrefabPlaceToolSettings.MaxScaleLimit;
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
    
        EditorGUILayout.HelpBox("Scene View Controls:\nSPACE = Spawn Object\n[ and ] = Rotate Object manually\nSHIFT + BACKSPACE = Erase Tool \nUp and Down Arrows = Cycle Prefabs Manually", MessageType.Info);
        EditorGUILayout.EndScrollView();

        if(EditorGUI.EndChangeCheck() && isToolActive)
        {
            //If any settings changed, update the ghost object to reflect those changes
            PrepareNextSpawn();
        }
    }
}
