using UnityEngine;
using System.Collections.Generic;

//Save/Loading palette presets functions by using scriptable objects which store all the prefabs with data
//When they get loaded, a check will happen to see if there are null prefabs and they get removed from the palette

[CreateAssetMenu(fileName = "New Prefab Palette", menuName = "Tools/Prefab Palette Preset")]
public class PrefabPlaceToolPalettePreset : ScriptableObject
{
    [Tooltip("The saved list of prefabs, offsets and weights.")]
    public List<PalleteEntry> prefabPallete = new List<PalleteEntry>();
}
