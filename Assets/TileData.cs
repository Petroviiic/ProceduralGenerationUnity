using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData : MonoBehaviour 
{
    //ovo moze biti i scriptable object. first make it work, then make it beautiful

    private Dictionary<Sprite, int[]> data = new Dictionary<Sprite, int[]>();

    public static TileData instance;
    private void Awake()
    {
        instance = this;
    }
    public void UpdateData(Sprite sprite, int[] colorData)
    {
        data[sprite] = colorData;
    }

    public int[] GetData(Sprite sprite)
    {
        if (data.ContainsKey(sprite))
        {
            return data[sprite];
        }
        Debug.LogWarning("no available data for sprite " + sprite.name);
        return null;
    }
}
