using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.XR;
using NUnit.Framework;


public partial class PrefabPlaceTool : EditorWindow
{
    //This is where the ray cast logic happens which tells us where to place objects as well as drawing ghost and grid previews    
    void OnSceneGUI(SceneView sceneView)
    {
        if(!isToolActive)
        {
            return;
        }

        Event e = Event.current;

        // Failsafe: Ensure ghost object exists
        if (ghostObject == null && !isErasing && prefabPallete.Count > 0)
        {
            PrepareNextSpawn();
        }

        //Tell Unity not to select objects or consume hotkeys
        int controlID = GUIUtility.GetControlID("PrefabPlaceTool".GetHashCode(), FocusType.Passive);
        if (e.type == EventType.Layout)
        {
            HandleUtility.AddDefaultControl(controlID);
        }

        //Raycast
        // We temporarily turn off the ghost object so it is impossible for our raycast to hit it.
        if (ghostObject != null) ghostObject.SetActive(false);

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        hasHit = Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, placementMask, QueryTriggerInteraction.Ignore);
        bool isBlocked = false;

        if (hasHit)
        {
            currentPreviewPosition = hit.point;
            
            // Apply grid maths
            if(useGrid)
            {
                currentPreviewPosition.x = Mathf.Round(currentPreviewPosition.x / gridSize) * gridSize;
                currentPreviewPosition.z = Mathf.Round(currentPreviewPosition.z / gridSize) * gridSize;
                currentPreviewPosition.y = snapHeight ? Mathf.Round(currentPreviewPosition.y / gridSize) * gridSize : hit.point.y;
            }

            // Limit slope if enabled
            if(limitSlope)
            {
                float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
                if(surfaceAngle > maxSlopeAngle)
                {
                    isBlocked = true;
                }
            }
            // Overlap checking, only check if not blocked
            if(preventOverlap && !isBlocked)
            {
                Vector3 checkPosition = currentPreviewPosition + (hit.normal * overlapRadius);
                isBlocked = Physics.CheckSphere(checkPosition, overlapRadius, overlapMask);
            }
        }

