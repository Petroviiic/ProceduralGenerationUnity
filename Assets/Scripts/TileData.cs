using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TileData")]

[System.Serializable]
public class TileData : ScriptableObject 
{
    public int colorDiversity;

    public List<Sprite> sprites = new List<Sprite>();
    public List<Marks> spriteMarks = new List<Marks>();

    public bool UseDiagonalMovementPathfinding;
}

[System.Serializable]   
public class Marks
{
    public int[] marks;
}
