using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TileData), false)]
public class TileDataEditor : Editor
{
    private TileData tileData => target as TileData;

    public override void OnInspectorGUI()
    {
 
    }
}
