using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System.Globalization;
using UnityEngine.UI;


public class VisualizationScript : MonoBehaviour
{
    [SerializeField] private ProceduralGenerationManager manager;
    [SerializeField] private Image showImage;
    private CellTile lastSelectedTile;
    private int spriteIndex;

    private Sprite defaultSprite;
    private void Start()
    {
        showImage.sprite = null;
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

            if (hit.collider != null)
            {
                GameObject hitObj = hit.collider.gameObject;

                if (manager.gameObjectToCell.TryGetValue(hitObj, out CellTile clickedCell))
                {
                    #if UNITY_EDITOR
                        UnityEditor.Selection.activeGameObject = hitObj;
                    #endif

                    if (clickedCell != lastSelectedTile)
                    {
                        spriteIndex = 0;
                        lastSelectedTile = clickedCell;
                    }
                    else
                    {
                        spriteIndex++;
                    }
                }
            }
            else
            {
                #if UNITY_EDITOR
                    UnityEditor.Selection.activeGameObject = null;
                #endif
                DrawCellDetails(null);
            }
        }
    }

    private void OnDrawGizmos()
    {
        foreach (var cell in manager.activeCells)
        {
            Vector3 pos = cell.selfObject.transform.position;

            Gizmos.color = cell.isPlaced ? Color.green : Color.white;
            Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);

#if UNITY_EDITOR
            string info = cell.isPlaced ? "OK" : cell.GetOptionsCount().ToString();

            GUIStyle style = new GUIStyle();
            style.normal.textColor = (cell.GetOptionsCount() == 0 && !cell.isPlaced) ? Color.red : Color.white;
            style.alignment = TextAnchor.MiddleCenter;

            Handles.Label(pos, info, style);

            if (Selection.activeGameObject != null && Selection.activeGameObject == cell.selfObject)
            {
                DrawCellDetails(cell);
            }
            
            #endif
        }
    }
    private void DrawCellDetails(CellTile cell)
    {
        if (cell == null) 
        {
            showImage.sprite = defaultSprite;
            return;
        }
        if (cell.GetOptionsCount()==0)
        {
            return;
        }
        spriteIndex = spriteIndex % cell.GetOptionsCount();
        showImage.sprite = cell.GetOptions()[spriteIndex];
    }
}
