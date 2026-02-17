using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class PrefabPlaceTool : EditorWindow
{
    bool isToolActive = false;
    [SerializeField] List<GameObject> prefabPallete = new List<GameObject>();
    
    //These let us draw the object list in Inspector
    SerializedObject serializedObject;
    SerializedProperty propPallete;

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

    UnityEngine.Vector3 currentPreviewPosition; //Where ghost object is currently previewed
    bool hasHit = false; //Whether the raycast has hit a valid surface

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
        //Hook into scene view updating 
        SceneView.duringSceneGui += OnSceneGUI;
    }

    //Clean up listeners when window is closed
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    //The UI
    void OnGUI()
    {
        GUILayout.Label("Prefab Placer Tool", EditorStyles.boldLabel);
        
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
        serializedObject.ApplyModifiedProperties();
        EditorGUILayout.Space();
        
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
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if(!isToolActive) return;
        //Shoot a ray cats
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        RaycastHit hit;
        if(Physics.Raycast(ray, out hit))
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
            //Draw the ghost prefab
            Handles.color = Color.cyan;
            Handles.DrawWireCube(currentPreviewPosition, Vector3.one * (useGrid ? gridSize : 1.0f));
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
        }
        sceneView.Repaint();
    }

    void DrawGridPreview(Vector3 center)
    {
        if(!useGrid) return;
        Handles.color = new Color(1f,1f,1f,0.5f);
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
        if(prefabPallete.Count == 0)
        {
            Debug.LogWarning("Prefab Pallete is empty! Please add some prefabs to spawn.");
            return;
        }
        //Pick random prefab
        GameObject prefab = prefabPallete[Random.Range(0, prefabPallete.Count)];
        if(prefab == null)
        {
            Debug.LogWarning("One of the prefabs in the pallete is null! Please remove any empty slots.");
            return;
        }
        //Instantiate
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        //Register Undo
        Undo.RegisterCreatedObjectUndo(newObj, "Spawned Prefab");
        //Set Pos
        newObj.transform.position = position;
        //Apply rotation
        if(randomRotation)
        {
            newObj.transform.rotation = Quaternion.Euler(
                Random.Range(minRotation.x, maxRotation.x),
                Random.Range(minRotation.y, maxRotation.y),
                Random.Range(minRotation.z, maxRotation.z)
            );
        }
        else
        {
            newObj.transform.up = normal; //Align to surface normal if no random rotation
        }
        //Apply scale
        if(randomScale)
        {
            float scale = Random.Range(minScale, maxScale);
            newObj.transform.localScale = Vector3.one * scale;
        }
    }

    void ReplaceSelectedObjects()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        if(selectedObjects.Length == 0 || prefabPallete.Count == 0)
        {
            Debug.LogWarning("No objects selected or prefab pallete is empty! Please select some objects and add prefabs to the pallete.");
            return;
        }
        foreach(GameObject obj in selectedObjects)
        {
            //Pick new prefab
            GameObject prefab = prefabPallete[Random.Range(0, prefabPallete.Count)];
            GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            //Copy transform data before destroying
            newObj.transform.position = obj.transform.position;
            newObj.transform.rotation = obj.transform.rotation;
            newObj.transform.localScale = obj.transform.localScale;
            newObj.transform.parent = obj.transform.parent;
            //Register Undo
            Undo.RegisterCreatedObjectUndo(newObj, "Replaced Object");
            //Destroy old object
            Undo.DestroyObjectImmediate(obj);   
        }
    }

}
