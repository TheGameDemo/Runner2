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

    [SerializeField]
    SkylineGenerator[] skylineGenerators;

    /// <summary>
    /// The obstacles will need a bit more attention than the regular skylines and aren't optional
    /// </summary>
    [SerializeField]
    SkylineGenerator obstacleGenerator;

    /// <summary>
    /// Use to increase the gap and sequence length when invoking FillView on the obstacle generator when the game is updated.
    /// </summary>
    [SerializeField]
    float extraGapFactor = 0.5f, extraSequenceFactor = 1f;

    bool isPlaying;

    private void Awake()
    {
        Application.targetFrameRate = 120;
    }

    void StartNewGame()
    {
        trackingCamera.StartNewGame();
        runner.StartNewGame(obstacleGenerator.StartNewGame(trackingCamera));    // Start and fill obstacle before the other generators.
        trackingCamera.Track(runner.Position);

        // To generate the skylines
        for (int i = 0; i < skylineGenerators.Length; i++)
        {
            skylineGenerators[i].StartNewGame(trackingCamera);
        }

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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            runner.StartJumping();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            runner.EndJumping();
        }

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

        obstacleGenerator.FillView(
            trackingCamera,
            runner.SpeedX * extraGapFactor,
            runner.SpeedX * extraSequenceFactor
        );
        for (int i = 0; i < skylineGenerators.Length; i++)
        {
            skylineGenerators[i].FillView(trackingCamera);
        }
    }
}