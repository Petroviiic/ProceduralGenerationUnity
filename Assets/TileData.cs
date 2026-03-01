using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "TileData")]

[System.Serializable]
public class TileData : ScriptableObject 
{
    public int colorDiversity;


    private List<Sprite> sprites = new List<Sprite>();
    public List<Sprite> Sprites { get { return sprites; } set { sprites = value; } }

    private List<Marks> spriteMarks = new List<Marks>();
    public List<Marks> SpriteMarks { get { return spriteMarks; } set { spriteMarks = value; } }
    //private Dictionary<Sprite, int[]> data = new Dictionary<Sprite, int[]>();
    //public Dictionary<Sprite, int[]> Data { get { return data; } set { data = value; } }
}

[System.Serializable]   
public class Marks
{
    public int[] marks;
}
