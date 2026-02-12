using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class CellTile
{
    public GameObject selfObject { get; private set; }

    private SpriteRenderer spriteRenderer;

    public bool isPlaced = false;

    private List<Sprite> options = new List<Sprite>();

    private Vector2Int coordinates = new Vector2Int(-1, -1);
    public CellTile(GameObject cell, List<Sprite> options)
    {
        selfObject = cell;

        coordinates = new Vector2Int(-1, -1);
        spriteRenderer = selfObject.GetComponent<SpriteRenderer>();
        selfObject.SetActive(false);
        this.options = options;
        isPlaced = false;
    }
    public void SetOptions(List<Sprite> options)
    {
        this.options = options;
    }
    public void UpdateSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }
    public string[] Place(Vector2Int coords)
    {
        if (options.Count < 0)
        {
            Debug.LogWarning("No available options");
            return null;
        }
        coordinates = coords;
        isPlaced = true;
        int rand = UnityEngine.Random.Range(0, GetOptionsCount());
        UpdateSprite(options[rand]);

        string[] marks = TileData.instance.GetData(options[rand]);
        options.RemoveAt(rand);
        return marks;
    }
    public int GetOptionsCount()
    {
        return options.Count;
    }
    public Vector2Int GetCoordinates()
    {
        return coordinates;
    }

    public void CheckCompatibility(string origin, int side)
    {
        List<Sprite> toRemove = new List<Sprite>();
        for(int i = 0; i < options.Count; i++)
        {
            string[] marks = TileData.instance.GetData(options[i]);
            if (side == 0)      //origin:left - this : right
            {
                if (origin != ReverseString(marks[2]))
                    toRemove.Add(options[i]);
            }
            else if (side == 1) //origin:up - this : down
            {
                if (origin != ReverseString(marks[3]))
                    toRemove.Add(options[i]);
            }
            else if (side == 2) //origin:right - this : left
            {
                if (origin != ReverseString(marks[0]))
                    toRemove.Add(options[i]);
            }
            else if (side == 3) //origin:down - this : up
            {
                if (origin != ReverseString(marks[1]))
                    toRemove.Add(options[i]);
            }
        }
        foreach(Sprite i in toRemove)
        {
            options.Remove(i);
        }
    }

    public static string ReverseString(string value)
    {
        Span<char> charSpan = value.ToCharArray();

        int left = 0, right = charSpan.Length - 1;
        while (left < right)
        {
            char temp = charSpan[left];
            charSpan[left] = charSpan[right];
            charSpan[right] = temp;

            left++;
            right--;
        }

        return new string(charSpan);
    }
}
