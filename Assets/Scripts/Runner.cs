using UnityEngine;

public class Runner : MonoBehaviour
{
    [SerializeField, Min(0f)]
    float extents = 0.5f;

    [SerializeField]
    Light pointLight;

    [SerializeField]
    ParticleSystem explosionSystem, trailSystem;

    [SerializeField, Min(0f)]
    float startSpeedX = 5f;

    SkylineObject currentObstacle;

    MeshRenderer meshRenderer;

    Vector2 position;

    bool transitioning;

    public Vector2 Position => position;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;
        pointLight.enabled = false;
    }

    public void StartNewGame(SkylineObject obstacle)
    {
        currentObstacle = obstacle;
        while (currentObstacle.MaxX < extents)
        {
            currentObstacle = currentObstacle.Next;
        }

        position = new Vector2(0f, currentObstacle.GapY.min + extents);
        transform.localPosition = position;
        meshRenderer.enabled = true;
        pointLight.enabled = true;
        explosionSystem.Clear();
        SetTrailEmission(true);
        trailSystem.Clear();
        trailSystem.Play();
        transitioning = false;
    }

    void Explode()
    {
        meshRenderer.enabled = false;
        pointLight.enabled = false;
        SetTrailEmission(false);
        transform.localPosition = position;
        explosionSystem.Emit(explosionSystem.main.maxParticles);
    }

    void SetTrailEmission(bool enabled)
    {
        ParticleSystem.EmissionModule emission = trailSystem.emission;
        emission.enabled = enabled;
    }

    public bool Run(float dt)
    {
        position.x += startSpeedX * dt;

        if (position.x + extents < currentObstacle.MaxX)
        {
            ConstrainY(currentObstacle);
        }
        else
        {
            bool stillInsideCurrent = position.x - extents < currentObstacle.MaxX;
            if (stillInsideCurrent)
            {
                ConstrainY(currentObstacle);
            }

            if (!transitioning)
            {
                if (CheckCollision())
                {
                    return false;
                }
                transitioning = true;
            }

            ConstrainY(currentObstacle.Next);

            if (!stillInsideCurrent)
            {
                currentObstacle = currentObstacle.Next;
            }
        }
        return true;
    }

    public void UpdateVisualization()
    {
        transform.localPosition = position;
    }

    void ConstrainY(SkylineObject obstacle)
    {
        FloatRange openY = obstacle.GapY;
        if (position.y - extents <= openY.min)
        {
            position.y = openY.min + extents;
        }
        else if (position.y + extents >= openY.max)
        {
            position.y = openY.max - extents;
        }
    }

    bool CheckCollision()
    {
        Vector2 transitionPoint;
        transitionPoint.x = currentObstacle.MaxX - extents;
        transitionPoint.y = position.y;
        float shrunkExtents = extents - 0.01f;
        FloatRange gapY = currentObstacle.Next.GapY;
        if (
            transitionPoint.y - shrunkExtents < gapY.min ||
            transitionPoint.y + shrunkExtents > gapY.max
        )
        {
            position = transitionPoint;
            Explode();
            return true;
        }
        return false;
    }
}