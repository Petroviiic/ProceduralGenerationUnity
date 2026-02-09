using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellTile
{
    private SpriteRenderer spriteRenderer;

    public CellTile(GameObject cell)
    {
        spriteRenderer = cell.GetComponent<SpriteRenderer>();

    }
    public void UpdateCell(Sprite sprite)
    {
        spriteRenderer.sprite = sprite;
    }
}
