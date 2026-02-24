using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;

public partial class PrefabPlaceTool : EditorWindow
{
    //This is where the ray cast logic happens which tells us where to place objects as well as drawing ghost and grid previews    
    void OnSceneGUI(SceneView sceneView)
    {
        if(!isToolActive) return;
        //Shoot a ray cats
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit, Mathf.Infinity, placementMask, QueryTriggerInteraction.Ignore))
        {
            hasHit = true;
            currentPreviewPosition = hit.point;
            //Apply grid maths
            if(useGrid)
            {
                currentPreviewPosition.x = Mathf.Round(currentPreviewPosition.x / gridSize) * gridSize;
                currentPreviewPosition.z = Mathf.Round(currentPreviewPosition.z / gridSize) * gridSize;
                if(snapHeight)
                {
                    currentPreviewPosition.y = Mathf.Round(currentPreviewPosition.y / gridSize) * gridSize;
                }
                else
                {
                    currentPreviewPosition.y = hit.point.y;
                }
            }
            //Draw grid preview
            DrawGridPreview(currentPreviewPosition);
            //Overlap checking
            bool isBlocked = false;
            if(preventOverlap)
            {
                //Check slightly above the ground
                Vector3 checkPosition = currentPreviewPosition + (hit.normal * overlapRadius);
                isBlocked = Physics.CheckSphere(checkPosition, overlapRadius, overlapMask);
                //Draw overlap check sphere in scene view for debugging
                Handles.color = isBlocked ? new Color(1f,0f,0f,0.5f) : new Color(0f,1f,0f,0.5f);
                Handles.DrawSolidDisc(checkPosition, Vector3.up, overlapRadius);
                Handles.color = isBlocked ? Color.red : Color.green;
                Handles.DrawWireDisc(checkPosition, hit.normal, overlapRadius);
            }


            //Draw the ghost prefab with the correct rotation, scale and offset based on the surface normal and user settings
            if(ghostObject != null && nextPrefabIndex < prefabPallete.Count)
            {
                if(isBlocked)
                {
                    ghostObject.SetActive(false);
                    return;
                }
                else
                {
                    PalleteEntry currentItem = prefabPallete[nextPrefabIndex];
                    ghostObject.SetActive(true);
                    ghostObject.transform.localScale = Vector3.one * nextScale;
                    Quaternion finalRotation = matchSurfaceNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) * nextRotation : nextRotation;
                    ghostObject.transform.rotation = finalRotation;
                    ghostObject.transform.position = currentPreviewPosition + (finalRotation * currentItem.offset);
                }
            }
            //Check for input
            Event e = Event.current;
            if(e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            {
                if(!isBlocked)
                {
                   SpawnObject(currentPreviewPosition, hit.normal); 
                }
                e.Use();
            }
        }
        else
        {
            hasHit = false;
            if(ghostObject != null) ghostObject.SetActive(false);
        }
        sceneView.Repaint();
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
        else
        {
            nextRotation = Quaternion.identity;
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

            // Apply position WITH the new prefab's offset taken into account
            newObj.transform.position = obj.transform.position + (newObj.transform.rotation * currentItem.offset);
            
            Undo.RegisterCreatedObjectUndo(newObj, "Replaced Object");
            Undo.DestroyObjectImmediate(obj);   
        }
    }
}
