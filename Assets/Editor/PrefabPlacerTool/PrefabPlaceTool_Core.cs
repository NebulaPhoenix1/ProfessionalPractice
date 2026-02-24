using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//Partial classes to define a class over multiple scripts
public partial class PrefabPlaceTool : EditorWindow
{
    bool isToolActive = false;
    [SerializeField] List<PalleteEntry> prefabPallete = new List<PalleteEntry>();
    
    //These let us draw the object list in Inspector
    SerializedObject serializedObject;
    SerializedProperty propPallete;
    SerializedProperty propPlacementMask;
    SerializedProperty propParentContainer;
    SerializedProperty propOverlapMask; //Which layers we are allowed to overlap with if the collision check is enabled

    //Placement settings
    bool matchSurfaceNormal = true;
    [SerializeField] LayerMask placementMask = ~0; //Default to everything
    [SerializeField] Transform parentContainer = null; //Option to parent all spawned objects under a specific transform for organisation
    //Overlap prevention settings
    //If enabled, the tool will check for existing colliders within a certain radius of the spawn point and prevent spawning if any are found. This can help prevent accidentally placing multiple objects on top of each other.
    bool preventOverlap = false;
    float overlapRadius = 0.5f;
    [SerializeField] LayerMask overlapMask = ~0; //Default to everything; layers to check for collisions with.

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
        propParentContainer = serializedObject.FindProperty("parentContainer");
        propOverlapMask = serializedObject.FindProperty("overlapMask");

        //Hook into scene view updating 
        SceneView.duringSceneGui += OnSceneGUI;
    }

    //Clean up listeners when window is closed
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroyGhostObject();
    }
}
