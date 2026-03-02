using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;


enum GenerationType
{
    InitGridOnly,
    StepByStepGeneration,
    SingleGeneration,
    MultipleGenerations
}
public class TestManager : MonoBehaviour
{
    [SerializeField] private ProceduralGenerationManager generationManager;

    private Coroutine multipleGenerationCoroutine;
    private Coroutine generationCoroutine;
    private bool stepByStepMode;

    private void Update()
    {
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

        if (Input.GetKeyDown(KeyCode.R))           //safety check, resets the scene
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void StartGeneration(GenerationType type)
    {
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
                multipleGenerationCoroutine = StartCoroutine(MultipleGeneration(20));
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


}
