using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


    //------------------------PATHFINDING ---------------------------


    //za pocetak cu izabrati samo 2 tacke i onda kad se izadje iz mapmode da nadje put izmedju njih ako je moguc, ali mogu dodati
    //ili da se moze izabrati vise tacaka, i onda da ide od jedne do druge pa do trece itd, kao usputne stanice, znaci da imam
    //listu odredista, ili da izaberem jednu tacku, pa da prati mis i u real timeu racuna put do njega. takodje mogu napraviti i 
    //kombinaciju ovih ideja, da prati mis od A do kad se pritisne na B, pa da prati do C itd

    [SerializeField] private Color32 pathColor;
    [SerializeField] private int colorTolerance;
    [SerializeField] private float walkabilityThreshold;

    private List<GameObject> marks = new List<GameObject>();
    [SerializeField] private Sprite blankSprite;
    private GraphNode[] walkables;

    private bool isInMapMode = false;
    private GraphNode startNode;
    private GraphNode endNode;
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            walkables = new GraphNode[columns * rows * tileDataPalette.colorDiversity * tileDataPalette.colorDiversity];
            walkables = GeneratePathGraph();
        }

        if (Input.GetKeyDown(KeyCode.M) && walkables != null && walkables.Length != 0)
        {

            isInMapMode = !isInMapMode;
            if (isInMapMode)
            {
                Debug.Log("Map mode ON");
                startNode = null;
                endNode = null;     //reseting start and end
            }
            else
            {
                Debug.Log("Map mode OFF");
                AStar(startNode, endNode);
            }
        }


        //rewrite ovo da ne racunas npr bounds svaki put 
        if (isInMapMode && Input.GetMouseButtonDown(0))
        {
            Vector3 worldMousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            worldMousePos.z = 0;

            Transform firstCell = activeCells[0].selfObject.transform;
            Vector3 localPosToFirstCell = firstCell.InverseTransformPoint(worldMousePos);
            Vector2 sSize = sprites[0].bounds.size;


            int gridX = Mathf.FloorToInt((localPosToFirstCell.x / sSize.x + 0.5f) * tileDataPalette.colorDiversity);
            int gridY = -(Mathf.FloorToInt((localPosToFirstCell.y / sSize.y - 0.5f) * tileDataPalette.colorDiversity) + 1);

            int index = gridX + gridY * columns * tileDataPalette.colorDiversity;

            if (index >= 0 && index < walkables.Length && walkables[index].walkable)
            {
                Debug.Log("Selecting tile at: " + index + ",Grid position: " + new Vector2(gridX, gridY));
                SelectPoint(index);
            }
            else
            {
                Debug.Log("Out of bounds or cell not walkable. Error selecting a tile at  " + index + ",Grid position: " + new Vector2(gridX, gridY));
            }
        }
    }
    private void SelectPoint(int index)
    {
        if (startNode == null)
            startNode = walkables[index];
        else
            endNode = walkables[index];
    }
    public class GraphNode
    {
        public Vector2 nodePosition;
        public bool walkable;

        // A* 
        public int gCost; // Udaljenost od starta
        public int hCost; // Udaljenost do cilja (heuristika)
        public int fCost; // Ukupni trošak

        public GraphNode parent;
        public Vector2Int gridCoords;

        public GraphNode(bool _walkable, Vector2 _pos, int x, int y)
        {
            gCost = int.MaxValue;
            hCost = int.MaxValue;
            fCost = int.MaxValue;

            walkable = _walkable;
            nodePosition = _pos;
            gridCoords = new Vector2Int(x, y);
        }
    }

    //ovo mozda mozes i u scriptable object da preprocessujes ali aj za pocetak nek bude on runtime;
    //ali svakako mozda je pametnija ideja da uradis tako nesto, da se ne mora isti sprite x puta procesovati ovdje u loopovima
    //ako nista mogu koristiti dictionary koji ce sacuvati podatke odredjenog spritea ovdje
    private GraphNode[] GeneratePathGraph()
    {
        Color32[] pixels;
        Texture2D cellTex;
        CellTile cellTile;
        int div = tileDataPalette.colorDiversity;

        Vector2Int cellSize = new Vector2Int(sprites[0].texture.width, sprites[0].texture.height);

        Vector2 cellSizeWorld = new Vector2(-1, 1);
        Vector2 offset = -cellSizeWorld / div;
        Vector2 firstNodePos = (Vector2)activeCells[0].selfObject.transform.position + cellSizeWorld / 2 + offset / 2;

        GraphNode[] walkables = new GraphNode[columns * rows * div * div];
        
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

                        // making sure to traverse from top-left corner, to avoid indexing complications later :)
                        for (int pixY = (div - n) * blockSize.y - 1; pixY >= (div - n - 1) * blockSize.y; pixY--)
                        {
                            for (int pixX = m * blockSize.x; pixX < (m + 1) * blockSize.x; pixX++)
                            {
                                pixColor = pixels[pixX + pixY * cellSize.x];

                                if (AreColorsSimilar(pathColor, pixColor, colorTolerance))
                                    similarColorsCount++;

                            }
                        }
                        
                        Vector2Int walkableCoords = new Vector2Int(x * div + m, y * div + n);

                        GraphNode node = new GraphNode(false, firstNodePos + walkableCoords * offset, walkableCoords.x, walkableCoords.y);
                      

                        if (similarColorsCount > (walkabilityThreshold / 100f) * (blockSize.x * blockSize.y))
                        {
                            node.walkable = true;
                        }

                        walkables[walkableCoords.x + walkableCoords.y * columns * div] = node;
                    }
                }
            }

        }

        //foreach(GameObject item in marks)
        //{
        //    item.SetActive(false);
        //}
        //marks.Clear();  
        // T - path is walkable, F - path is an obstacle
        for (int i = 0; i < rows * div; i++)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int j = 0; j < columns * div; j++)
            {
                GraphNode curr = walkables[j + i * columns * div];
                sb.Append(curr.walkable ? "T " : "F ");

                //GameObject test = new GameObject();
                //test.transform.position = curr.nodePosition;
                //test.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                //test.AddComponent<SpriteRenderer>().sprite = blankSprite;
                //test.GetComponent<SpriteRenderer>().color = curr.walkable ? Color.green : Color.red;
                //test.GetComponent<SpriteRenderer>().sortingOrder = 5;
                //marks.Add(test);    
            }
            Debug.Log(sb.ToString());
        }


        return walkables;
    }
    private void OnDrawGizmos()
    {
        if (walkables == null || walkables.Length == 0)
            return;
        foreach (GraphNode node in walkables)
        {
            Vector3 pos = node.nodePosition;

            Gizmos.color = node.walkable ? Color.green : Color.red;
            Gizmos.DrawWireSphere(pos, .1f);
        }
    }

    private bool AreColorsSimilar(Color32 target, Color32 pixel, float tolerance)
    {
        float diffR = target.r - pixel.r;
        float diffG = target.g - pixel.g;
        float diffB = target.b - pixel.b;

        return (diffR * diffR) + (diffG * diffG) + (diffB * diffB) < (tolerance * tolerance);
    }


    private void AStar(GraphNode start, GraphNode end)
    {
        if (start == null || end == null)
        {
            Debug.Log("Start or End nodes not set. Can't start pathfinding!");
            return;
        }

        // Neka npr u scriptable object ima neki checkmark da li da se provjeravaju dijagonalni putevi ili ne,
        // jer npr kod MyTiles nema poente provjeravati njih, nego samo ove lijevo desno gore dole jer sam tako napravio puteve
        
        // dodaj priority queue kad zavrsis sve, zasad nek budu liste
        // nodeDistance - horizontalna udaljenost dva nodea pomnozena sa 10 i zaokruzena na najblizi integer

        List<GraphNode> open = new List<GraphNode>();
        HashSet<GraphNode> closed = new HashSet<GraphNode>();


        foreach (GraphNode n in walkables)
        {
            n.gCost = int.MaxValue;
            n.parent = null; 
        }

        start.gCost = 0;
        start.hCost = FindDistance(start, end);
        start.fCost = FindDistance(start, end);     //stavi ovo mzd u metodu u klasi da se automatski izracuna
        open.Add(start);

        while (open.Count > 0)
        {
            GraphNode current = open[0];   //node je onaj sa najmanjim fcostom
            foreach (GraphNode node in open)
            {
                if (node.fCost < current.fCost || (node.fCost == current.fCost && node.hCost < current.hCost))
                    current = node;
            }

            open.Remove(current);
            closed.Add(current);
            Debug.Log(("picked ", current.gridCoords));
            
            if (current == end)
            {
                Debug.Log("Path found");
                ReconstructPath(start, end);
                return;
            }

            foreach (var dir in directions)
            {
                GraphNode neighbor = GetGraphNeighbor(current, dir);
                if (neighbor == null || !neighbor.walkable || closed.Contains(neighbor))
                    continue;

                int moveCostToNeighbor = (dir.x != 0 && dir.y != 0) ? 14 : 10;
                int gCost = current.gCost + moveCostToNeighbor;

                bool isInOpen = false;
                if (gCost >= neighbor.gCost)
                {
                    isInOpen = open.Contains(neighbor);
                }

                if (gCost < neighbor.gCost || !isInOpen)
                {
                    neighbor.parent = current;

                    int hCost = FindDistance(neighbor, end);
                    neighbor.hCost = hCost;
                    neighbor.gCost = gCost;
                    neighbor.fCost = hCost + gCost;

                    if (!isInOpen)
                    {
                        Debug.Log(("added ", neighbor.gridCoords));
                        open.Add(neighbor);
                    }
                }
            }
        }

        Debug.Log("Couldnt find path");
        return;
    }

    private GraphNode GetGraphNeighbor(GraphNode node, Vector2Int dir)
    {
        if (node == null)
            return null;

        Vector2Int coords = node.gridCoords + dir;

        if (coords.x < 0 || coords.y < 0 || coords.y >= rows * tileDataPalette.colorDiversity || coords.x >= columns * tileDataPalette.colorDiversity)
            return null;
        int index = coords.x + coords.y * columns * tileDataPalette.colorDiversity;

        return walkables[index];
    }

    private int FindDistance(GraphNode a, GraphNode b)
    {
        int dstX = Mathf.Abs(a.gridCoords.x - b.gridCoords.x);
        int dstY = Mathf.Abs(a.gridCoords.y - b.gridCoords.y);

        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);

        return 14 * dstX + 10 * (dstY - dstX);
    }


    [SerializeField] private LineRenderer lineRenderer;
    private void ReconstructPath(GraphNode start, GraphNode end)
    {
        List<Vector3> path = new List<Vector3>();
        GraphNode curr = end;
        while (curr != start)
        {
            path.Add(curr.nodePosition);
            curr = curr.parent;
        }
        path.Add(start.nodePosition);

        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
    }
}