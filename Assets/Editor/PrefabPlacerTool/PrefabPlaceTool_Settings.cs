using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.VisualScripting;

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

    //Settings UI
    [MenuItem("Tools/Prefab Place Tool Settings")]
    public static void ShowSettings()
    {
        GetWindow<PrefabPlaceToolSettings>("Prefab Place Tool Settings");
    }

    void OnGUI()
    {
        GUILayout.Label("Prefab Place Tool Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck(); //If a change happens, tells ghost object to instantly update

        MaxScaleLimit = EditorGUILayout.FloatField(new GUIContent("Max Scale Limit", "The maximum scale allowed when random scale is enabled"), MaxScaleLimit);
        ManualRotationAmount = EditorGUILayout.FloatField(new GUIContent("Manual Rotation Amount", "The amount to rotate the object when pressing the rotate hotkeys"), ManualRotationAmount);

        EditorGUILayout.Space();
        GUILayout.Label("Preview Colors", EditorStyles.boldLabel);
        ValidPreviewColor = EditorGUILayout.ColorField(new GUIContent("Valid Placement Color", "The color of the preview when hovering over a valid placement surface"), ValidPreviewColor);
        InvalidPreviewColor = EditorGUILayout.ColorField(new GUIContent("Invalid Placement Color", "The color of the preview when hovering over an invalid placement surface"), InvalidPreviewColor);
        ErasePreviewColor = EditorGUILayout.ColorField(new GUIContent("Erase Mode Color", "The color of the preview when in erase mode"), ErasePreviewColor);

        if (EditorGUI.EndChangeCheck())
        {
            MaxScaleLimit = Mathf.Max(0.1f, MaxScaleLimit); //Clamp to a minimum value to prevent issues
            ManualRotationAmount = Mathf.Max(1f, ManualRotationAmount); //Clamp to a minimum value to prevent issues
            ValidPreviewColor = new Color(ValidPreviewColor.r, ValidPreviewColor.g, ValidPreviewColor.b, Mathf.Clamp01(ValidPreviewColor.a)); //Clamp alpha to 0-1
            InvalidPreviewColor = new Color(InvalidPreviewColor.r, InvalidPreviewColor.g, InvalidPreviewColor.b, Mathf.Clamp01(InvalidPreviewColor.a)); //Clamp alpha to 0-1
            ErasePreviewColor = new Color(ErasePreviewColor.r, ErasePreviewColor.g, ErasePreviewColor.b, Mathf.Clamp01(ErasePreviewColor.a)); //Clamp alpha to 0-1
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
            SceneView.RepaintAll();
        }
    }
}