        //Update ghost object preview
        // Now that the raycast is done, we can turn the ghost object back on and move it!
        if (hasHit && !isBlocked && !isErasing && ghostObject != null && nextPrefabIndex < prefabPallete.Count && !usePaintBrush)
        {
            PalleteEntry currentItem = prefabPallete[nextPrefabIndex];
            ghostObject.SetActive(true); 
            ghostObject.transform.localScale = Vector3.one * nextScale;
            Quaternion finalRotation = matchSurfaceNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) * nextRotation : nextRotation;
            ghostObject.transform.rotation = finalRotation;
            //Subtracting (hit.normal * nextDepthJitter) from the position to create a jitter effect that moves the object slightly towards or away from the surface normal, this can help make placements look more natural and less uniform
            ghostObject.transform.position = currentPreviewPosition + (finalRotation * currentItem.offset) - (hit.normal * nextDepthJitter);
        }
        //Make sure ghost object is hidden if painting
        else if(ghostObject != null)
        {
            ghostObject.SetActive(false);
        }
        if(usePaintBrush && !isErasing && !e.alt && e.button == 0)
        {
            if(e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
            {
                if(hasHit)
                {
                    //Check if we click or dragged far enough from the last paint position to spawn more objects
                    if(Vector3.Distance(currentPreviewPosition, lastPaintPosition) >= brushSpacing || e.type == EventType.MouseDown)
                    {
                        PaintPrefabs(currentPreviewPosition);
                        lastPaintPosition = currentPreviewPosition;
                    }
                }
                e.Use();
            }
        }

        //Input handling
        if (e.type == EventType.KeyDown)
        {
            //Detecting spacebar for placement 
            if (e.keyCode == KeyCode.Space && !usePaintBrush) 
            {
                if (hasHit && !isBlocked && !isErasing)
                {
                    SpawnObject(currentPreviewPosition, hit.normal); 
                }
                e.Use(); 
            }
            else if (e.keyCode == KeyCode.LeftBracket) //Rotate Left
            {
                float rotationAmount = (snapRotation && snapAngle > 0) ? snapAngle : PrefabPlaceToolSettings.ManualRotationAmount; // Default to 45 degrees if snapping is off or angle is invalid
                nextRotation *= Quaternion.Euler(0, -rotationAmount, 0);
                e.Use();
            }
            else if (e.keyCode == KeyCode.RightBracket) //Rotate Right
            {
                float rotationAmount = (snapRotation && snapAngle > 0) ? snapAngle : PrefabPlaceToolSettings.ManualRotationAmount; // Default to 45 degrees if snapping is off or angle is invalid
                nextRotation *= Quaternion.Euler(0, rotationAmount, 0);
                e.Use();
            }
            //Cycling prefabs with arrow keys (up and down)
            else if(e.keyCode == KeyCode.UpArrow)
            {
                CyclePrefab(1); //Go to next 
                e.Use();
            }
            else if(e.keyCode == KeyCode.DownArrow)
            {
                CyclePrefab(-1); //Go to previous
                e.Use();
            }
            //Detecting shift + backspace for erasing
            else if (e.keyCode == KeyCode.Backspace && e.shift)
            {
                isErasing = true;
                EraseObjectsInRadius(ray);
                e.Use();
            }
            //Settings toggle hot keys go here
            //Toggle Grid (G)
            else if (e.keyCode == KeyCode.G && !e.shift)
            {
                useGrid = !useGrid;
                Repaint();
                e.Use();
            }
            //Toggle Match Normal (N)
            else if(e.keyCode == KeyCode.N && !e.shift)
            {
                matchSurfaceNormal = !matchSurfaceNormal;
                PrepareNextSpawn(); //Update ghost object rotation immediately to reflect the change
                Repaint();
                e.Use();
            }
            //Toggle Prevent Overlap (O)
            else if(e.keyCode == KeyCode.O && !e.shift)
            {
                preventOverlap = !preventOverlap;
                Repaint();
                e.Use();
            }
            //Toggle Paint Brush (P)
            else if(e.keyCode == KeyCode.P && !e.shift)
            {
                usePaintBrush = !usePaintBrush;
                PrepareNextSpawn(); //Update ghost object immediately to reflect the change
                Repaint();
                e.Use();
            }
            //Toggle Random Scale (L)
            else if(e.keyCode == KeyCode.L && !e.shift)
            {
                randomScale = !randomScale;
                PrepareNextSpawn(); //Update ghost object immediately to reflect the change
                Repaint();
                e.Use();
            }
            //Toggle Random Rotation (R)
            else if(e.keyCode == KeyCode.R && !e.shift)
            {
                randomRotation = !randomRotation;
                PrepareNextSpawn(); //Update ghost object immediately to reflect the change
                Repaint();
                e.Use();
            }
            //Toggle Rotation Snapping (J)
            else if(e.keyCode == KeyCode.J && !e.shift)
            {
                snapRotation = !snapRotation;
                Repaint();
                e.Use();
            }
            //Toggle Random Prefab Selection (Y)
            else if(e.keyCode == KeyCode.Y && !e.shift)
            {
                randomSelection = !randomSelection;
                PrepareNextSpawn(); //Update ghost object immediately to reflect the change
                Repaint();
                e.Use();
            }
            //Toggle Auto Apply Static Flags (F)
            else if(e.keyCode == KeyCode.F && !e.shift)
            {
                autoApplyStaticFlags = !autoApplyStaticFlags;
                Repaint();
                e.Use();
            }
            //Erase filter hot keys
            //Shift E: Eye dropper tool for filter object
            else if(e.keyCode == KeyCode.E && e.shift)
            {
                SampleEraseTarget(ray);
                Repaint();
                e.Use();
            }
            //Shift C: Clear filter
            else if(e.keyCode == KeyCode.C && e.shift)
            {
                useTargetErase = false;
                targetedErasePrefab = null;
                Repaint();
                e.Use();
            }
        }
        else if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Backspace)
        {
            isErasing = false;
        }
        else if (isErasing && (e.type == EventType.MouseDrag || e.type == EventType.MouseMove))
        {
            if (e.shift)
            {
                EraseObjectsInRadius(ray);
                e.Use();
            }
            else
            {
                isErasing = false;
            }
        }

        //Visuals 
        if (e.type == EventType.Repaint)
        {
            if(isErasing)
            {
                Vector3 brushPos;
                Vector3 brushNormal;

                //Try hitting an erasable object
                if (Physics.Raycast(ray, out RaycastHit eraseHit, Mathf.Infinity, eraseMask, QueryTriggerInteraction.Ignore))
                {
                    brushPos = eraseHit.point;
                    brushNormal = eraseHit.normal;
                }
                //Hitting the ground/placement surface
                else if (hasHit)
                {
                    brushPos = hit.point;
                    brushNormal = hit.normal;
                }
                //Float the circle in front of the camera
                else
                {
                    brushPos = ray.GetPoint(10f); // 10 units in front of the camera
                    brushNormal = -ray.direction; // Face the camera
                }
                Color eraseCol = PrefabPlaceToolSettings.ErasePreviewColor;
                Handles.color = eraseCol;
                Handles.DrawSolidDisc(brushPos, brushNormal, eraseRadius);
                
                eraseCol.a = 1f; // Make the wireframe fully opaque
                Handles.color = eraseCol;
                Handles.DrawWireDisc(brushPos, brushNormal, eraseRadius);
            }
            else if(hasHit)
            {
                DrawGridPreview(currentPreviewPosition);
                Vector3 visualPosition = currentPreviewPosition + (hit.normal * 0.05f); //Offset the preview slightly above the surface to prevent z-fighting
                if(usePaintBrush)
                {
                    Color brushColor = isBlocked ? PrefabPlaceToolSettings.InvalidPreviewColor : PrefabPlaceToolSettings.ValidPreviewColor;
                    Handles.color = brushColor;
                    Handles.DrawWireDisc(visualPosition, hit.normal, brushRadius);
                    if(isBlocked) Handles.DrawSolidDisc(visualPosition, hit.normal, brushRadius); //Fill in so obvious when brush is blocked
                }
                //Individual placement mode
                else
                {
                    Color activeColor = isBlocked ? PrefabPlaceToolSettings.InvalidPreviewColor : PrefabPlaceToolSettings.ValidPreviewColor;
                    Handles.color = activeColor;
                    //Default to a tiny radius so we can still see
                    float radius = preventOverlap ? overlapRadius : 0.5f;
                    Handles.DrawSolidDisc(visualPosition, hit.normal, radius);
                    //Drawing a wiredisc 
                    activeColor.a = 1f;
                    Handles.color = activeColor;
                    Handles.DrawWireDisc(visualPosition, hit.normal, radius);
                    //If blockec draw a line out of the surface to ensure the user can see if placement is blocked
                    if(isBlocked)
                    {
                        Handles.DrawLine(visualPosition, visualPosition + (hit.normal * 2f), 3f);
                    }

                }
            }
        }
        // Only refresh the scene view when the user actually interacts, avoiding infinite loops
        if(e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.KeyDown || e.type == EventType.KeyUp)
        {
            sceneView.Repaint(); 
        }

        if(PrefabPlaceToolSettings.ShowSceneUI) DrawHandles(); 
    }

    void DrawHandles()
    {
        Handles.BeginGUI();
        string toolMode;
        Color modeColor;
        //Figure out which tool is currently active for display 
        if(isErasing)
        {
            //Differentiate between normal and filtered erase
            if(useTargetErase && targetedErasePrefab != null)
            {
                toolMode = "Erasing: " + targetedErasePrefab.name;
            }
            else toolMode = "Erasing All";
            modeColor = PrefabPlaceToolSettings.ErasePreviewColor;
        }
        else if(usePaintBrush)
        {
            toolMode = "Paint Brush Mode";
            modeColor = PrefabPlaceToolSettings.ValidPreviewColor;
        }
        else
        {
            toolMode = "Placement Mode";
            modeColor = PrefabPlaceToolSettings.ValidPreviewColor;
        }
        GUIStyle titleStyle = new GUIStyle(GUI.skin.box);
        titleStyle.fontSize = 14;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = modeColor;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        //Draw the title box
        GUI.Box(new Rect(10, 10, 200, 30), toolMode, titleStyle);
        //Draw Hotkey box if enabled
        if(PrefabPlaceToolSettings.ShowHotKeysInScene)
        {
            GUILayout.BeginArea(new Rect(10, 50, 250, 290), GUI.skin.box);
            GUIStyle hotKeyText = new GUIStyle(GUI.skin.label);
            hotKeyText.fontSize = 12;
            hotKeyText.normal.textColor = new Color(0.8f,0.8f,0.8f);
            GUILayout.Label("SPACE: Place Object", hotKeyText);
            GUILayout.Label("SHIFT + BACKSPACE: Erase Object", hotKeyText);
            GUILayout.Label("UP/DOWN ARROWS: Cycle Prefabs", hotKeyText);
            GUILayout.Label("LEFT/RIGHT BRACKETS: Rotate Object", hotKeyText);
            GUILayout.Space(5);
            GUILayout.Label("G: Toggle Grid", hotKeyText);
            GUILayout.Label("N: Toggle Match Normal", hotKeyText);
            GUILayout.Label("O: Toggle Prevent Overlap", hotKeyText);
            GUILayout.Label("P: Toggle Paint Brush", hotKeyText);
            GUILayout.Label("L: Toggle Random Scale", hotKeyText);
            GUILayout.Label("R: Toggle Random Rotation", hotKeyText);
            GUILayout.Label("J: Toggle Rotation Snapping", hotKeyText);
            GUILayout.Label("Y: Toggle Random Prefab Selection", hotKeyText);
            GUILayout.Label("F: Toggle Auto Apply Static Flags", hotKeyText);
            GUILayout.Label("Shift + E: Eyedropper filter", hotKeyText);
            GUILayout.Label("Shift + C: Clear filter", hotKeyText);
            GUILayout.Space(5);
            GUILayout.EndArea();
        }
        Handles.EndGUI();
    }

    //Function to pick the next prefab to spawn as well as calculating its random rotation and scale based on user settings, then creates the ghost object for previewing
    void PrepareNextSpawn()
    {
        if(prefabPallete.Count == 0) return;
        //Make a list of all the valid prefab indices (those that are not null) so we can pick from them when spawning
        List<int> validIndices = new List<int>();
        for(int i = 0; i < prefabPallete.Count; i++)
        {
            if(prefabPallete[i].prefab != null)
            {
                validIndices.Add(i);
            }
        }
        if(validIndices.Count == 0) return;
        if(randomSelection)
        {
            nextPrefabIndex = GetRandomWeightedPrefabIndex(validIndices);
        }
        else
        {
            if(!validIndices.Contains(nextPrefabIndex))
            {
                nextPrefabIndex = validIndices[0]; //Reset to first valid index if the current one is invalid
            }
        }
        nextScale = randomScale ? Random.Range(minScale, maxScale) : 1.0f;
        nextDepthJitter = randomJitter ? Random.Range(minDepthJitter, maxDepthJitter) : 0.0f;
        if(randomRotation)
        {
            nextRotation = Quaternion.Euler(
                Random.Range(minRotation.x, maxRotation.x),
                Random.Range(minRotation.y, maxRotation.y),
                Random.Range(minRotation.z, maxRotation.z)
            );
        }
        else if(snapRotation && snapAngle > 0)
        {
            Vector3 euler = nextRotation.eulerAngles;
            euler.x = Mathf.Round(euler.x / snapAngle) * snapAngle;
            euler.y = Mathf.Round(euler.y / snapAngle) * snapAngle;
            euler.z = Mathf.Round(euler.z / snapAngle) * snapAngle;
            nextRotation = Quaternion.Euler(euler);
        }
        CreateGhostObject();
    }

    //Function to pick a prefab based on it's weight
    int GetRandomWeightedPrefabIndex(List<int> validIndices)
    {
        float totalWeight = 0f;
        //Calculate total weight
        foreach(int index in validIndices)
        {
            totalWeight += prefabPallete[index].weight;
        }
        //Check if total weight is set to 0, if so return first valid prefab
        if(totalWeight <= 0f) return validIndices[0];
        //Pick a random value between 0 and total weight
        float randomValue = Random.Range(0f, totalWeight);
        //Iterate through and subtract weights until we hit 0 or less
        foreach(int index in validIndices)
        {
            randomValue -= prefabPallete[index].weight;
            if(randomValue <= 0f)
            {
                return index;
            }
        }
        return validIndices[validIndices.Count - 1]; //Fallback, should never happen if weights are set up correctly
    }


    void CyclePrefab(int direction)
    {
        if(prefabPallete.Count == 0) return;
        if(randomSelection)
        {
            randomSelection = false; //Turn off random selection if user starts manually cycling
            Repaint();
        }
        //Make a list of all valid prefab indices
        List<int> validIndices = new List<int>();
        for(int i = 0; i < prefabPallete.Count; i++)
        {
            if(prefabPallete[i].prefab != null)
            {
                validIndices.Add(i);
            }
        }
        if(validIndices.Count == 0) return;
        //Find where we are in the valid list
        int currentIndexInValid = validIndices.IndexOf(nextPrefabIndex);
        if(currentIndexInValid == -1) currentIndexInValid = 0; //If current index is invalid for some reason, reset to first valid index
        currentIndexInValid += direction;
        //Wrap around
        if(currentIndexInValid >= validIndices.Count) currentIndexInValid = 0;
        if(currentIndexInValid < 0) currentIndexInValid = validIndices.Count - 1;
        nextPrefabIndex = validIndices[currentIndexInValid];
        //Keep rotation and scale consistent when cycling
        if(randomRotation)
        {
            nextRotation = Quaternion.Euler(
                UnityEngine.Random.Range(minRotation.x, maxRotation.x),
                UnityEngine.Random.Range(minRotation.y, maxRotation.y),
                UnityEngine.Random.Range(minRotation.z, maxRotation.z)
            );
        }
        CreateGhostObject();
       
    }

    void CreateGhostObject()
    {
        DestroyGhostObject(); 
        
        if (prefabPallete.Count == 0 || nextPrefabIndex >= prefabPallete.Count) return;

        GameObject prefab = prefabPallete[nextPrefabIndex].prefab;
        if (prefab == null) return;

        ghostObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        ghostObject.hideFlags = HideFlags.HideAndDontSave; //Hide from hierarchy and prevent saving
        
        Collider[] colliders = ghostObject.GetComponentsInChildren<Collider>();
        foreach(Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    void DestroyGhostObject()
    {
        if (ghostObject != null)
        {
            DestroyImmediate(ghostObject);
        }
    }

    void DrawGridPreview(Vector3 center)
    {
        if(!useGrid) return;
        Handles.color = new Color(1f,1f,1f,1f);
        int lines = 4;
        float size = gridSize * lines;
        //Draw X lines
        for(int i = -lines; i <= lines; i++)
        {
            Vector3 start = center + new Vector3(-size, 0, i * gridSize);
            Vector3 end = center + new Vector3(size, 0, i * gridSize);
            Handles.DrawLine(start, end);
        }
        
        //Draw Z lines
        for(int j = -lines; j <= lines; j++)
        {
            Vector3 start = center + new Vector3(j * gridSize, 0, -size);
            Vector3 end = center + new Vector3(j * gridSize, 0, size);
            Handles.DrawLine(start, end);
        }
    }

    void SpawnObject(Vector3 position, Vector3 normal)
    {
        //Return early if no valid prefabs to spawn
        if(prefabPallete.Count == 0 || nextPrefabIndex >= prefabPallete.Count) return;
        //Instantiate the prefab, apply rotation, scale and offset based on settings and surface normal, then prepare the next spawn
        PalleteEntry currentItem = prefabPallete[nextPrefabIndex];
        if(currentItem.prefab == null) return;
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(currentItem.prefab);
        Undo.RegisterCreatedObjectUndo(newObj, "Spawned Prefab");
        newObj.transform.position = position + currentItem.offset;
        newObj.transform.localScale = Vector3.one * nextScale;
        //Calculate rotation
        Quaternion finalRotation = matchSurfaceNormal ? Quaternion.FromToRotation(Vector3.up, normal) * nextRotation : nextRotation;
        newObj.transform.rotation = finalRotation;

        // Apply Position + Local Offset
        newObj.transform.position = position + (finalRotation * currentItem.offset) - (normal * nextDepthJitter); //Apply depth jitter along the surface normal to add variation to placements
        //Parent to container if set
        if(parentContainer != null)
        {
            newObj.transform.parent = parentContainer;
        }
        //Apply auto static flags if enabled
        if(autoApplyStaticFlags)
        {
            SetStaticFlagsRecursievely(newObj, staticFlags);
        }
        //Overrude layer if enabled
        if(overridePrefabLayer)
        {
            SetLayerRecursively(newObj, spawnLayer);
        }
        PrepareNextSpawn();
    }

    //Function to replace selected objects with random prefabs from the pallete, applying the same rotation, scale and parent as the original object as well as the prefab offset
    void ReplaceSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if(selectedObjects.Length == 0 || prefabPallete.Count == 0) return;
        //Get valid indices
        List<int> validIndices = new List<int>();
        for(int i = 0; i < prefabPallete.Count; i++)
        {
            if(prefabPallete[i].prefab != null)
            {
                validIndices.Add(i);
            }
        }
        if(validIndices.Count == 0) return;

        //List to hold new spawned objects
        List<GameObject> newObjects = new List<GameObject>();
        //Group undo operations so if replaced is mashed Ctrl Z is easier
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Replace Selected Objects");
        int undoGroup = Undo.GetCurrentGroup();

        foreach(GameObject obj in selectedObjects)
        {
            //Pick random item from pallete based on weight if set
            int randomIndex = GetRandomWeightedPrefabIndex(validIndices);
            PalleteEntry currentItem = prefabPallete[randomIndex];
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(currentItem.prefab);
            
            // Apply original transform data
            newObj.transform.rotation = obj.transform.rotation;
            newObj.transform.localScale = obj.transform.localScale;
            newObj.transform.parent = obj.transform.parent;

            // Apply position with the new prefab's offset taken into account
            newObj.transform.position = obj.transform.position + (newObj.transform.rotation * currentItem.offset);
            
            Undo.RegisterCreatedObjectUndo(newObj, "Replaced Object");
            Undo.DestroyObjectImmediate(obj); 
            newObjects.Add(newObj);
        }
        Undo.CollapseUndoOperations(undoGroup);
        Selection.objects = newObjects.ToArray();
    }
    

    void EraseObjectsInRadius(Ray ray)
    {
        Vector3 eraseCenter;
        if(Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, eraseMask | placementMask, QueryTriggerInteraction.Ignore))
        {
            eraseCenter = hit.point;
        }
        else
        {
            eraseCenter = ray.GetPoint(10f); // 10 units in front of the camera, fallback if aiming at nothing
        }
        Collider[] collidersToErase = Physics.OverlapSphere(eraseCenter, eraseRadius, eraseMask, QueryTriggerInteraction.Ignore);
        if(collidersToErase.Length > 0)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Erase Multiple Objects");
            int undoGroup = Undo.GetCurrentGroup();
            HashSet<GameObject> erasedObjects = new HashSet<GameObject>(); //Use a hash set to avoid duplicates
            foreach(Collider col in collidersToErase)
            {
                GameObject objToDelete = col.gameObject;
                GameObject rootToDelete = objToDelete.transform.root.gameObject; //Get the root object to delete the entire prefab instance
                GameObject finalTarget = rootToDelete != null ? rootToDelete : objToDelete; //Fallback to the collider's own object if something goes wrong with getting the root
                //Targeted erase check
                if(useTargetErase && targetedErasePrefab != null)
                {
                    GameObject sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(finalTarget);
                    //If it does not match our filter skip it
                    if(sourceObject != targetedErasePrefab) continue;
                }            

                if(!erasedObjects.Contains(finalTarget))
                {
                    Undo.DestroyObjectImmediate(finalTarget);
                    erasedObjects.Add(finalTarget);
                }
            }
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    //Sets the layer of the object and all its children recursively, used for the layer override feature
    void SetLayerRecursively(GameObject obj, int layer)
    {
        if(obj == null) return;
        //Get every transform attached to this object and its children (true for inactive objects)
        Transform[] allTransforms = obj.GetComponentsInChildren<Transform>(true);
        foreach(Transform t in allTransforms)
        {
            t.gameObject.layer = layer;
        }
    } 

    //Paint brush logic
    void PaintPrefabs(Vector3 brushCenter)
    {
        if(prefabPallete.Count == 0) return;

        //Calculate vaild prefabs
        List<int> validIndices = new List<int>();
        if(randomSelection)
        {
            for(int i = 0; i < prefabPallete.Count; i++)
            {
                if(prefabPallete[i].prefab != null)
                {
                    validIndices.Add(i);
                }
            }
            if(validIndices.Count == 0) return;
        }

        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName("Paint Prefabs");
        int undoGroup = Undo.GetCurrentGroup();
        //Iterate through brush density
        for(int i = 0; i < brushDensity; i++)
        {
            //Pick random 2D position within radius
            Vector2 randomPos = UnityEngine.Random.insideUnitCircle * brushRadius;
            //Go high in sky to prepare for raycast downwards
            Vector3 raycastOrigin = brushCenter + new Vector3(randomPos.x, 200f, randomPos.y);
            //Raycast downwards to find height of ground at this random point
            if(Physics.Raycast(raycastOrigin, Vector3.down, out RaycastHit hit, 200f, placementMask, QueryTriggerInteraction.Ignore))
            {
                if(limitSlope)
                {
                    float surfaceAngle = Vector3.Angle(Vector3.up, hit.normal);
                    if(surfaceAngle > maxSlopeAngle)
                    {
                        continue; //Skip this spawn if the slope is too steep
                    }
                }
                //Check for overlap if enabled
                if(preventOverlap)
                {
                    Vector3 checkPosition = hit.point + (hit.normal * overlapRadius);
                    if(Physics.CheckSphere(checkPosition, overlapRadius, overlapMask))
                    {
                        continue; //Skip this spawn if it is overlapping
                    }
                }
                //Choose which prefab to spawn (manual selection vs random)
                int indexToSpawn = randomSelection ? GetRandomWeightedPrefabIndex(validIndices) : nextPrefabIndex;
                PalleteEntry currentItem = prefabPallete[indexToSpawn];
                if(currentItem.prefab == null) continue;
                //Calculate random rotation and scale
                float scale = randomScale ? UnityEngine.Random.Range(minScale, maxScale) : 1.0f;
                float depthJitter = randomJitter ? UnityEngine.Random.Range(minDepthJitter, maxDepthJitter) : 0.0f;
                Quaternion rotation = nextRotation;
                if(randomRotation)
                {
                    rotation = Quaternion.Euler(
                        UnityEngine.Random.Range(minRotation.x, maxRotation.x),
                        UnityEngine.Random.Range(minRotation.y, maxRotation.y),
                        UnityEngine.Random.Range(minRotation.z, maxRotation.z)
                    );
                }
                //Match surface normal if enabled
                Quaternion finalRotation = matchSurfaceNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) * rotation : rotation;
                Vector3 finalPosition = hit.point + (finalRotation * currentItem.offset) - (hit.normal * depthJitter); //Apply depth jitter along the surface normal to add variation to placements
                //Spawn the prefab
                GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(currentItem.prefab);
                newObj.transform.position = finalPosition;
                newObj.transform.localScale = Vector3.one * scale;
                newObj.transform.rotation = finalRotation;
                if(parentContainer != null) newObj.transform.parent = parentContainer;
                if(overridePrefabLayer) SetLayerRecursively(newObj, spawnLayer);
                if(autoApplyStaticFlags) SetStaticFlagsRecursievely(newObj, staticFlags);
                Undo.RegisterCreatedObjectUndo(newObj, "Painted Prefab");
                if(preventOverlap) Physics.SyncTransforms(); //Ensure physics engine is up to date before next overlap check
            }
            Undo.CollapseUndoOperations(undoGroup);
            PrepareNextSpawn(); //Prepare next spawn to update ghost object and prefab selection
        }
    }

    void SetStaticFlagsRecursievely(GameObject obj, StaticEditorFlags flags)
    {
        if(obj == null) return;
        GameObjectUtility.SetStaticEditorFlags(obj, flags);
        foreach(Transform child in obj.transform)
        {
            SetStaticFlagsRecursievely(child.gameObject, flags);
        }
    }

    //Uses a ray and prefab utility to get and set a erase object filter 
    void SampleEraseTarget(Ray ray)
    {
        if(Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, eraseMask, QueryTriggerInteraction.Ignore))
        {
            GameObject hitObj = hit.collider.gameObject;
            //Get root of the hit object in case we hit a leaf or something
            GameObject rootHit = PrefabUtility.GetOutermostPrefabInstanceRoot(hitObj);
            GameObject finalTarget = rootHit != null ? rootHit : hitObj;
            //Find the original project asset the object was spawned from
            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(finalTarget);
            if(sourcePrefab != null)
            {
                targetedErasePrefab = sourcePrefab;
                //Show quick pop up notification saying what the filtered object is
                SceneView.currentDrawingSceneView.ShowNotification(new GUIContent($"Erase filter set to: {targetedErasePrefab.name}"));
                useTargetErase = true;
            }
            else
            {
                //Show quick pop up notification saying what the filtered object is
                SceneView.currentDrawingSceneView.ShowNotification(new GUIContent($"Target is not a prefab."));
            }
        }
    }
}
