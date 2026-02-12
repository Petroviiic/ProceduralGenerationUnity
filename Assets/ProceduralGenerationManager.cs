using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    private List<CellTile> placedCells = new List<CellTile>();

    private List<Sprite> sprites = new List<Sprite>();

    [SerializeField] private int colorDiversity = 3;
    private Dictionary<Color, char> colorMapping = new Dictionary<Color, char>();
    private char colorMappingIndex = 'A';


    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
    };
    private bool goNext = false;        //used for stepbystep iteration
    private bool isRunning = false;     //is generation currently in progress
    private bool isReset = false;     

    public void Setup(bool isReset)
    {
        AdjustFOV();

        if (!isReset)
            LoadSprites(colorDiversity);

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
                    cellPool.Add(cellTile);
                }

                cellTile = cellPool[k];
                cellTile.selfObject.SetActive(true);
                cellTile.selfObject.transform.localPosition = gridOffset + new Vector2(i * cellSize.x, -j * cellSize.y);

                cellTile.UpdateSprite(sprites[0]);
                cellTile.SetOptions(sprites.GetRange(1, sprites.Count - 1));
                activeCells.Add(cellTile);
                k++;
            }
        }
    }
    private void AdjustFOV()
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
    }
    private void LoadSprites(int colorDiversity)
    {
        sprites.Clear();
        foreach (Sprite item in Resources.LoadAll<Sprite>("Tiles"))
        {
            sprites.Add(item);
        }
        Vector2 spriteSize = sprites[0].rect.size;

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


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))        //inits grid only
        {
            Setup(isReset);     
        }

        if (Input.GetKeyDown(KeyCode.G))            //starts single generation process
        {
            StartCoroutine(GenerateMap());
        }

        if (Input.GetKeyDown(KeyCode.T))            //starts N generation processes for testing purposes
        {
            StartCoroutine(TestGeneration(20));
        }
        if (Input.GetKeyDown(KeyCode.F))            //used for step by step generation
        {
            foreach (CellTile item in placedCells)
            {
                item.selfObject.GetComponent<SpriteRenderer>().color = Color.white;
            }
            goNext = true;
            foreach (CellTile item in placedCells)
            {
                item.selfObject.GetComponent<SpriteRenderer>().color = Color.blue;
            }
        }

        if (Input.GetKeyDown(KeyCode.R))           //safety check, resets the scene
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private IEnumerator TestGeneration(int num)
    {
        for (int i = 0; i < num; i++)
        {
            while(isRunning)
            {
                yield return null;
            }
            Setup(true);
            StartCoroutine(GenerateMap(true));
        }
    }

    private IEnumerator GenerateMap(bool isTestEnv = false)
    {
        isRunning = true;

        int placed = 0;
        Vector2Int coords = new Vector2Int(Random.Range(0, rows), Random.Range(0, columns));

        for (placed = 0; placed < columns * rows; placed++)     //ovdje mogu staviti while  coords.x>=0 && cords.y >= 0, al aj
        {
            if (isTestEnv)
                yield return null;
            else
                yield return new WaitForSeconds(0.01f);
            //while (!goNext)
            //{
            //    yield return null;
            //}
            goNext = false;
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
        isRunning = false;
    }
    private Vector2Int FindNext()
    {   
        int leastEntrophy = int.MaxValue;
        Vector2Int coordinates = new Vector2Int(-1, -1);

        for (int i = 0; i < placedCells.Count;)
        {
            CellTile placed = placedCells[i];
            Vector2Int coords = placed.GetCoordinates();

            if (coords.x < 0 || coords.y < 0)
            {
                Debug.Log("Error! Tile is present in placedCells, but its coordinates are negative");
                break;
            }

            int neigborsCount = 0;
            int placedNeighborsCount = 0;
            foreach (Vector2Int direction in directions)
            {
                Vector2Int coordsToCheck = coords + direction;
                int posToCheck = coordsToCheck.y + coordsToCheck.x * columns;   //coords to list index
                if (coordsToCheck.y < 0 || coordsToCheck.x < 0 || coordsToCheck.y >= columns || coordsToCheck.x >= rows)
                    continue;
                neigborsCount++;
                CellTile neighbor = activeCells[posToCheck];

                int optionsCount = neighbor.GetOptionsCount();
                if (neighbor.isPlaced)
                {
                    placedNeighborsCount++;
                    continue;
                }

                if (optionsCount < leastEntrophy)
                {
                    leastEntrophy = optionsCount;
                    coordinates.y = coordsToCheck.y;
                    coordinates.x = coordsToCheck.x;
                }
            }

            if (placedNeighborsCount == neigborsCount)
            {
                placedCells.Remove(placed);
                continue;
            }
            i++;
        }                
        return coordinates;
    }
    private bool PlaceTile(Vector2Int pos)
    {
        CellTile toPlace = activeCells[pos.y + pos.x * columns];

        string[] tileMarks = toPlace.Place(new Vector2Int(pos.x, pos.y));
        if (tileMarks == null)
        {
            return false;
        }

        print(("Postavio na : ", pos, " tj indeks: ", pos.y + pos.x * columns));

        for (int i = 0; i < directions.Length; i++)     //check left, up, right, down, as sorted in tileMarks
        {
            Vector2Int dir = directions[i];
            int y = pos.y + dir.y;
            int x = pos.x + dir.x;
            if (x < 0 || y < 0 || x >= rows || y >= columns)
                continue;

            activeCells[y + x * columns].CheckCompatibility(tileMarks[i], i); //ovdje ce biti greska sa indeksom
        }
        placedCells.Add(toPlace);
        return true;
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