using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData : MonoBehaviour 
{
    //ovo moze biti i scriptable object. first make it work, then make it beautiful

    private Dictionary<Sprite, string[]> data = new Dictionary<Sprite, string[]>();

    public static TileData instance;
    private void Awake()
    {
        instance = this;
    }
    public void UpdateData(Sprite sprite, string[] colorData)
    {
        data[sprite] = colorData;
    }

    public string[] GetData(Sprite sprite)
    {
        if (data.ContainsKey(sprite))
        {
            return data[sprite];
        }
        return null;
    }
}
