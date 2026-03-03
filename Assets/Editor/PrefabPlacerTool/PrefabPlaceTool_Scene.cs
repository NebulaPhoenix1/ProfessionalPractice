using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

public partial class PrefabPlaceTool : EditorWindow
{
    //This is where the ray cast logic happens which tells us where to place objects as well as drawing ghost and grid previews    
    void OnSceneGUI(SceneView sceneView)
    {
        if(!isToolActive) return;

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
            
            // Overlap checking
            if(preventOverlap)
            {
                Vector3 checkPosition = currentPreviewPosition + (hit.normal * overlapRadius);
                isBlocked = Physics.CheckSphere(checkPosition, overlapRadius, overlapMask);
            }
        }

        //Update ghost object preview
        // Now that the raycast is done, we can turn the ghost object back on and move it!
        if (hasHit && !isBlocked && !isErasing && ghostObject != null && nextPrefabIndex < prefabPallete.Count)
        {
            PalleteEntry currentItem = prefabPallete[nextPrefabIndex];
            ghostObject.SetActive(true); 
            ghostObject.transform.localScale = Vector3.one * nextScale;
            Quaternion finalRotation = matchSurfaceNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) * nextRotation : nextRotation;
            ghostObject.transform.rotation = finalRotation;
            ghostObject.transform.position = currentPreviewPosition + (finalRotation * currentItem.offset);
        }

        //Input handling
        if (e.type == EventType.KeyDown)
        {
            //Detecting spacebar for placement 
            if (e.keyCode == KeyCode.Space)
            {
                if (hasHit && !isBlocked && !isErasing)
                {
                    SpawnObject(currentPreviewPosition, hit.normal); 
                }
                e.Use(); 
            }
            else if (e.keyCode == KeyCode.LeftBracket) //Rotate Left
            {
                float rotationAmount = (snapRotation && snapAngle > 0) ? snapAngle : 45; // Default to 45 degrees if snapping is off or angle is invalid
                nextRotation *= Quaternion.Euler(0, -rotationAmount, 0);
                e.Use();
            }
            else if (e.keyCode == KeyCode.RightBracket) //Rotate Right
            {
                float rotationAmount = (snapRotation && snapAngle > 0) ? snapAngle : 45; // Default to 45 degrees if snapping is off or angle is invalid
                nextRotation *= Quaternion.Euler(0, rotationAmount, 0);
                e.Use();
            }
            //Detecting shift + backspace for erasing
            else if (e.keyCode == KeyCode.Backspace && e.shift)
            {
                isErasing = true;
                DeleteObjectUnderCursor(ray);
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
                DeleteObjectUnderCursor(ray);
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
                Handles.color = new Color(1f, 0f, 0f, 0.4f);
                Handles.DrawSolidDisc(brushPos, brushNormal, 0.5f);
                Handles.color = Color.red;
                Handles.DrawWireDisc(brushPos, brushNormal, 0.5f);
            }
            else if(hasHit)
            {
                DrawGridPreview(currentPreviewPosition);
                if (preventOverlap)
                {
                    Vector3 checkPos = currentPreviewPosition + (hit.normal * overlapRadius);
                    Handles.color = isBlocked ? new Color(1f, 0f, 0f, 0.5f) : new Color(0f, 1f, 0f, 0.5f);
                    Handles.DrawSolidDisc(checkPos, Vector3.up, overlapRadius);
                    Handles.color = isBlocked ? Color.red : Color.green;
                    Handles.DrawWireDisc(checkPos, hit.normal, overlapRadius);
                }
            }
        }
        // Only refresh the scene view when the user actually interacts, avoiding infinite loops
        if(e.type == EventType.MouseMove || e.type == EventType.MouseDrag || e.type == EventType.KeyDown || e.type == EventType.KeyUp)
        {
            sceneView.Repaint(); 
        }
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
        //Pick random valid index
        nextPrefabIndex = validIndices[Random.Range(0, validIndices.Count)];
        nextScale = randomScale ? Random.Range(minScale, maxScale) : 1.0f;
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
        newObj.transform.position = position + (finalRotation * currentItem.offset);
        //Parent to container if set
        if(parentContainer != null)
        {
            newObj.transform.parent = parentContainer;
        }
        PrepareNextSpawn();
    }

    //Function to replace selected objects with random prefabs from the pallete, applying the same rotation, scale and parent as the original object as well as the prefab offset
    void ReplaceSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if(selectedObjects.Length == 0 || prefabPallete.Count == 0) return;
        
        foreach(GameObject obj in selectedObjects)
        {
            // Just picking a basic random one here for replacement
            PalleteEntry currentItem = prefabPallete[Random.Range(0, prefabPallete.Count)];
            if (currentItem.prefab == null) continue;

            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(currentItem.prefab);
            
            // Apply original transform data
            newObj.transform.rotation = obj.transform.rotation;
            newObj.transform.localScale = obj.transform.localScale;
            newObj.transform.parent = obj.transform.parent;

            // Apply position with the new prefab's offset taken into account
            newObj.transform.position = obj.transform.position + (newObj.transform.rotation * currentItem.offset);
            
            Undo.RegisterCreatedObjectUndo(newObj, "Replaced Object");
            Undo.DestroyObjectImmediate(obj);   
        }
    }
    

    void DeleteObjectUnderCursor(Ray ray)
    {
       if(Physics.Raycast(ray, out RaycastHit eraseHit, Mathf.Infinity, eraseMask, QueryTriggerInteraction.Ignore))
       {
           GameObject objToDelete = eraseHit.collider.gameObject;
           GameObject rootToDelete = PrefabUtility.GetOutermostPrefabInstanceRoot(objToDelete);
           if(rootToDelete != null)
           {
               Undo.DestroyObjectImmediate(rootToDelete);
           }
           else
           {
               Undo.DestroyObjectImmediate(objToDelete);
           }
       }
    }
}
