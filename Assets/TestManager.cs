using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


enum GenerationType
{
    InitGridOnly,
    StepByStepGeneration,
    SingleGeneration,
    MultipleGenerations
}
public class TestManager : MonoBehaviour
{
    [Header("Test Environment Settings")]
    [Tooltip("How many map generations should be performed?")]
    [SerializeField, Range(1, 1000)] private int testCasesCount = 20;
 
    private Coroutine multipleGenerationCoroutine;
    private Coroutine generationCoroutine;
    private bool stepByStepMode;
    
    [Header("External References")]
    [SerializeField] private ProceduralGenerationManager generationManager;
    [SerializeField] private PathFinding pathFindingManager;
    [SerializeField] private Image samplingDisplayUI;
    [SerializeField] private GameObject HUD;
    private void Start()
    {
        samplingDisplayUI.gameObject.SetActive(false);
        HUD.gameObject.SetActive(false);
    }
    private void Update()
    {
        //Grid Generation
        if (Input.GetKeyDown(KeyCode.Space))        //inits grid only
        {
            StartGeneration(GenerationType.InitGridOnly);
        }
        if (Input.GetKeyDown(KeyCode.G))            //starts single generation process
        {
            StartGeneration(GenerationType.SingleGeneration);
        }
        if (Input.GetKeyDown(KeyCode.T))            //starts N generation processes for testing purposes
        {
            StartGeneration(GenerationType.MultipleGenerations);
        }
        if (Input.GetKeyDown(KeyCode.F))            //used for step by step generation
        {
            StartGeneration(GenerationType.StepByStepGeneration);
        }
        if (Input.GetKeyDown(KeyCode.V))            //shows which pixels are used for sampling
        {
            VisualizePixelsChecked();
        }



        //Pathfinding
        if (Input.GetKeyDown(KeyCode.P))            //generates path grid
        {
            Pathfinding();
        }
        if (Input.GetKeyDown(KeyCode.M))            //enters map mode; select points to generate path between them
        {
            PathFindingMapMode();
        }
        if (Input.GetKeyDown(KeyCode.Backspace))    //deletes the last point selection
        {
            DeselectLastPathPoint();
        }

        
        if (Input.GetKeyDown(KeyCode.L))           
        {
            ToggleDetailedLogs();
        }
        if (Input.GetKeyDown(KeyCode.H))            //hud consists of UI buttons for map generation and pathfinding
        {
            ToggleHUD();
        }

        if (Input.GetKeyDown(KeyCode.R))           //safety option, resets the scene
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
    //Map generation
    public void InitGrid()
    {
        StartGeneration(GenerationType.InitGridOnly);
    }
    public void SingleGeneration()
    {
        StartGeneration(GenerationType.SingleGeneration);
    }
    public void MultipleGeneration()
    {
        StartGeneration(GenerationType.MultipleGenerations);
    }
    public void StepByStepGeneration()
    {
        StartGeneration(GenerationType.StepByStepGeneration);
    }


    //Pathfinding
    public void Pathfinding()
    {
        if (generationManager.InitPathFinding())
        {
            //pathFindingCommands.SetActive(true);
            //pathFindingCommands.SetActive(true);
        }

    }
    public void PathFindingMapMode()
    {
        pathFindingManager.MapMode();
    }
    public void DeselectLastPathPoint()
    {
        pathFindingManager.DeselectPoint();
    }

    
    
    public void ToggleHUD()
    {
        HUD.SetActive(!HUD.activeSelf);
    }

    public void ToggleDetailedLogs()
    {
        Debugger.EnableLogs = !Debugger.EnableLogs;
        Debug.Log("Logs " + (Debugger.EnableLogs ? "enabled!" : "disabled!"));
    }


    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void StartGeneration(GenerationType type)
    {
        pathFindingManager.ResetPathData();


        if (generationManager.isRunning)
        {
            if (stepByStepMode && type == GenerationType.StepByStepGeneration)
            {
                generationManager.goNext = true;
                return;
            }

            if (multipleGenerationCoroutine != null)
                StopCoroutine(multipleGenerationCoroutine);
            
            if (generationCoroutine != null)
                StopCoroutine(generationCoroutine);
        }

        stepByStepMode = false;
        generationManager.isRunning = false;
        generationManager.Setup();

        switch (type)
        {
            case GenerationType.InitGridOnly:
                break;

            case GenerationType.StepByStepGeneration:
                stepByStepMode = true;
                generationCoroutine = StartCoroutine(generationManager.GenerateMapWithBacktracking(stepByStep: true));             
                break;

            case GenerationType.SingleGeneration:
                generationCoroutine = StartCoroutine(generationManager.GenerateMapWithBacktracking());
                break;

            case GenerationType.MultipleGenerations:
                multipleGenerationCoroutine = StartCoroutine(MultipleGeneration(testCasesCount));
                break;
        }
    }




    private IEnumerator MultipleGeneration(int num)
    {
        for (int i = 0; i < num; i++)
        {
            while (generationManager.isRunning)
            {
                yield return null;
            }
            generationManager.Setup();
            generationCoroutine = StartCoroutine(generationManager.GenerateMapWithBacktracking(isTestEnv: true));
        }
    }




    public void VisualizePixelsChecked()
    {
        if (samplingDisplayUI.gameObject.activeSelf)
        {
            samplingDisplayUI.gameObject.SetActive(false);
            return;
        }

        if (generationManager.tileDataPalette == null)
        {
            Debug.LogWarning("Tile Palette ScriptableObject is missing in the Inspector. Please assign it.");
            return;
        }
        if (generationManager.tileDataPalette.sprites.Count == 0)
        {
            Debug.LogWarning("Sprite list is empty. Scriptable object data is missing. Try generating it!");
            return;
        }
        int markSize = 3;

        int colorDiversity = generationManager.tileDataPalette.colorDiversity;

        Sprite toCopy = generationManager.tileDataPalette.sprites[generationManager.tileDataPalette.sprites.Count - 1];
        Vector2Int spriteSize = new Vector2Int(toCopy.texture.width, toCopy.texture.height);

        Sprite dummy = Sprite.Create(new Texture2D(spriteSize.x, spriteSize.y, toCopy.texture.format, toCopy.texture.mipmapCount, true), new Rect(0, 0, spriteSize.x, spriteSize.y), new Vector2(0.5f, 0.5f));
        Graphics.CopyTexture(toCopy.texture, dummy.texture);

        Vector2 offset = spriteSize / (colorDiversity * 2);
        int x = 0, y = 0;
        for (int side = 0; side < 4; side++)    // origin corner: bottom-left;  directions : up, right, down, left
        {
            for (int i = 0; i < colorDiversity; i++)
            {
                if (side == 0) 
                {
                    y = (int)(offset.y + i * spriteSize.y / colorDiversity);
                    x = (int)(offset.y);
                }
                else if (side == 1)                 
                {
                    x = (int)(offset.x + i * spriteSize.x / colorDiversity);
                    y = (int)(offset.y + (colorDiversity - 1) * spriteSize.y / colorDiversity);
                }
                else if (side == 2) 
                {
                    y = (int)(offset.y + i * spriteSize.y / colorDiversity);
                    x = (int)(offset.x + (colorDiversity - 1) * spriteSize.x / colorDiversity);
                }
                else if (side == 3) 
                {
                    x = (int)(offset.x + i * spriteSize.x / colorDiversity);
                    y = (int)(offset.y);
                }

                dummy.texture.SetPixel(x, y, Color.red);
                
                for (int m = -markSize; m <= markSize; m++)
                {
                    for (int n = -markSize; n <= markSize; n++)
                    {
                        dummy.texture.SetPixel(x + m, y + n, Color.red);
                    }
                }
            }
        }

        dummy.texture.Apply();
        
        samplingDisplayUI.gameObject.SetActive(true);
        samplingDisplayUI.sprite = dummy;
    }

}

public class Debugger
{
    public static bool EnableLogs = true;
    public static void ShowLog(string msg)
    {
        if (!EnableLogs)
            return;
        Debug.Log(msg);
    }
    public static void ShowLogWarning(string msg)
    {
        if (!EnableLogs)
            return;
        Debug.LogWarning(msg);
    }
    public static void ShowLogError(string msg)
    {
        if (!EnableLogs)
            return;
        Debug.LogError(msg);
    }
}
