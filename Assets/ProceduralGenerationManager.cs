using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ProceduralGenerationManager : MonoBehaviour
{
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject cellPrefab;

    // Using rows and newrows to prevent errors if these are edited mid-generation
    private int rows;
    private int columns;
    [SerializeField, Range(1, 200)] private int newRows = 3;
    [SerializeField, Range(1, 200)] private int newColumns = 4;

    [SerializeField] private Vector2 gridOffset;
    private Vector2 cellSize = new Vector2(1, 1);

    private List<CellTile> cellPool = new List<CellTile>();
    [HideInInspector] public List<CellTile> activeCells = new List<CellTile>();
    [HideInInspector] public Dictionary<GameObject, CellTile> gameObjectToCell = new Dictionary<GameObject, CellTile>();
    private List<CellTile> placedCells = new List<CellTile>();

    [NonSerialized, HideInInspector] public List<Sprite> sprites = new List<Sprite>();
    
    public TileData tileDataPalette;
    public Dictionary<Sprite, int[]> marksData = new Dictionary<Sprite, int[]>();

    [SerializeField] private float visualizationSpeed = 0.1f;

    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
    };
    [HideInInspector] public bool goNext = false;        //used for stepbystep iteration
    [HideInInspector] public bool isRunning = false;     //is generation currently in progress

    public void Setup()
    {
        AdjustFOV();

        if (!LoadData()) { 
            Debug.LogError("Something went wrong. TileData info is missing!");
            return;
        }

        foreach (CellTile tile in activeCells)
        {
            tile.isPlaced = false;
            tile.selfObject.SetActive(false);
        }
        activeCells.Clear();
        placedCells.Clear();

        cellSize = new Vector2(100 / (float)sprites[0].texture.width, 100 / (float)sprites[0].texture.height);

        columns = newColumns;
        rows = newRows;

        int k = 0;
        for (int j = 0; j < newRows; j++)
        {
            for (int i = 0; i < newColumns; i++)
            {
                CellTile cellTile = null;
                if (k >= cellPool.Count)
                {
                    GameObject cell = Instantiate(cellPrefab);
                    cell.transform.parent = gridParent;
                                        
                    cellTile = new CellTile(this, cell, sprites.GetRange(1, sprites.Count-1));
                    cellPool.Add(cellTile);
                    
                    gameObjectToCell.Add(cell, cellTile);
                    
                }

                cellTile = cellPool[k];
                cellTile.selfObject.SetActive(true);
                cellTile.selfObject.transform.localPosition = gridOffset + new Vector2(i, -j);
                cellTile.selfObject.transform.localScale = cellSize;

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
        gridPosition += new Vector3(-1 * (newColumns - 1) / 2, 1 * (newRows - 1) / 2);
        gridParent.localPosition = gridPosition;

        float aspectRatio = (float)Screen.width / (float)Screen.height;

        float sizeByHeight = newRows * 1 / 2f;
        float sizeByWidth = (newColumns * 1 / 2f) / aspectRatio;
        float padding = 1.1f;
        float finalSize = Mathf.Max(sizeByHeight, sizeByWidth) * padding;
        Camera.main.orthographicSize = Mathf.Max(5f, finalSize);
    }
    private bool LoadData()
    {
        bool isOk = true;
        if (tileDataPalette.sprites.Count == 0)
        {
            Debug.Log("TileDataPallete.Sprites is empty");
            isOk = false;
        }
        if (tileDataPalette.spriteMarks.Count == 0)
        {
            Debug.Log("TileDataPallete.SpriteMarks is empty");
            isOk = false;
        }
        if (!isOk) return false;

        sprites.Clear();
        sprites = new List<Sprite>(tileDataPalette.sprites);

        for (int i = 0; i < sprites.Count; i++)
        {
            marksData[sprites[i]] = tileDataPalette.spriteMarks[i].marks;
        }

        return true;
        // foreach (Sprite item in Resources.LoadAll<Sprite>("Images/mountains"))      //-----------!!!!!!!!!!!!
    }
    public int[] GetData(Sprite sprite)
    {
        if (marksData.ContainsKey(sprite))
        {
            return marksData[sprite];
        }
        Debug.LogWarning("No available data for sprite " + sprite.name);
        return null;
    }

    struct MapState
    {
        public Vector2Int currentTilePos;
        public int currentSpriteIndex;
        public Dictionary<int, List<Sprite>> optionsSnapshot;       //cellindex, options
        public List<CellTile> placedTilesSnapshot;
    }
    public IEnumerator GenerateMapWithBacktracking(bool isTestEnv = false, bool stepByStep = false)
    {
        isRunning = true;
        goNext = false;

        int placed = 0;
        Vector2Int coords = new Vector2Int(UnityEngine.Random.Range(0, rows), UnityEngine.Random.Range(0, columns));    //starting coords

        Stack<MapState> history = new Stack<MapState>();
        while (placed < columns * rows)
        {

            Debug.Log(("Trying coordinates: ", coords));
            Dictionary<int, List<Sprite>> options = (coords.x >= 0 && coords.y >= 0) ? PlaceTile(coords) : null;
            
            if (options != null)
            {
                history.Push(new MapState
                {
                    currentSpriteIndex = activeCells[coords.y + coords.x * columns].spriteSelection,
                    optionsSnapshot = options,
                    currentTilePos = coords,
                    placedTilesSnapshot = new List<CellTile>(placedCells.GetRange(0, placedCells.Count - 1))
                });
                placed++;
                coords = FindNext();
            }
            else
            {
                Debug.Log("Backtracking...");
                if (!TryBacktrack(history, out coords)) 
                    break;
                placed--;
            }
            


            Debug.Log(("History stack count: ", history.Count));
            if (!stepByStep)
            {
                if (isTestEnv)
                    yield return null;
                else
                    yield return new WaitForSeconds(visualizationSpeed);
            }
            else
            {
                while (!goNext)
                {
                    yield return null;
                }

                goNext = false;
            }
        }
        isRunning = false;
    }

    private bool TryBacktrack(Stack<MapState> history, out Vector2Int newCoords)
    {
        newCoords = new Vector2Int(-1, -1);
        if (history.Count == 0)
        {
            Debug.LogError("History stack is empty - Generation Failed");
            return false;
        }

        MapState lastState = history.Pop();
        RestoreState(lastState);
        newCoords = lastState.currentTilePos;
        return true;
    }

    private void RestoreState(MapState state)
    {
        foreach (int index in state.optionsSnapshot.Keys)
        {
            activeCells[index].RestoreOptions(state.optionsSnapshot[index]);
        }
        int currentCell = state.currentTilePos.y + state.currentTilePos.x * columns;
        activeCells[currentCell].ResetCell(sprites[0], state.currentSpriteIndex + 1);

        placedCells = new List<CellTile>(state.placedTilesSnapshot);
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
                int posToCheck = coordsToCheck.y + coordsToCheck.x * columns;   // Coords to list index
                
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
        if (leastEntrophy == 0) // Backtrack immediately
            return new Vector2Int(-1, -1);
        return coordinates;
    }
    private Dictionary<int, List<Sprite>> PlaceTile(Vector2Int pos)
    {
        CellTile toPlace = activeCells[pos.y + pos.x * columns];

        Dictionary<int, List<Sprite>> optionsSnapshot = new Dictionary<int, List<Sprite>>();
        optionsSnapshot[pos.y + pos.x * columns] = new List<Sprite>(activeCells[pos.y + pos.x * columns].GetOptions());

        int[] tileMarks = toPlace.Place(new Vector2Int(pos.x, pos.y));

        if (tileMarks == null)
        {
            Debug.Log("Tilemarks empty!");
            return null;
        }

        Debug.Log(("Tile placed at: ", pos));

        for (int i = 0; i < directions.Length; i++)     //check left, up, right, down, as sorted in tileMarks
        {
            Vector2Int dir = directions[i];
            int y = pos.y + dir.y;
            int x = pos.x + dir.x;
            if (x < 0 || y < 0 || x >= rows || y >= columns)
                continue;

            int index = y + x * columns;
            optionsSnapshot[index] = new List<Sprite>(activeCells[index].GetOptions()); 
            activeCells[index].CheckCompatibility(tileMarks[i], i); 
        }
        placedCells.Add(toPlace);
        return optionsSnapshot;
    }




    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            GeneratePathGraph();
        }
    }


    [SerializeField] private Color32 pathColor;
    [SerializeField] private int colorTolerance;
    [SerializeField] private float walkabilityThreshold;

    //ovo mozda mozes i u scriptable object da preprocessujes ali aj za pocetak nek bude on runtime;
    //ali svakako mozda je pametnija ideja da uradis tako nesto, da se ne mora isti sprite x puta procesovati ovdje u loopovima
    private void GeneratePathGraph()
    {
        Color32[] pixels;
        Texture2D cellTex;
        CellTile cellTile;
        int div = tileDataPalette.colorDiversity;

        Vector2Int cellSize = new Vector2Int(sprites[0].texture.width, sprites[0].texture.height);
        
        bool[] walkables = new bool[columns * rows * div * div];
        
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                cellTile = activeCells[x + y * columns];

                cellTex = cellTile.selfObject.GetComponent<SpriteRenderer>().sprite.texture;
                pixels = cellTex.GetPixels32();

                Color32 pixColor;
                Vector2Int blockSize = new Vector2Int(cellSize.x, cellSize.y) / div;

                for (int m = 0; m < div; m++)
                {
                    for (int n = 0; n < div; n++)
                    {
                        int similarColorsCount = 0;

                        for (int pixY = (div - n) * blockSize.y - 1; pixY >= (div - n - 1) * blockSize.y; pixY--)
                        {
                            for (int pixX = m * blockSize.x; pixX < (m + 1) * blockSize.x; pixX++)
                            {
                                pixColor = pixels[pixX + pixY * cellSize.x];

                                if (AreColorsSimilar(pathColor, pixColor, colorTolerance))
                                    similarColorsCount++;

                            }
                        }
                        
                        if (similarColorsCount > (walkabilityThreshold / 100f) * (blockSize.x * blockSize.y))
                        {
                            Vector2Int walkableCoords = new Vector2Int(x * div + m, y * div + n);
                            walkables[walkableCoords.x + walkableCoords.y * columns * div] = true;
                        }
                    }
                }
            }

        }


        // T - path is walkable, F - path is an obstacle
        for (int i = 0; i < rows * div; i++)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int j = 0; j < columns * div; j++)
            {
                sb.Append(walkables[j + i * columns * div] ? "T " : "F ");
            }
            Debug.Log(sb.ToString());
        }
    }

    private bool AreColorsSimilar(Color32 target, Color32 pixel, float tolerance)
    {
        float diffR = target.r - pixel.r;
        float diffG = target.g - pixel.g;
        float diffB = target.b - pixel.b;

        return (diffR * diffR) + (diffG * diffG) + (diffB * diffB) < (tolerance * tolerance);
    }
}