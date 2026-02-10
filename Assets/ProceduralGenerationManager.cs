using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralGenerationManager : MonoBehaviour
{
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject cellPrefab;

    [SerializeField] private int rows = 3;
    [SerializeField] private int columns = 4;
    [SerializeField] private Vector2 gridOffset;
    [SerializeField] private Vector2 cellSize = new Vector2(1, 1);

    private List<CellTile> cellPool = new List<CellTile>();

    private List<Sprite> sprites = new List<Sprite>();

    private int colorDiversity = 3;
    private Dictionary<Color, char> colorMapping = new Dictionary<Color, char>();
    private char colorMappingIndex = 'A';
    private void Start()
    {
       // Setup();
    }
    public void Setup()
    {
        sprites.Clear();
        foreach (Sprite item in Resources.LoadAll<Sprite>("Tiles"))
        {
            sprites.Add(item);
        }
        LoadSprites(colorDiversity, sprites[0].rect.size);

        foreach(CellTile tile in cellPool)
        {
            tile.isPlaced = false;
            tile.selfObject.SetActive(false);
        }

        int k = 0;
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                CellTile cellTile = null;
                if (k >= cellPool.Count)
                {
                    GameObject cell = Instantiate(cellPrefab);
                    cell.transform.parent = gridParent;
                                        
                    cellTile = new CellTile(cell);
                    cellTile.UpdateSprite(sprites[0]);
                    cellPool.Add(cellTile);
                }

                cellTile = cellPool[k];
                cellTile.selfObject.SetActive(true);
                cellTile.selfObject.transform.localPosition = gridOffset + new Vector2(i * cellSize.x, j * cellSize.y);

                k++;
            }
        }
    }


    private void LoadSprites(int colorDiversity, Vector2 spriteSize)
    {
        Vector2 offset = spriteSize / (colorDiversity * 2);

        foreach (Sprite sprite in sprites)
        {
            string[] marks = new string[4];
            int x = 0 , y = 0;
            for (int side = 0; side < 4; side++)    // origin corner: bottom-left;  directions : up, right, down, left
            {
                for (int i = 0; i < colorDiversity; i++)
                {
                    int step = (side == 0 || side == 1) ? i : (colorDiversity - 1 - i);

                    if (side == 0) //bottom - top
                    {
                        y = (int)(offset.y + step * spriteSize.y / colorDiversity);
                        x = (int)(offset.y);
                    }
                    else if (side == 1) //left - right
                    {
                        x = (int)(offset.x + step * spriteSize.x / colorDiversity);
                        y = (int)(offset.y + (colorDiversity - 1) * spriteSize.y / colorDiversity);
                    }
                    else if (side == 2) //top - bot
                    {
                        y = (int)(offset.y + step * spriteSize.y / colorDiversity);
                        x = (int)(offset.x + (colorDiversity - 1) * spriteSize.x / colorDiversity);
                    }
                    else if (side == 3) //right - left
                    {
                        x = (int)(offset.x + step * spriteSize.x / colorDiversity);
                        y = (int)(offset.y);
                    }
                    Color color = sprite.texture.GetPixel(x, y);
                    if (!colorMapping.ContainsKey(color))
                    {
                        colorMapping[color] = colorMappingIndex++;
                    }
                    marks[side] += colorMapping[color];
                    
                }
            }
            print((marks[0] + " " + marks[1] + " " + marks[2] + " " + marks[3]));
        }
    }



    //void VisualizePixelsChecked(int colorDiversity, Vector2 spriteSize)
    //{
    //    Sprite toCopy = sprites[4];
    //    Sprite dummy = Sprite.Create(new Texture2D(100, 100, toCopy.texture.format, toCopy.texture.mipmapCount, true), new Rect(0, 0, 100, 100), new Vector2(50, 50));
    //    Graphics.CopyTexture(toCopy.texture, dummy.texture);
    //    dummy.texture.Apply();

    //    int x, y;
    //    Vector2 offset = spriteSize / (colorDiversity * 2);
    //    for (int i = 0; i < colorDiversity; i++)
    //    {
    //        for (int j = 0; j < colorDiversity; j++)
    //        {
    //            x = (int)(offset.x + j * spriteSize.x / colorDiversity);
    //            y = (int)(offset.y + i * spriteSize.y / colorDiversity);

    //            Color color = dummy.texture.GetPixel(x, y);
    //            if (!colorMapping.ContainsKey(color))
    //            {
    //                colorMapping[color] = colorMappingIndex++;
    //            }
    //            // print(colorMapping[color]);

    //            for (int k = 0; k < 3; k++)
    //            {
    //                dummy.texture.SetPixel(x - k, y - k, Color.red);
    //                dummy.texture.SetPixel(x - k, y + k, Color.red);
    //                dummy.texture.SetPixel(x + k, y - k, Color.red);
    //                dummy.texture.SetPixel(x + k, y + k, Color.red);
    //                dummy.texture.Apply();
    //            }
    //            sprites[0] = dummy;
    //        }
    //    }
    //}
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Setup();
        }
    }
    private void GenerateMap()
    {
        int placed = 0;
        for (placed = 0; placed < columns * rows; placed++)
        {
            int leastEntrophy = int.MaxValue;
            Vector2Int pos = new Vector2Int(0, columns - 1);
            PlaceTile(pos);

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                
                }
            }
        }
    }
    private void PlaceTile(Vector2Int pos)
    {
        
    }
}
