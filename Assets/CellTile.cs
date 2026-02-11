using System;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class CellTile 
{
    public GameObject selfObject {  get; private set; }

    private SpriteRenderer spriteRenderer;

    public bool isPlaced = false;

    private List<Sprite> options = new List<Sprite>();

    public CellTile(GameObject cell, List<Sprite> options)
    {
        selfObject = cell;
        
        spriteRenderer = selfObject.GetComponent<SpriteRenderer>();
        selfObject.SetActive(false);
        this.options = options;
        isPlaced = false;
    }
    public void UpdateSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }
    public string[] Place()
    {
        if (options.Count < 0)
        {
            Debug.LogWarning("No available options");
            return null;
        } 
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
    

    public void CheckCompatibility(string origin, int side)
    {
        List<Sprite> toRemove = new List<Sprite>();
        for(int i = 0; i < options.Count; i++)
        {
            string[] marks = TileData.instance.GetData(options[i]);
            if (side == 0)      //origin:left - this : right
            {
                Debug.Log((side, origin, marks[2], ReverseString(marks[2]), origin != ReverseString(marks[2])));
                if (origin != ReverseString(marks[2]))
                    toRemove.Add(options[i]);
            }
            else if (side == 1) //origin:up - this : down
            {
                Debug.Log((side, origin, marks[3], ReverseString(marks[3]), origin != ReverseString(marks[3])));
                if (origin != ReverseString(marks[3]))
                    toRemove.Add(options[i]);
            }
            else if (side == 2) //origin:right - this : left
            {
                Debug.Log((side, origin, marks[0], ReverseString(marks[0]), origin != ReverseString(marks[0])));
                if (origin != ReverseString(marks[0]))
                    toRemove.Add(options[i]);
            }
            else if (side == 3) //origin:down - this : up
            {
                Debug.Log((side, origin, marks[1], ReverseString(marks[1]), origin != ReverseString(marks[1])));
                if (origin != ReverseString(marks[1]))
                    toRemove.Add(options[i]);
            }
        }
        Debug.Log((side, toRemove.Count));
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
