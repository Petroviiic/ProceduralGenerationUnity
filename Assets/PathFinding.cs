using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

using Debug = UnityEngine.Debug;

public class PathFinding : MonoBehaviour
{
    //za pocetak cu izabrati samo 2 tacke i onda kad se izadje iz mapmode da nadje put izmedju njih ako je moguc, ali mogu dodati
    //ili da se moze izabrati vise tacaka, i onda da ide od jedne do druge pa do trece itd, kao usputne stanice, znaci da imam
    //listu odredista, ili da izaberem jednu tacku, pa da prati mis i u real timeu racuna put do njega. takodje mogu napraviti i 
    //kombinaciju ovih ideja, da prati mis od A do kad se pritisne na B, pa da prati do C itd


    [SerializeField] private Color32 pathColor;
    [SerializeField] private int colorTolerance;
    [SerializeField] private float walkabilityThreshold;

    private Dictionary<Sprite, GraphNode[]> spriteMarks = new Dictionary<Sprite, GraphNode[]>();
    private GraphNode[] walkables;

    private bool isInMapMode = false;
    private GraphNode startNode;
    private GraphNode endNode;
   // private List<GraphNode> targetNodes;

    // Generation manager data
    [SerializeField] private ProceduralGenerationManager manager;
    private int columns;
    private int rows;
    private int colorDiversity;
    private CellTile firstActiveCell;
    private bool diagonalsAllowed;


    private Vector2Int[] AStarDirections = new Vector2Int[]
{
        new Vector2Int(1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1),

        new Vector2Int(1, 1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1),
    };

