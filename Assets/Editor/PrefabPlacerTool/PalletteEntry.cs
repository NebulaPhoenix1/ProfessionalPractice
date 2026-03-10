using UnityEngine;

//Class so each prefab can have its own offset value
[System.Serializable]
public class PalleteEntry
{
    public GameObject prefab;
    public Vector3 offset;
    [Range(0,100f)]
    public float weight = 100f; //Default to 100 so if the user doesn't change it, it will be used in the random selection process as normal.
}
