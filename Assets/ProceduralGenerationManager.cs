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
    private List<CellTile> activeCells = new List<CellTile>();

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
        Vector3 gridPosition = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10));
        gridPosition += new Vector3(-cellSize.x * (columns - 1) / 2, cellSize.y * (rows - 1) / 2);
        gridParent.localPosition = gridPosition;

        float aspectRatio = (float)Screen.width / (float)Screen.height;

        float sizeByHeight = rows * cellSize.y / 2f;
        float sizeByWidth = (columns * cellSize.x / 2f) / aspectRatio;
        float padding = 1.1f;
        float finalSize = Mathf.Max(sizeByHeight, sizeByWidth) * padding;
        Camera.main.orthographicSize = Mathf.Max(5f, finalSize);


        sprites.Clear();
        foreach (Sprite item in Resources.LoadAll<Sprite>("Tiles"))
        {
            sprites.Add(item);
        }
        LoadSprites(colorDiversity, sprites[0].rect.size);


        foreach (CellTile tile in activeCells)
        {
            tile.isPlaced = false;
            tile.selfObject.SetActive(false);
        }
        activeCells.Clear();

        int k = 0;
        for (int j = 0; j < rows; j++)
        {
            for (int i = 0; i < columns; i++)
            {
                CellTile cellTile = null;
                if (k >= cellPool.Count)
                {
                    GameObject cell = Instantiate(cellPrefab);
                    cell.transform.parent = gridParent;
                                        
                    cellTile = new CellTile(cell, sprites.GetRange(1, sprites.Count-1));
                    cellTile.UpdateSprite(sprites[0]);
                    cellPool.Add(cellTile);
                }

                cellTile = cellPool[k];
                cellTile.selfObject.SetActive(true);
                cellTile.selfObject.transform.localPosition = gridOffset + new Vector2(i * cellSize.x, -j * cellSize.y);

                activeCells.Add(cellTile);
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
            TileData.instance.UpdateData(sprite, marks);
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
        if (Input.GetKeyDown(KeyCode.G))
        {
            StartCoroutine(GenerateMap());
        }
    }

    //TODO ovdje dodaj svaki put da se refreshuje lista opcija za svaki cell
    private IEnumerator GenerateMap()
    {
        int placed = 0;
        //Vector2Int coords = new Vector2Int(0, columns - 1);
        Vector2Int coords = new Vector2Int(Random.Range(0, rows), Random.Range(0, columns));

        for (placed = 0; placed < columns * rows; placed++)
        {
            yield return new WaitForSeconds(0.01f);
            if (!PlaceTile(coords))
            {
                Debug.Log("Error placing a tile");
                break;
            }
            coords = FindNext();
            if (coords.x < 0 || coords.y < 0)
            {
                Debug.LogError("Greska, koordinate ne mogu biti negativne");
                break;
            }
        }
        print("DONE!");
    }
    private Vector2Int FindNext()
    {
        int leastEntrophy = int.MaxValue;
        Vector2Int coordinates = new Vector2Int(-1, -1);
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                CellTile tile = activeCells[j + i * columns];
                if (!tile.isPlaced && tile.GetOptionsCount() < leastEntrophy)
                {
                    leastEntrophy = tile.GetOptionsCount();
                    coordinates.y = j;
                    coordinates.x = i;
                }
            }
        }
        return coordinates;

    }
    private bool PlaceTile(Vector2Int pos)
    {
        string[] tileMarks = activeCells[pos.y + pos.x * columns].Place();
        if (tileMarks == null)
        {
            return false;
        }
        print(("postavio na : ", pos, " tj indeks: ", pos.y + pos.x * columns));
        print((tileMarks[0] + " " + tileMarks[1] + " " + tileMarks[2] + " " + tileMarks[3]));
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
        };
        for (int i = 0; i < directions.Length; i++)     //check left, up, right, down, as sorted in tileMarks
        {
            Vector2Int dir = directions[i];
            int y = pos.y + dir.y;
            int x = pos.x + dir.x;
            if (x < 0 || y < 0 || x >= rows || y >= columns)
                continue;
            print(("Cekiram neighbors na koordinatama: ", y + x * columns, tileMarks[i]));
            activeCells[y + x * columns].CheckCompatibility(tileMarks[i], i); //ovdje ce biti greska sa indeksom
        }

        return true;
    }
}
