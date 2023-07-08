using TMPro;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField, Min(0.001f)]
    float maxDeltaTime = 1f / 120f;

    [SerializeField]
    Runner runner;

    [SerializeField]
    TrackingCamera trackingCamera;

    [SerializeField]
    TextMeshPro displayText;

    bool isPlaying;

    void StartNewGame()
    {
        trackingCamera.StartNewGame();
        runner.StartNewGame();
        isPlaying = true;
    }

    void Update()
    {
        if (isPlaying)
        {
            UpdateGame();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            StartNewGame();
        }
    }

    void UpdateGame()
    {
        // To enforce precise movement we'll implement a maximum delta time.
        float accumulateDeltaTime = Time.deltaTime;
        while (accumulateDeltaTime > maxDeltaTime && isPlaying)
        {
            isPlaying = runner.Run(maxDeltaTime);
            accumulateDeltaTime -= maxDeltaTime;
        }

        isPlaying = isPlaying && runner.Run(accumulateDeltaTime);
        
        runner.UpdateVisualization();
        trackingCamera.Track(runner.Position);
        displayText.SetText("{0}", Mathf.Floor(runner.Position.x));
    }
}