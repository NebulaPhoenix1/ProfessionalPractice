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
        //Custom inspector for prefab pallete list
        GUILayout.BeginVertical("box");
        GUILayout.Label("Prefab Pallete", EditorStyles.boldLabel);
        Undo.RecordObject(this, "Modify Prefab Pallete"); //Allows undo/redo for changes to the prefab pallete
        for (int i = 0; i < prefabPallete.Count; i++)
        {
            GUILayout.BeginHorizontal();
            Texture2D preview = null;
            if(prefabPallete[i].prefab != null)
            {
                preview = AssetPreview.GetAssetPreview(prefabPallete[i].prefab);
            }
            //Draw the icon preview of the prefab if it exists, otherwise draw a box with text saying no preview available
            if(preview != null)
            {
                GUILayout.Label(preview, GUILayout.Width(64), GUILayout.Height(64));
            }
            else
            {
                GUILayout.Box("No Preview Available", GUILayout.Width(64), GUILayout.Height(64));
            }
            //Draw Properties
            GUILayout.BeginVertical();
            prefabPallete[i].prefab = (GameObject)EditorGUILayout.ObjectField("Prefab Object", prefabPallete[i].prefab, typeof(GameObject), false);
            prefabPallete[i].offset = EditorGUILayout.Vector3Field("Placement Offset", prefabPallete[i].offset);
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100; //Set label width for weight slider to prevent it from taking up too much space
            prefabPallete[i].weight = EditorGUILayout.Slider(new GUIContent("Spawn Weight", "Chance of the prefab to spawn"), prefabPallete[i].weight, 0f, 100f);
            EditorGUIUtility.labelWidth = originalLabelWidth; //Reset label width
            EditorGUILayout.Space();
            GUILayout.EndVertical();
            
            //Remove button
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f); //Light red color for remove button
            if(GUILayout.Button("X", GUILayout.Width(25), GUILayout.ExpandHeight(true)))
            {
                prefabPallete.RemoveAt(i);
                i--;
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space();
            GUILayout.EndHorizontal();
        }
        //Add new prefab button
        GUI.backgroundColor = new Color(0.4f, 1f, 0.4f); //Light green color for add button
        if(GUILayout.Button("Add New Prefab", GUILayout.Height(25)))
        {
            prefabPallete.Add(new PalleteEntry());
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndVertical();
        EditorGUILayout.Space();


        //Placement Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Placement Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(propParentContainer, new GUIContent("Parent Container", "If set, all spawned objects will be parented under this transform for organisation"));
        EditorGUILayout.PropertyField(propPlacementMask, new GUIContent("Valid Placement Layers", "The tool will only place objects on colliders on these layers."));
        matchSurfaceNormal = EditorGUILayout.Toggle(new GUIContent("Match Surface Normal", "If enabled, spawned objects will be rotated to match the surface normal of the placement surface"), matchSurfaceNormal);
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
        
        
        
        EditorGUILayout.Space();

        //Paint Brush Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Paint Brush Settings", EditorStyles.boldLabel);
        usePaintBrush = EditorGUILayout.Toggle(new GUIContent("Use Paint Brush", "If enabled, the tool will spawn multiple prefabs in a brush pattern while the mouse button is held down"), usePaintBrush);
        if(usePaintBrush)
        {
            brushRadius = EditorGUILayout.Slider(new GUIContent("Brush Radius", "The radius of the paint brush"), brushRadius, 0.1f, 50f);
            brushDensity = EditorGUILayout.IntSlider(new GUIContent("Brush Density", "How many prefabs to spawn per brush stroke"), brushDensity, 1, 20);
            brushSpacing = EditorGUILayout.Slider(new GUIContent("Brush Spacing", "Minimum distance between spawned prefabs when using the paint brush"), brushSpacing, 0.1f, 10f);
            EditorGUILayout.HelpBox("Hold Left Click and Drag in the Scene View to paint prefabs. The brush will spawn prefabs in a radius around the mouse cursor, the density and spacing of the prefabs can be adjusted with the settings above.", MessageType.Info);
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

        

        //Erase Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Erase Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(propEraseMask, new GUIContent("Eraseable Layers", "The tool will only erase objects on colliders on these layers."));
        eraseRadius = EditorGUILayout.Slider(new GUIContent("Erase Radius", "The radius of the area eraser"), eraseRadius, 0.1f, 50f);
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
