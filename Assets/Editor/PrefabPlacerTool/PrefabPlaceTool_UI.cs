using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.VersionControl;

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
        //Prefab Palette UI
        GUILayout.BeginVertical("box");
        GUILayout.Label("Prefab Palette Preset", EditorStyles.boldLabel);
        activePreset = (PrefabPlaceToolPalettePreset)EditorGUILayout.ObjectField("Active Palette Preset", activePreset, typeof(PrefabPlaceToolPalettePreset), false);
        GUILayout.BeginHorizontal();
        //Disable load buttons if no preset is provided
        GUI.enabled = activePreset != null;
        if(GUILayout.Button("Load from preset"))
        {
            LoadPaletteFromPreset();
        }
        GUI.enabled = true;//Re enable rest of the UI
        string saveButtonText = activePreset != null ? "Save (Overwrite)" : "Save as New Preset";
        if(GUILayout.Button(saveButtonText))
        {
            SavePaletteToPreset();
        }
    
        GUILayout.EndHorizontal();

        //Clear button to empty current tool palette
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if(GUILayout.Button("Clear Palette"))
        {
            //Confirm clear button
            if(EditorUtility.DisplayDialog("Clear Palette?", "Are you sure you want to clear the current working palette?", "Yes", "Cancel"));
            {
                Undo.RecordObject(this, "Clear Palette");
                activePreset = null;
                prefabPallete.Clear();
            }
        }
        GUI.backgroundColor = Color.white;
        GUILayout.EndVertical();
        EditorGUILayout.Space();


        //Custom inspector for prefab pallete list
        GUILayout.BeginVertical("box");
        GUILayout.Label("Prefab Palette", EditorStyles.boldLabel);
        Undo.RecordObject(this, "Modify Prefab Palette"); //Allows undo/redo for changes to the prefab pallete
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
            float originalLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 85; //Set label width for object field to prevent it from taking up too much space
            prefabPallete[i].prefab = (GameObject)EditorGUILayout.ObjectField("Prefab Object", prefabPallete[i].prefab, typeof(GameObject), false);
            EditorGUIUtility.labelWidth = originalLabelWidth; //Reset label width

            prefabPallete[i].offset = EditorGUILayout.Vector3Field("Placement Offset", prefabPallete[i].offset);
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
        EditorGUILayout.PropertyField(propPlacementMask, new GUIContent("Valid Placement Layers", "The tool will only place objects on colliders on these layers."));
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
        //Parent container
        EditorGUILayout.PropertyField(propParentContainer, new GUIContent("Parent Container", "If set, all spawned objects will be parented under this transform for organisation"));
        //Match surface normal 
        matchSurfaceNormal = EditorGUILayout.Toggle(new GUIContent("Match Surface Normal", "If enabled, spawned objects will be rotated to match the surface normal of the placement surface"), matchSurfaceNormal);
        //Slope limit
        limitSlope = EditorGUILayout.Toggle(new GUIContent("Limit Slope", "If enabled, the tool will check the slope of the placement surface and prevent placement if it exceeds the specified angle"), limitSlope);
        if(limitSlope)
        {
            maxSlopeAngle = EditorGUILayout.Slider(new GUIContent("Max Slope Angle", "The maximum slope angle (in degrees) that allows placement"), maxSlopeAngle, 0f, PrefabPlaceToolSettings.MaxSlopeAngleLimit);
            if(maxSlopeAngle > PrefabPlaceToolSettings.MaxSlopeAngleLimit) maxSlopeAngle = PrefabPlaceToolSettings.MaxSlopeAngleLimit; //Failsafe to prevent user from setting a slope angle that is too high which can cause issues with placement logic
        }
        //Auto apply static flags
        autoApplyStaticFlags = EditorGUILayout.Toggle(new GUIContent("Auto Apply Static Flags", "If enabled, static flags will automatically be applied to spawned objects based on the settings below"), autoApplyStaticFlags);
        if(autoApplyStaticFlags)
        {
            staticFlags = (StaticEditorFlags)EditorGUILayout.EnumFlagsField(new GUIContent("Static Flags", "The static flags to apply to spawned objects"), staticFlags);
            EditorGUILayout.Space();
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
        //Random Jitter Settings
        randomJitter = EditorGUILayout.Toggle(new GUIContent("Random Depth Jitter", "If enabled, spawned objects will have a random offset applied along the surface normal to add variation to placements"), randomJitter);
        if (randomJitter)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Depth Jitter Range", GUILayout.Width(100));
            minDepthJitter = EditorGUILayout.FloatField(minDepthJitter, GUILayout.MaxWidth(50));
            EditorGUILayout.MinMaxSlider(ref minDepthJitter, ref maxDepthJitter, 0f, PrefabPlaceToolSettings.MaxJitterLimit);
            maxDepthJitter = EditorGUILayout.FloatField(maxDepthJitter, GUILayout.MaxWidth(50));
            //Failsafe to prevent user from setting jitter values that are too high which can cause issues with placements
            if (maxDepthJitter > PrefabPlaceToolSettings.MaxJitterLimit) maxDepthJitter = PrefabPlaceToolSettings.MaxJitterLimit;
            GUILayout.EndHorizontal();
        }
            

        GUILayout.EndVertical();
        EditorGUILayout.Space();

        

        //Erase Settings
        GUILayout.BeginVertical("box");
        GUILayout.Label("Erase Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(propEraseMask, new GUIContent("Eraseable Layers", "The tool will only erase objects on colliders on these layers."));
        eraseRadius = EditorGUILayout.Slider(new GUIContent("Erase Radius", "The radius of the area eraser"), eraseRadius, 0.1f, 50f);
        //Filtered erase
        useTargetErase = EditorGUILayout.Toggle(new GUIContent("Filtered Erase", "If enabled the eraser will only delete objects that match the selected prefab below"), useTargetErase);
        if(useTargetErase)
        {
            targetedErasePrefab = (GameObject)EditorGUILayout.ObjectField("Selected Prefab", targetedErasePrefab, typeof(GameObject), false);
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

        //Prefab Placer
        GUILayout.BeginVertical("box");
        GUILayout.Label("Prefab Placer", EditorStyles.boldLabel);
        if(GUILayout.Button("Replace Selected Objects with Random Prefab"))
        {
            ReplaceSelectedObjects();
        }
        GUILayout.EndVertical();
    
        EditorGUILayout.HelpBox("Scene View Controls:\nSPACE = Spawn Object\n[ and ] = Rotate Object manually\nSHIFT + BACKSPACE = Erase Tool \nUp and Down Arrows = Cycle Prefabs Manually ", MessageType.Info);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("G: Toggle Grid\nN: Toggle Match Normal\nO: Toggle Prevent Overlap\nP: Toggle Paint Brush\nL: Toggle Random Scale\nR: Toggle Random Rotation\nJ: Toggle Rotation Snapping\nY: Toggle Random Prefab Selection\nF: Toggle Auto Apply Static Flags\nShift + E: Eyedropper Tool \nShift + C: Clear Filter", MessageType.Info);
        EditorGUILayout.EndScrollView();

        if(EditorGUI.EndChangeCheck() && isToolActive)
        {
            //If any settings changed, update the ghost object to reflect those changes
            PrepareNextSpawn();
        }
    }

    void LoadPaletteFromPreset()
    {
        //Return if this gets called with no active preset, but this should never happen
        if(activePreset == null) return;
        //Set up undo
        Undo.RecordObject(this, "Load Palette Preset");
        prefabPallete.Clear();
        //Keep track of if anything from the preset is null so we can log a message to console if so
        int missingCount = 0;
        foreach(PalleteEntry entry in activePreset.prefabPallete)
        {
            //If current entry is null, skip 
            if(entry.prefab == null)
            {
                missingCount++;
                continue;
            }
            //Make a copy of the asset to the palette 
            PalleteEntry newPrefab = new PalleteEntry();
            newPrefab.prefab = entry.prefab;
            newPrefab.offset = entry.offset;
            newPrefab.weight = entry.weight;
            prefabPallete.Add(newPrefab);
        }
        //Reset spawn selection logic
        PrepareNextSpawn();
        //If any null prefabs, notify user
        if(missingCount > 0)
        {
            //$ to say Hey, im gonna include variables in this string so we don't have to concatenate with +
            Debug.LogWarning($"[Prefab Place Tool] Loaded Preset' {activePreset.name} ' but {missingCount} prefab(s) were missing from the project and skipped.");
            SceneView.currentDrawingSceneView?.ShowNotification(new GUIContent($"Loaded Preset (Skipped {missingCount} Missing)"));
        }
    }
    void SavePaletteToPreset()
    {
        //Create new file if one is not set
        if(activePreset == null)
        {
            CreateNewPreset();
            return;
        }
        if(EditorUtility.DisplayDialog("Overwrite Preset?", $"Are you sure you want to overwrite the {activePreset.name} preset with the current palette?", "Yes", "Cancel"));
        {
            Undo.RecordObject(this, "Save Palette Preset");
            activePreset.prefabPallete.Clear();
            foreach(PalleteEntry entry in prefabPallete)
            {
                PalleteEntry newEntry = new PalleteEntry();
                newEntry.prefab = entry.prefab;
                newEntry.offset = entry.offset;
                newEntry.weight = entry.weight;
                activePreset.prefabPallete.Add(newEntry);
            }
            EditorUtility.SetDirty(activePreset);
            AssetDatabase.SaveAssets();
            SceneView.currentDrawingSceneView?.ShowNotification(new GUIContent($"Saved: {activePreset.name}"));

        }
    }
    void CreateNewPreset()
    {
        //Open a save window thats locked to the unity project folder
        string path = EditorUtility.SaveFilePanelInProject(
            "Create New Palette Preset",
            "New Prefab Palette",
            "asset",
            "Choose a location to store your asset");
        //Check if the user hits cancel, if so abort
        if(string.IsNullOrEmpty(path)) return;
        //Create a blank file in memory
        PrefabPlaceToolPalettePreset newPreset = ScriptableObject.CreateInstance<PrefabPlaceToolPalettePreset>();
        //Copy the current palette to this new memory file
        foreach(PalleteEntry entry in prefabPallete)
        {
            PalleteEntry newEntry = new PalleteEntry();
            newEntry.prefab = entry.prefab;
            newEntry.offset = entry.offset;
            newEntry.weight = entry.weight;
            newPreset.prefabPallete.Add(newEntry);
        }
        //Write the file in memory to disk
        AssetDatabase.CreateAsset(newPreset, path);
        AssetDatabase.SaveAssets();
        //Set this new file to be active
        activePreset = newPreset;
        SceneView.currentDrawingSceneView?.ShowNotification(new GUIContent($"Created and Saved: {activePreset.name}" ));
    }
}


