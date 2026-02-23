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

    private int spriteSelection = 0;
    public CellTile(GameObject cell, List<Sprite> options)
    {
        selfObject = cell;
        selfObject.SetActive(false);
        spriteRenderer = selfObject.GetComponent<SpriteRenderer>();

        this.options = new List<Sprite>(options);
        //foreach(Sprite sprite in this.options)
        //{
        //    Debug.Log(sprite.name);
        //}
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
    public string[] Place(Vector2Int coords)
    {
        if (options.Count < 0 || spriteSelection >= options.Count)
        {
            Debug.LogWarning("No available options " + options.Count + " " + spriteSelection);
            return null;
        }
        coordinates = coords;
        isPlaced = true;
        //int rand = UnityEngine.Random.Range(0, GetOptionsCount());
        UpdateSprite(options[spriteSelection]);

        string[] marks = TileData.instance.GetData(options[spriteSelection]);
        options.RemoveAt(spriteSelection);
        return marks;
    }
    public void ResetCell(Sprite defaultSprite)
    {
        coordinates = new Vector2Int(-1, -1);
        isPlaced = false;
        spriteRenderer.sprite = defaultSprite;
        spriteSelection++;
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
