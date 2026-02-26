using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class CellTile
{
    public GameObject selfObject { get; private set; }
    private SpriteRenderer spriteRenderer;

    private List<Sprite> options = new List<Sprite>();
    private Vector2Int coordinates = new Vector2Int(-1, -1);
    public bool isPlaced = false;

    public int spriteSelection { get; private set; }
    public CellTile(GameObject cell, List<Sprite> options)
    {
        selfObject = cell;
        selfObject.SetActive(false);
        spriteRenderer = selfObject.GetComponent<SpriteRenderer>();

        this.options = new List<Sprite>(options);
        ShuffleOptions();
        coordinates = new Vector2Int(-1, -1);
        isPlaced = false;
    }
    private void ShuffleOptions()
    {
        int count = options.Count;
        int last = count - 1;
        for (int i = 0; i < last; i++)
        {
            int r = UnityEngine.Random.Range(i, count);
            Sprite tmp = this.options[i];
            this.options[i] = this.options[r];
            this.options[r] = tmp;
        }
    }

    public void SetOptions(List<Sprite> options)
    {
        spriteSelection = 0;
        this.options = options;
        ShuffleOptions();
    }
    public void UpdateSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }
    public int[] Place(Vector2Int coords)
    {
        if (options.Count <= 0)
        {
            Debug.LogWarning("No available options " + options.Count + " " + spriteSelection);
            return null;
        }
        coordinates = coords;
        isPlaced = true;
        UpdateSprite(options[options.Count - 1]);

        int[] marks = TileData.instance.GetData(options[options.Count - 1]);
        return marks;
    }
    public void ResetCell(Sprite defaultSprite, int spriteSelection)
    {
        if (options.Count > 0)
        {
            options.RemoveAt(options.Count - 1); 
        }
        coordinates = new Vector2Int(-1, -1);
        isPlaced = false;
        spriteRenderer.sprite = defaultSprite;
        this.spriteSelection = spriteSelection;
    }
    public void RestoreOptions(List<Sprite> savedOptions)
    {
        this.options = new List<Sprite>(savedOptions);
    }
    public List<Sprite> GetOptions()
    {
        return options;
    }
    public int GetOptionsCount()
    {
        return options.Count;
    }
    public Vector2Int GetCoordinates()
    {
        return coordinates;
    }

    public void CheckCompatibility(int origin, int side)
    {
        List<Sprite> toRemove = new List<Sprite>();
        for(int i = 0; i < options.Count; i++)
        {
            int[] marks = TileData.instance.GetData(options[i]);
            if (side == 0)      //origin:left - this : right
            {
                if (origin != marks[2])
                    toRemove.Add(options[i]);
            }
            else if (side == 1) //origin:up - this : down
            {
                if (origin != marks[3])
                    toRemove.Add(options[i]);
            }
            else if (side == 2) //origin:right - this : left
            {
                if (origin != marks[0])
                    toRemove.Add(options[i]);
            }
            else if (side == 3) //origin:down - this : up
            {
                if (origin != marks[1])
                    toRemove.Add(options[i]);
            }
        }
        foreach(Sprite i in toRemove)
        {
            options.Remove(i);
        }
    }
}