    public void InitPathFinding(int columns, int rows, int colorDiversity, CellTile firstActiveCell, bool diagonalsAllowed)
    {
        this.columns = columns;
        this.rows = rows;
        this.colorDiversity = colorDiversity;
        this.firstActiveCell = firstActiveCell;
        this.diagonalsAllowed = diagonalsAllowed;

        if (firstActiveCell == null)
            return;

        
        walkables = new GraphNode[columns * rows * colorDiversity * colorDiversity];
        walkables = GeneratePathGraph();
    }
    private void Update()
    {
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

            Vector3 localPosToFirstCell = firstActiveCell.selfObject.transform.InverseTransformPoint(worldMousePos);
            Vector2 sSize = manager.sprites[0].bounds.size;


            int gridX = Mathf.FloorToInt((localPosToFirstCell.x / sSize.x + 0.5f) * colorDiversity);
            int gridY = -(Mathf.FloorToInt((localPosToFirstCell.y / sSize.y - 0.5f) * colorDiversity) + 1);


            if (SelectPoint(gridX, gridY))
            {
                Debug.Log("Selecting tile at: " + new Vector2(gridX, gridY));
            }           
            else
            {
                Debug.Log("Out of bounds or cell not walkable. Error selecting a tile at grid position: " + new Vector2(gridX, gridY));
            }
        }
    }

    private bool SelectPoint(int gridX, int gridY)
    {
        int index = gridX + gridY * columns * colorDiversity;
        if (index < 0 || index >= walkables.Length || !walkables[index].walkable)
        {
            return false;
        }

        if (startNode == null)
            startNode = walkables[index];
        else
            endNode = walkables[index];

        return true;
    }

    //ovo mozda mozes i u scriptable object da preprocessujes ali aj za pocetak nek bude on runtime;
    //ali svakako mozda je pametnija ideja da uradis tako nesto, da se ne mora isti sprite x puta procesovati ovdje u loopovima
    //ako nista mogu koristiti dictionary koji ce sacuvati podatke odredjenog spritea ovdje
    private GraphNode[] GeneratePathGraph()
    {
        CellTile cellTile;

        Vector2Int cellSize = new Vector2Int(manager.sprites[0].texture.width, manager.sprites[0].texture.height);

        Vector2 cellSizeWorld = new Vector2(-1, 1);
        Vector2 offset = -cellSizeWorld / colorDiversity;
        Vector2 firstNodePos = (Vector2)firstActiveCell.selfObject.transform.position + cellSizeWorld / 2 + offset / 2;

        Vector2Int blockSize = new Vector2Int(cellSize.x, cellSize.y) / colorDiversity; 

        GraphNode[] walkables = new GraphNode[columns * rows * colorDiversity * colorDiversity];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                cellTile = manager.activeCells[x + y * columns];

                Sprite cellSprite = cellTile.spriteRenderer.sprite;

                GraphNode[] marks;
                if (!spriteMarks.TryGetValue(cellSprite, out marks))
                {
                    marks = GenerateSpriteMarks(cellSprite, cellSize, blockSize);
                    spriteMarks[cellSprite] = marks;
                }

                foreach(GraphNode n in marks)
                {
                    Vector2Int walkableCoords = n.gridCoords + new Vector2Int(x * colorDiversity, y * colorDiversity);

                    GraphNode node = new GraphNode(n.walkable, firstNodePos + walkableCoords * offset, walkableCoords.x, walkableCoords.y);
                    walkables[walkableCoords.x + walkableCoords.y * columns * colorDiversity] = node;
                }
            }

        }

        // T - path is walkable, F - path is an obstacle
        for (int i = 0; i < rows * colorDiversity; i++)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int j = 0; j < columns * colorDiversity; j++)
            {
                GraphNode curr = walkables[j + i * columns * colorDiversity];
                sb.Append(curr.walkable ? "T " : "F ");
            }
            Debug.Log(sb.ToString());
        }


        return walkables;
    }

    private GraphNode[] GenerateSpriteMarks(Sprite sprite, Vector2Int cellSize, Vector2Int blockSize)
    {
        GraphNode[] marks = new GraphNode[colorDiversity*colorDiversity];
        Texture2D cellTex = sprite.texture;
        Color32[] pixels = cellTex.GetPixels32();
        Color32 pixColor;

        for (int m = 0; m < colorDiversity; m++)
        {
            for (int n = 0; n < colorDiversity; n++)
            {
                int similarColorsCount = 0;

                // making sure to traverse from top-left corner, to avoid indexing complications later :)
                for (int pixY = (colorDiversity - n) * blockSize.y - 1; pixY >= (colorDiversity - n - 1) * blockSize.y; pixY--)
                {
                    for (int pixX = m * blockSize.x; pixX < (m + 1) * blockSize.x; pixX++)
                    {
                        pixColor = pixels[pixX + pixY * cellSize.x];

                        if (AreColorsSimilar(pathColor, pixColor, colorTolerance))
                            similarColorsCount++;

                    }
                }

                GraphNode node = new GraphNode(false, Vector2.zero, m, n);
                
                if (similarColorsCount > (walkabilityThreshold / 100f) * (blockSize.x * blockSize.y))
                {
                    node.walkable = true;
                }

                marks[m + n * colorDiversity] = node;
            }
        }
        return marks;
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



    //---------------- AStar Pathfinding -----------------
    private void AStar(GraphNode start, GraphNode end)
    {
        if (start == null || end == null)
        {
            Debug.Log("Start or End nodes not set. Can't start pathfinding!");
            return;
        }

        PriorityQueue<GraphNode> open = new PriorityQueue<GraphNode>(walkables.Length);
        HashSet<GraphNode> closed = new HashSet<GraphNode>();

        foreach (GraphNode n in walkables)
        {
            n.HeapIndex = -1;
            n.gCost = int.MaxValue;
            n.parent = null;
        }

        start.SetCosts(0, FindDistance(start, end));
        open.Add(start);

        while (open.Count > 0)
        {
            GraphNode current = open.Pop();

            closed.Add(current);
            if (current == null)
            {
                Debug.Log("Error!");
                return;
            }
            if (current == end)
            {
                Debug.Log("Path found");
                ReconstructPath(start, current);
                return;
            }

            foreach (var dir in AStarDirections)
            {
                GraphNode neighbor = GetGraphNeighbor(current, dir);
                if (neighbor == null || !neighbor.walkable || closed.Contains(neighbor))
                    continue;

                bool isDiagonal = (dir.x != 0 && dir.y != 0);
                if (isDiagonal && !diagonalsAllowed)
                    continue;
                int moveCostToNeighbor = isDiagonal ? 14 : 10;
                int gCost = current.gCost + moveCostToNeighbor;

                if (gCost < neighbor.gCost || !open.Contains(neighbor))
                {
                    neighbor.parent = current;

                    neighbor.SetCosts(gCost, FindDistance(neighbor, end));

                    if (!open.Contains(neighbor))
                    {
                        open.Add(neighbor);
                    }
                    else
                    {
                       open.UpdateItem(neighbor);
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

        if (coords.x < 0 || coords.y < 0 || coords.y >= rows * colorDiversity || coords.x >= columns * colorDiversity)
            return null;
        int index = coords.x + coords.y * columns * colorDiversity;

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




public class GraphNode : IHeapItem<GraphNode>
{
    public Vector2 nodePosition;
    public bool walkable;

    // A* 
    public int gCost; // Distance from start
    public int hCost; // Distance from end
    public int fCost; // Sum of these two

    public GraphNode parent;
    public Vector2Int gridCoords;
    private int heapIndex;
    public GraphNode(bool _walkable, Vector2 _pos, int x, int y)
    {
        gCost = int.MaxValue;
        hCost = int.MaxValue;
        fCost = int.MaxValue;

        walkable = _walkable;
        nodePosition = _pos;
        gridCoords = new Vector2Int(x, y);
    }

    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }
    public int CompareTo(GraphNode other)
    {
        if (fCost < other.fCost) return 1;
        if (fCost > other.fCost) return -1;

        if (hCost < other.hCost) return 1;
        if (hCost > other.hCost) return -1;

        return 0;
    }

    public void SetCosts(int gCost, int hCost)
    {
        this.hCost = hCost;
        this.gCost = gCost;
        this.fCost = hCost + gCost;
    }
}
