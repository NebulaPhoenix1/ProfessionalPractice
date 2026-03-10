using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Data.Common;

public class PrefabPlaceToolSettings : EditorWindow
{
    //We save settings for the tool to editor preferences so they persist between sessions
    public static float MaxScaleLimit
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_MaxScaleLimit", 3.0f); //3 is the default value if none is set
        set => EditorPrefs.SetFloat("PrefabPlaceTool_MaxScaleLimit", value); //This sets the value to what the user inputs in the UI
    }

    public static float ManualRotationAmount
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_ManualRotationAmount", 45.0f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_ManualRotationAmount", value);
    }

    public static float MaxGridSize
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_MaxGridSize", 10.0f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_MaxGridSize", value);
    }

    public static float GridSizePreset1
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_GridSizePreset1", 0.5f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_GridSizePreset1", value);
    }

    public static float GridSizePreset2
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_GridSizePreset2", 1.0f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_GridSizePreset2", value);
    }

    public static float GridSizePreset3
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_GridSizePreset3", 2.0f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_GridSizePreset3", value);
    }

    public static float RotationSnappingPreset1
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_RotationSnappingPreset1", 15.0f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_RotationSnappingPreset1", value);
    }

    public static float RotationSnappingPreset2
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_RotationSnappingPreset2", 45.0f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_RotationSnappingPreset2", value);
    }

    public static float RotationSnappingPreset3
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_RotationSnappingPreset3", 90.0f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_RotationSnappingPreset3", value);
    }

    public static float RotationSnappingPreset4
    {
        get => EditorPrefs.GetFloat("PrefabPlaceTool_RotationSnappingPreset4", 180.0f);
        set => EditorPrefs.SetFloat("PrefabPlaceTool_RotationSnappingPreset4", value);
    }

    //Colors cannot be saved to Editor Prefs directly (for some reason)
    //So I am going to save them as 4 individual floats (RGBA) and then convert them back to a color when we need to use them
    public static Color ValidPreviewColor
    {
        get
        {
            float r = EditorPrefs.GetFloat("PrefabPlaceTool_ValidPreviewColor_R", 0f);
            float g = EditorPrefs.GetFloat("PrefabPlaceTool_ValidPreviewColor_G", 1f);
            float b = EditorPrefs.GetFloat("PrefabPlaceTool_ValidPreviewColor_B", 0f);
            float a = EditorPrefs.GetFloat("PrefabPlaceTool_ValidPreviewColor_A", 0.5f);
            return new Color(r, g, b, a);
        }
        set
        {
            EditorPrefs.SetFloat("PrefabPlaceTool_ValidPreviewColor_R", value.r);
            EditorPrefs.SetFloat("PrefabPlaceTool_ValidPreviewColor_G", value.g);
            EditorPrefs.SetFloat("PrefabPlaceTool_ValidPreviewColor_B", value.b);
            EditorPrefs.SetFloat("PrefabPlaceTool_ValidPreviewColor_A", value.a);
        }
    }

    public static Color InvalidPreviewColor
    {
        get
        {
            float r = EditorPrefs.GetFloat("PrefabPlaceTool_InvalidPreviewColor_R", 1f);
            float g = EditorPrefs.GetFloat("PrefabPlaceTool_InvalidPreviewColor_G", 0f);
            float b = EditorPrefs.GetFloat("PrefabPlaceTool_InvalidPreviewColor_B", 0f);
            float a = EditorPrefs.GetFloat("PrefabPlaceTool_InvalidPreviewColor_A", 0.5f);
            return new Color(r, g, b, a);
        }
        set
        {
            EditorPrefs.SetFloat("PrefabPlaceTool_InvalidPreviewColor_R", value.r);
            EditorPrefs.SetFloat("PrefabPlaceTool_InvalidPreviewColor_G", value.g);
            EditorPrefs.SetFloat("PrefabPlaceTool_InvalidPreviewColor_B", value.b);
            EditorPrefs.SetFloat("PrefabPlaceTool_InvalidPreviewColor_A", value.a);
        }
    }

    public static Color ErasePreviewColor
    {
        get
        {
            float r = EditorPrefs.GetFloat("PrefabPlaceTool_ErasePreviewColor_R", 1f);
            float g = EditorPrefs.GetFloat("PrefabPlaceTool_ErasePreviewColor_G", 1f);
            float b = EditorPrefs.GetFloat("PrefabPlaceTool_ErasePreviewColor_B", 0f);
            float a = EditorPrefs.GetFloat("PrefabPlaceTool_ErasePreviewColor_A", 0.4f);
            return new Color(r, g, b, a);
        }
        set
        {
            EditorPrefs.SetFloat("PrefabPlaceTool_ErasePreviewColor_R", value.r);
            EditorPrefs.SetFloat("PrefabPlaceTool_ErasePreviewColor_G", value.g);
            EditorPrefs.SetFloat("PrefabPlaceTool_ErasePreviewColor_B", value.b);
            EditorPrefs.SetFloat("PrefabPlaceTool_ErasePreviewColor_A", value.a);
        }
    }

    public static bool ShowSceneUI
    {
        get => EditorPrefs.GetBool("PrefabPlaceTool_ShowSceneUI", true); //Default to true
        set => EditorPrefs.SetBool("PrefabPlaceTool_ShowSceneUI", value);
    }

    public static bool ShowHotKeysInScene
    {
        get => EditorPrefs.GetBool("PrefabPlaceTool_ShowHotKeysInScene", true); //Default to true
        set => EditorPrefs.SetBool("PrefabPlaceTool_ShowHotKeysInScene", value);
    }

    //Settings UI
    [MenuItem("Tools/Prefab Place Tool Settings #%M")] //Shortcut Ctrl/Cmd + Shift + C
    public static void ShowSettings()
    {
        GetWindow<PrefabPlaceToolSettings>("Prefab Place Tool Settings");
    }

    void OnGUI()
    {
        GUILayout.Label("Prefab Place Tool Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck(); //If a change happens, tells ghost object to instantly update

        GUILayout.BeginVertical("box");
        GUILayout.Label("Scene View UI Settings", EditorStyles.boldLabel);
        ShowSceneUI = EditorGUILayout.Toggle(new GUIContent("Show Scene UI", "Toggles heads up display in scene view"), ShowSceneUI);
        if(ShowSceneUI)
        {
            ShowHotKeysInScene = EditorGUILayout.Toggle(new GUIContent("Show Hotkeys in Scene", "Toggles whether to show the hotkeys for each action in the scene view UI"), ShowHotKeysInScene);
        }
        GUILayout.EndVertical();
        EditorGUILayout.Space();

        GUILayout.BeginVertical("box");
        GUILayout.Label("Grid Value Settings", EditorStyles.boldLabel);
        MaxGridSize = EditorGUILayout.FloatField(new GUIContent("Max Grid Size", "The maximum grid size allowed"), MaxGridSize);
        GridSizePreset1 = EditorGUILayout.FloatField(new GUIContent("Grid Size Preset 1", "The grid size used when pressing the first grid preset button"), GridSizePreset1);
        GridSizePreset2 = EditorGUILayout.FloatField(new GUIContent("Grid Size Preset 2", "The grid size used when pressing the second grid preset button"), GridSizePreset2);
        GridSizePreset3 = EditorGUILayout.FloatField(new GUIContent("Grid Size Preset 3", "The grid size used when pressing the third grid preset button"), GridSizePreset3);
        GUILayout.EndVertical();
        EditorGUILayout.Space();

        GUILayout.BeginVertical("box");
        GUILayout.Label("Rotation Value Settings", EditorStyles.boldLabel);
        ManualRotationAmount = EditorGUILayout.FloatField(new GUIContent("Manual Rotation Amount", "The amount to rotate the object when pressing the rotate hotkeys"), ManualRotationAmount);
        RotationSnappingPreset1 = EditorGUILayout.FloatField(new GUIContent("Rotation Snap Preset 1", "The rotation snapping value for the first preset button"), RotationSnappingPreset1);
        RotationSnappingPreset2 = EditorGUILayout.FloatField(new GUIContent("Rotation Snap Preset 2", "The rotation snapping value for the second preset button"), RotationSnappingPreset2);
        RotationSnappingPreset3 = EditorGUILayout.FloatField(new GUIContent("Rotation Snap Preset 3", "The rotation snapping value for the third preset button"), RotationSnappingPreset3);
        RotationSnappingPreset4 = EditorGUILayout.FloatField(new GUIContent("Rotation Snap Preset 4", "The rotation snapping value for the fourth preset button"), RotationSnappingPreset4);
        GUILayout.EndVertical();
        EditorGUILayout.Space();

        MaxScaleLimit = EditorGUILayout.FloatField(new GUIContent("Max Scale Limit", "The maximum scale allowed when random scale is enabled"), MaxScaleLimit);

        GUILayout.BeginVertical("box");
        GUILayout.Label("Preview Colors", EditorStyles.boldLabel);
        ValidPreviewColor = EditorGUILayout.ColorField(new GUIContent("Valid Placement Color", "The color of the preview when hovering over a valid placement surface"), ValidPreviewColor);
        InvalidPreviewColor = EditorGUILayout.ColorField(new GUIContent("Invalid Placement Color", "The color of the preview when hovering over an invalid placement surface"), InvalidPreviewColor);
        ErasePreviewColor = EditorGUILayout.ColorField(new GUIContent("Erase Mode Color", "The color of the preview when in erase mode"), ErasePreviewColor);
        GUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            MaxScaleLimit = Mathf.Max(0.1f, MaxScaleLimit); //Clamp to a minimum value to prevent issues
            ManualRotationAmount = Mathf.Max(1f, ManualRotationAmount); 
            ValidPreviewColor = new Color(ValidPreviewColor.r, ValidPreviewColor.g, ValidPreviewColor.b, Mathf.Clamp01(ValidPreviewColor.a)); //Clamp alpha to 0-1
            InvalidPreviewColor = new Color(InvalidPreviewColor.r, InvalidPreviewColor.g, InvalidPreviewColor.b, Mathf.Clamp01(InvalidPreviewColor.a)); //Clamp alpha to 0-1
            ErasePreviewColor = new Color(ErasePreviewColor.r, ErasePreviewColor.g, ErasePreviewColor.b, Mathf.Clamp01(ErasePreviewColor.a)); //Clamp alpha to 0-1
            //Grid settings
            MaxGridSize = Mathf.Max(0.1f, MaxGridSize); 
            GridSizePreset1 = Mathf.Clamp(GridSizePreset1, 0.1f, MaxGridSize);
            GridSizePreset2 = Mathf.Clamp(GridSizePreset2, 0.1f, MaxGridSize);
            GridSizePreset3 = Mathf.Clamp(GridSizePreset3, 0.1f, MaxGridSize);
            //Rotation snapping preset settings
            //Ensure they are all between 1 and 360 degrees to prevent issues with the snapping function
            RotationSnappingPreset1 = Mathf.Clamp(RotationSnappingPreset1, 1f, 360f);
            RotationSnappingPreset2 = Mathf.Clamp(RotationSnappingPreset2, 1f, 360f);
            RotationSnappingPreset3 = Mathf.Clamp(RotationSnappingPreset3, 1f, 360f);
            RotationSnappingPreset4 = Mathf.Clamp(RotationSnappingPreset4, 1f, 360f);
            SceneView.RepaintAll(); //Repaint scene view to update preview colors immediately
        }

        EditorGUILayout.Space();
        //Reset to defaults button (deletes the keys from editor prefs, which will cause the getters to return the default values)
        if (GUILayout.Button("Reset to Defaults"))
        {
            EditorPrefs.DeleteKey("PrefabPlaceTool_MaxScaleLimit");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ManualRotationAmount");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ValidPreviewColor_R");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ValidPreviewColor_G");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ValidPreviewColor_B");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ValidPreviewColor_A");
            EditorPrefs.DeleteKey("PrefabPlaceTool_InvalidPreviewColor_R");
            EditorPrefs.DeleteKey("PrefabPlaceTool_InvalidPreviewColor_G");
            EditorPrefs.DeleteKey("PrefabPlaceTool_InvalidPreviewColor_B");
            EditorPrefs.DeleteKey("PrefabPlaceTool_InvalidPreviewColor_A");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ErasePreviewColor_R");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ErasePreviewColor_G");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ErasePreviewColor_B");   
            EditorPrefs.DeleteKey("PrefabPlaceTool_ErasePreviewColor_A");
            EditorPrefs.DeleteKey("PrefabPlaceTool_MaxGridSize");
            EditorPrefs.DeleteKey("PrefabPlaceTool_GridSizePreset1");
            EditorPrefs.DeleteKey("PrefabPlaceTool_GridSizePreset2");
            EditorPrefs.DeleteKey("PrefabPlaceTool_GridSizePreset3");
            EditorPrefs.DeleteKey("PrefabPlaceTool_RotationSnappingPreset1");
            EditorPrefs.DeleteKey("PrefabPlaceTool_RotationSnappingPreset2");
            EditorPrefs.DeleteKey("PrefabPlaceTool_RotationSnappingPreset3");
            EditorPrefs.DeleteKey("PrefabPlaceTool_RotationSnappingPreset4");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ShowSceneUI");
            EditorPrefs.DeleteKey("PrefabPlaceTool_ShowHotKeysInScene");
            SceneView.RepaintAll();
        }
    }
}
