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

    private List<GameObject> cellPool = new List<GameObject>();

    private List<Sprite> sprites = new List<Sprite>();

    private void Start()
    {
        Setup();
    }
    public void Setup()
    {
        foreach (Sprite item in Resources.LoadAll<Sprite>("Tiles"))
        {
            sprites.Add(item);
        }

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                GameObject cell = Instantiate(cellPrefab);
                cell.transform.parent = gridParent;
                cell.transform.localPosition = gridOffset + new Vector2(i * cellSize.x, j * cellSize.y);

                CellTile cellTile = new CellTile(cell);
                cellTile.UpdateCell(sprites[0]);

                cellPool.Add(cell);
            }
        }
    }

    private void GenerateMap()
    {
        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                GameObject cell = Instantiate(cellPrefab);
                cell.transform.parent = gridParent;
                cell.transform.localPosition = gridOffset + new Vector2(i * cellSize.x, j * cellSize.y);

                CellTile cellTile = new CellTile(cell);
                cellTile.UpdateCell(sprites[0]);

                cellPool.Add(cell);
            }
        }
    }
}
