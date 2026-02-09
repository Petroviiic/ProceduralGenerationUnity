using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellTile
{
    public GameObject selfObject {  get; private set; }

    private SpriteRenderer spriteRenderer;

    public bool isPlaced = false;

    private List<SIDES> availableSides = new List<SIDES>();
    public CellTile(GameObject cell)
    {
        selfObject = cell;
        
        spriteRenderer = selfObject.GetComponent<SpriteRenderer>();
        selfObject.SetActive(false);

        isPlaced = false;
    }
    public void UpdateSprite(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }
}
