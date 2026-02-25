using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using System.Globalization;


public class VisualizationScript : MonoBehaviour
{
    [SerializeField] private ProceduralGenerationManager manager;
    [SerializeField] private SpriteRenderer showSprite;
    private CellTile lastSelectedTile;
    private int spriteIndex;

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
        }
    }

    private void OnDrawGizmos()
    {
        //ispis broja preostalih celija
        foreach (var cell in manager.activeCells)
        {
            Vector3 pos = cell.selfObject.transform.position;

            Gizmos.color = cell.isPlaced ? Color.green : Color.white;
            Gizmos.DrawWireCube(pos, Vector3.one * 0.9f);

            // 3. ISPIS TEKSTA (ENTROPIJA)
            // Koristimo #if jer UnityEditor ne radi kad se igra napravi (Build)
#if UNITY_EDITOR
            //string info = cell.isPlaced ? "OK" : cell.spriteSelection.ToString() + "\n" + cell.GetOptionsCount().ToString();
            string info = cell.isPlaced ? "OK" : cell.GetOptionsCount().ToString();

            // Postavi boju labela (crveno ako je 0 - to je tvoj problem!)
            GUIStyle style = new GUIStyle();
            style.normal.textColor = (cell.GetOptionsCount() == 0 && !cell.isPlaced) ? Color.red : Color.white;
            style.alignment = TextAnchor.LowerCenter;

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
        if (cell.GetOptionsCount()==0)
        {
            return;
        }
        spriteIndex = spriteIndex % cell.GetOptionsCount();
        showSprite.sprite = cell.GetOptions()[spriteIndex];
    }
}
