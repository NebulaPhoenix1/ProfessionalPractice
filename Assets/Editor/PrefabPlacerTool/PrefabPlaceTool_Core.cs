using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

//Partial classes to define a class over multiple scripts
public partial class PrefabPlaceTool : EditorWindow
{
    bool isToolActive = false;
    bool isErasing = false;
    [SerializeField] List<PalleteEntry> prefabPallete = new List<PalleteEntry>();
    
    //These let us draw the object list in Inspector
    SerializedObject serializedObject;
    SerializedProperty propPallete;
    SerializedProperty propPlacementMask;
    SerializedProperty propParentContainer;
    SerializedProperty propOverlapMask; //Which layers we are allowed to overlap with if the collision check is enabled
    SerializedProperty propEraseMask; //Which layers we are allowed to erase when in erase mode

    //Placement settings
    bool matchSurfaceNormal = true;
    [SerializeField] LayerMask placementMask = ~0; //Default to everything
    [SerializeField] Transform parentContainer = null; //Option to parent all spawned objects under a specific transform for organisation
    
    //Layer override settings
    bool overridePrefabLayer = false;
    int spawnLayer = 0; //Default layer
    
    //Overlap prevention settings
    //If enabled, the tool will check for existing colliders within a certain radius of the spawn point and prevent spawning if any are found. This can help prevent accidentally placing multiple objects on top of each other.
    bool preventOverlap = false;
    float overlapRadius = 0.5f;
    [SerializeField] LayerMask overlapMask = ~0; //Default to everything; layers to check for collisions with.

    //Erase mode settings
    [SerializeField] LayerMask eraseMask = ~0; //Default to everything; layers that can be erased when in erase mode
    float eraseRadius = 2.0f;

    //Grid Settings
    bool useGrid = false;
    float gridSize = 1.0f;
    bool snapHeight = false;

    //Rotation snapping settings
    bool snapRotation = false;
    float snapAngle = 90.0f;

    //Randomisation Settings
    bool randomSelection = true; //Whether to randomly select a prefab or if the user can select one with arrow keys
    bool randomRotation = false;
    UnityEngine.Vector3 minRotation = UnityEngine.Vector3.zero;
    UnityEngine.Vector3 maxRotation = new UnityEngine.Vector3(0, 360, 0);
    bool randomScale = false;
    float minScale = 0.8f;
    float maxScale = 1.2f;

    //Paint brush settings
    bool usePaintBrush = false;
    float brushRadius = 5.0f;
    int brushDensity = 5; //How many prefabs to spawn per brush stroke
    float brushSpacing = 2.0f; //Minimum distance between spawned prefabs when using the paint brush
    UnityEngine.Vector3 lastPaintPosition = Vector3.positiveInfinity; 

    //Preview settings
    UnityEngine.Vector3 currentPreviewPosition; //Where ghost object is currently previewed
    bool hasHit = false; //Whether the raycast has hit a valid surface

    //Ghost object values (these objects let us preview what we are going to place before we place it)
    GameObject ghostObject;
    int nextPrefabIndex = 0;
    Quaternion nextRotation = Quaternion.identity;
    float nextScale = 1.0f;


    [MenuItem("Tools/Open Prefab Place Tool Window")]
    public static void ShowWindow()
    {
        GetWindow<PrefabPlaceTool>("Prefab Place Tool");
    }

    [MenuItem("Tools/Toggle Prefab Place Tool %#w")] //Shortcut Ctrl/Cmd + Shift + W
    public static void ToggleToolWithHotKey()
    {
        PrefabPlaceTool window = GetWindow<PrefabPlaceTool>("Prefab Place Tool");
        window.isToolActive = !window.isToolActive;
        if (!window.isToolActive)
        {
            window.DestroyGhostObject();
            SceneView.RepaintAll();
        }
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
        propEraseMask = serializedObject.FindProperty("eraseMask");

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
