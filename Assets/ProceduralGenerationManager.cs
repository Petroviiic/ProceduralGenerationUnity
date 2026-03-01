using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ProceduralGenerationManager : MonoBehaviour
{
    [SerializeField] private Transform gridParent;
    [SerializeField] private GameObject cellPrefab;

    [SerializeField] private int rows = 3;
    [SerializeField] private int columns = 4;
    [SerializeField] private Vector2 gridOffset;
    private Vector2 cellSize = new Vector2(1, 1);

    private List<CellTile> cellPool = new List<CellTile>();
    [HideInInspector] public List<CellTile> activeCells = new List<CellTile>();
    [HideInInspector] public Dictionary<GameObject, CellTile> gameObjectToCell = new Dictionary<GameObject, CellTile>();
    private List<CellTile> placedCells = new List<CellTile>();

    private List<Sprite> sprites = new List<Sprite>();
    
    public TileData tileDataPallete;
    public Dictionary<Sprite, int[]> marksData = new Dictionary<Sprite, int[]>();


    //[SerializeField] private int colorDiversity = 3;
    //private Dictionary<Color, int> colorMapping = new Dictionary<Color, int>();
    //private int colorMappingIndex = 1;

    [SerializeField] private float visualizationSpeed = 0.1f;

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


        //if (!isReset) //mzd ovako da ne moram posebno praviti za loadanje novog tiledatapalleta, a svakako je vec sve preprocesovano
        if (!LoadData()) { 
            Debug.LogError("Oops, something went wrong. TileData info is missing!");
            return;
        }
       // print(sprites.Count);
        //VisualizePixelsChecked(colorDiversity, new Vector2Int(sprites[0].texture.width, sprites[0].texture.height));

        foreach (CellTile tile in activeCells)
        {
            tile.isPlaced = false;
            tile.selfObject.SetActive(false);
        }
        activeCells.Clear();
        placedCells.Clear();


       // print((tileDataPallete.Data.Count, tileDataPallete.Sprites.Count));


        cellSize = new Vector2(100 / (float)sprites[0].texture.width, 100 / (float)sprites[0].texture.height);
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
                                        
                    cellTile = new CellTile(this, cell, sprites.GetRange(1, sprites.Count-1));
                    cellPool.Add(cellTile);
                    
                    gameObjectToCell.Add(cell, cellTile);
                    
                    cell.transform.localScale = cellSize;
                }

                cellTile = cellPool[k];
                cellTile.selfObject.SetActive(true);
                cellTile.selfObject.transform.localPosition = gridOffset + new Vector2(i, -j);

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
        gridPosition += new Vector3(-1 * (columns - 1) / 2, 1 * (rows - 1) / 2);
        gridParent.localPosition = gridPosition;

        float aspectRatio = (float)Screen.width / (float)Screen.height;

        float sizeByHeight = rows * 1 / 2f;
        float sizeByWidth = (columns * 1 / 2f) / aspectRatio;
        float padding = 1.1f;
        float finalSize = Mathf.Max(sizeByHeight, sizeByWidth) * padding;
        Camera.main.orthographicSize = Mathf.Max(5f, finalSize);
    }
    private bool LoadData()
    {
        bool isOk = true;
        if (tileDataPallete.Sprites.Count == 0)
        {
            Debug.Log("TileDataPallete.Sprites is empty");
            isOk = false;
        }
        if (tileDataPallete.SpriteMarks.Count == 0)
        {
            Debug.Log("TileDataPallete.SpriteMarks is empty");
            isOk = false;
        }
        if (!isOk) return false;

        sprites.Clear();
        sprites = new List<Sprite>(tileDataPallete.Sprites);

        for (int i = 0; i < sprites.Count; i++)
        {
            marksData[sprites[i]] = tileDataPallete.SpriteMarks[i].marks;
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
        Debug.LogWarning("no available data for sprite " + sprite.name);
        return null;
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))        //inits grid only
        {
            isRunning = false;
            Setup(isReset);     
        }

        if (Input.GetKeyDown(KeyCode.G))            //starts single generation process
        {
            isRunning = false;
            Setup(true);
            StartCoroutine(GenerateMapWithBacktracking());
        }

        if (Input.GetKeyDown(KeyCode.T))            //starts N generation processes for testing purposes
        {
            isRunning = false;
            Setup(true);
            StartCoroutine(TestGeneration(20));
        }
        if (Input.GetKeyDown(KeyCode.F))            //used for step by step generation
        {
            if (!isRunning)
            {
                StartCoroutine(GenerateMapWithBacktracking(stepByStep : true));
                return;
            }
            goNext = true;
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
            StartCoroutine(GenerateMapWithBacktracking(isTestEnv: true));
        }
    }


    struct MapState
    {
        public Vector2Int currentTilePos;
        public int currentSpriteIndex;
        public Dictionary<int, List<Sprite>> optionsSnapshot;       //cellindex, options
        public List<CellTile> placedTilesSnapshot;
    }
    private IEnumerator GenerateMapWithBacktracking(bool isTestEnv = false, bool stepByStep = false)
    {
        isRunning = true;
        goNext = false;

        int placed = 0;
        Vector2Int coords = new Vector2Int(UnityEngine.Random.Range(0, rows), UnityEngine.Random.Range(0, columns));    //starting coords

        Stack<MapState> history = new Stack<MapState>();
        while (placed < columns * rows)
        {
            print(coords);
            bool shouldContinue = false;
            if (coords.x < 0 || coords.y < 0)
            {
                if (history.Count == 0)
                {
                    Debug.LogError("History stack is empty");
                    break;
                }
                placed--;
                MapState lastState = history.Pop();
                RestoreState(lastState);
                Debug.LogError("Greska, koordinate ne mogu biti negativne");

                coords = lastState.currentTilePos;
                shouldContinue = true;
            }
            else
            {
                Dictionary<int, List<Sprite>> options = PlaceTile(coords);
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
                }
                else
                {
                    //no available options
                    if (history.Count == 0)
                    {
                        Debug.LogError("History stack is empty");
                        break;
                    }
                    print("backtracking...");
                    placed--;
                    MapState lastState = history.Pop();
                    RestoreState(lastState);
                    
                    coords = lastState.currentTilePos;
                    shouldContinue = true;
                }
            }


            print(history.Count);
            if (!shouldContinue)
                coords = FindNext();
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
        if (leastEntrophy == 0) //backtrack immediatelly
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
            print("tilemarks empty");
            return null;
        }

        print(("Postavio na : ", pos, " tj indeks: ", pos.y + pos.x * columns));

        for (int i = 0; i < directions.Length; i++)     //check left, up, right, down, as sorted in tileMarks
        {
            Vector2Int dir = directions[i];
            int y = pos.y + dir.y;
            int x = pos.x + dir.x;
            if (x < 0 || y < 0 || x >= rows || y >= columns)
                continue;

            int index = y + x * columns;
            optionsSnapshot[index] = new List<Sprite>(activeCells[index].GetOptions()); 
            activeCells[index].CheckCompatibility(tileMarks[i], i); //ovdje ce biti greska sa indeksom


            //neka return null ako neki od komsija ima optionscount == 0
        }
        placedCells.Add(toPlace);
        return optionsSnapshot;
    }






    void VisualizePixelsChecked(int colorDiversity, Vector2Int spriteSize)
    {
        Sprite toCopy = sprites[0];
        Sprite dummy = Sprite.Create(new Texture2D(spriteSize.x, spriteSize.y, toCopy.texture.format, toCopy.texture.mipmapCount, true), new Rect(0, 0, spriteSize.x, spriteSize.y), new Vector2(0.5f, 0.5f));
        Graphics.CopyTexture(toCopy.texture, dummy.texture);
        dummy.texture.Apply();

        Vector2 offset = spriteSize / (colorDiversity * 2);
        int x = 0, y = 0;
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
                Color color = dummy.texture.GetPixel(x, y);

                dummy.texture.SetPixel(x, y, Color.red);
                for (int k = 0; k < 3; k++)
                {
                    //dummy.texture.SetPixel(x - k, y - k, Color.red);
                    //dummy.texture.SetPixel(x - k, y + k, Color.red);
                    //dummy.texture.SetPixel(x + k, y - k, Color.red);
                    //dummy.texture.SetPixel(x + k, y + k, Color.red);
                }
            }
        }
        dummy.texture.Apply();
        sprites[0] = dummy;
    }
}