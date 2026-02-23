using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;

//Class so each prefab can have its own offset value
[System.Serializable]
public class PalleteEntry
{
    public GameObject prefab;
    public Vector3 offset;
}

public class PrefabPlaceTool : EditorWindow
{
    bool isToolActive = false;
    [SerializeField] List<PalleteEntry> prefabPallete = new List<PalleteEntry>();
    
    //These let us draw the object list in Inspector
    SerializedObject serializedObject;
    SerializedProperty propPallete;
    SerializedProperty propPlacementMask;

    //Placement settings
    bool matchSurfaceNormal = true;
    [SerializeField] LayerMask placementMask = ~0; //Default to everything

    //Grid Settings
    bool useGrid = false;
    float gridSize = 1.0f;
    bool snapHeight = false;

    //Randomisation Settings
    bool randomRotation = false;
    UnityEngine.Vector3 minRotation = UnityEngine.Vector3.zero;
    UnityEngine.Vector3 maxRotation = new UnityEngine.Vector3(0, 360, 0);
    bool randomScale = false;
    float minScale = 0.8f;
    float maxScale = 1.2f;

    //Preview settings
    UnityEngine.Vector3 currentPreviewPosition; //Where ghost object is currently previewed
    bool hasHit = false; //Whether the raycast has hit a valid surface

    //Ghost object values (these objects let us preview what we are going to place before we place it)
    GameObject ghostObject;
    int nextPrefabIndex = 0;
    Quaternion nextRotation = Quaternion.identity;
    float nextScale = 1.0f;


    [MenuItem("Tools/Prefab Place Tool")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPlaceTool>("Prefab Place Tool");
    }

    private void OnEnable()
    {
        //Setup serialized properties for prefab list
        ScriptableObject target = this;
        serializedObject = new SerializedObject(target);
        propPallete = serializedObject.FindProperty("prefabPallete");
        propPlacementMask = serializedObject.FindProperty("placementMask");

        //Hook into scene view updating 
        SceneView.duringSceneGui += OnSceneGUI;
    }

    //Clean up listeners when window is closed
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroyGhostObject();
    }

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
        EditorGUILayout.PropertyField(propPlacementMask, new GUIContent("Valid Placement Layers"));
        matchSurfaceNormal = EditorGUILayout.Toggle("Match Surface Normal", matchSurfaceNormal);
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
            //Draw the ghost prefab with the correct rotation, scale and offset based on the surface normal and user settings
            if(ghostObject != null && nextPrefabIndex < prefabPallete.Count)
            {
                PalleteEntry currentItem = prefabPallete[nextPrefabIndex];
                ghostObject.SetActive(true);
                ghostObject.transform.localScale = Vector3.one * nextScale;
                Quaternion finalRotation = matchSurfaceNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) * nextRotation : nextRotation;
                ghostObject.transform.rotation = finalRotation;
                ghostObject.transform.position = currentPreviewPosition + (finalRotation * currentItem.offset);
            }
            //Check for input
            Event e = Event.current;
            if(e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
            {
                SpawnObject(currentPreviewPosition, hit.normal);
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
        int index = validIndices[Random.Range(0, validIndices.Count)];
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
            Vector3 start = center + new Vector3(i * gridSize, 0, -size);
            Vector3 end = center + new Vector3(i * gridSize, 0, size);
            start.y = center.y;
            end.y = center.y;
            Handles.DrawLine(start, end);
        }
        //Draw Z lines
        for(int j = -lines; j <= lines; j++)
        {
            Vector3 start = center + new Vector3(-size, 0, j * gridSize);
            Vector3 end = center + new Vector3(size, 0, j * gridSize);
            start.y = center.y;
            end.y = center.y;
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
