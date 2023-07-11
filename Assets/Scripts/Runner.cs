using UnityEngine;
using UnityEngine.Android;

public class Runner : MonoBehaviour
{
    /// <summary>
    /// Set to 0.5 by default to match its visual size.
    /// </summary>
    [SerializeField, Min(0f)]
    float extents = 0.5f;

    [SerializeField]
    Light pointLight;

    [SerializeField]
    ParticleSystem explosionSystem, trailSystem;

    [SerializeField, Min(0f)]
    float startSpeedX = 5f, maxSpeedX = 40f, jumpAcceleration = 100f, gravity = 40f;

    [SerializeField]
    FloatRange jumpDuration = new FloatRange(0.1f, 0.2f);

    /// <summary>
    /// The curve maps X velocity to acceleration.
    /// </summary>
    [SerializeField]
    AnimationCurve runAccelerationCurve;

    [SerializeField, Min(0f)]
    float spinDuration = 0.75f;

    float spinTimeRemaining;

    Vector3 spinRotation;

    SkylineObject currentObstacle;

    MeshRenderer meshRenderer;

    Vector2 position, velocity;

    /// <summary>
    /// Keep track of whether we're currently in a transition between two obstables.
    /// </summary>
    bool transitioning;

    bool grounded;

    float jumpTimeRemaining;

    public Vector2 Position => position;

    public float SpeedX
    {
        get => velocity.x;
        set => velocity.x = value;
    }

    void SetTrailEmission(bool enabled)
    {
        ParticleSystem.EmissionModule emission = trailSystem.emission;
        emission.enabled = enabled;
    }

    private void Awake()
    {
        // Initially both the renderer and light are disabled.
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;
        pointLight.enabled = false;
    }

    public void StartNewGame(SkylineObject obstacle)
    {
        // loop through the obstacle sequence until one is found that entirely contains the runner at its starting position,
        // which is zero.
        currentObstacle = obstacle;
        while (currentObstacle.MaxX < extents)
        {
            currentObstacle = currentObstacle.Next;
        }

        // Sets the position to zero and clear the rotation
        position = new Vector2(0f, currentObstacle.GapY.min + extents);
        transform.SetPositionAndRotation(position, Quaternion.identity);

        // Enables the renderer and light
        meshRenderer.enabled = true;
        pointLight.enabled = true;

        // Clears the explosion system
        explosionSystem.Clear();

        // Enables, clears, and plays the trail system
        SetTrailEmission(true);
        trailSystem.Clear();
        trailSystem.Play();

        transitioning = false;
        grounded = true;
        jumpTimeRemaining = 0f;
        spinTimeRemaining = 0f;

        velocity = new Vector2(startSpeedX, 0f);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dt"></param>
    /// <returns> Whether the runner is still active </returns>
    public bool Run(float dt)
    {
        Move(dt);

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

            // Check for a collision before constraining based on the next obstacle.
            // But this should only be done once per transition
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
                transitioning = false;
            }
        }
        return true;
    }

    void Move(float dt)
    {
        if (jumpTimeRemaining > 0f)
        {
            jumpTimeRemaining -= dt;
            velocity.y += jumpAcceleration * Mathf.Min(dt, jumpTimeRemaining);
        }
        else
        {
            velocity.y -= gravity * dt;
        }

        //  Apply acceleration by evaluating the curve based on the current X speed divided by its max,
        //  adding that scaled by the delta time to the X velocity, and limiting that to the max.
        if (grounded)
        {
            velocity.x = Mathf.Min(
                velocity.x + runAccelerationCurve.Evaluate(velocity.x / maxSpeedX) * dt,
                maxSpeedX
            );
            grounded = false;
        }
        position += velocity * dt;
    }

    /// <summary>
    /// Synchronizes the game object's position with its 2D position.
    /// </summary>
    public void UpdateVisualization()
    {
        transform.localPosition = position;

        if (spinTimeRemaining > 0f)
        {
            spinTimeRemaining = Mathf.Max(spinTimeRemaining - Time.deltaTime, 0f);
            transform.localRotation = Quaternion.Euler(
                Vector3.Lerp(spinRotation, Vector3.zero, spinTimeRemaining / spinDuration)
            );
        }
    }

    /// <summary>
    /// Clamps its Y position so it stays inside the vertical gap of a given obstacle.
    /// </summary>
    /// <param name="obstacle"></param>
    void ConstrainY(SkylineObject obstacle)
    {
        FloatRange openY = obstacle.GapY;

        // The floor is touched
        if (position.y - extents <= openY.min)
        {
            position.y = openY.min + extents;
            
            velocity.y = Mathf.Max(velocity.y, 0f);
            jumpTimeRemaining = 0f;
            grounded = true;
        }
        // The ceiling  is touched
        else if (position.y + extents >= openY.max)
        {
            position.y = openY.max - extents;
            
            velocity.y = Mathf.Min(velocity.y, 0f);
            jumpTimeRemaining = 0f;
        }

        obstacle.Check(this);
    }

    /// <summary>
    /// Checks whether the runner fits inside the vertical gap of the next obstacle at the transition point.
    /// </summary>
    /// <returns></returns>
    bool CheckCollision()
    {
        Vector2 transitionPoint;
        transitionPoint.x = currentObstacle.MaxX - extents;
        // The vertical component of the transition point depends on the now variable vertical speed,
        // so we have to rewind it.
        transitionPoint.y =
            position.y - velocity.y * (position.x - transitionPoint.x) / velocity.x;
        float shrunkExtents = extents - 0.01f;          // Besides adding a tiny bit of leniency it also prevents incorrect collisions
                                                        // due to floating-point precision limitations.
        FloatRange gapY = currentObstacle.Next.GapY;

        // If not move it back to the transition point and explode.
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

    public void StartJumping()
    {
        // A jump is only started for real if we're grounded, which sets the remaining jump time to its maximum.
        if (grounded)
        {
            jumpTimeRemaining = jumpDuration.max;

            if (spinTimeRemaining <= 0f)
            {
                spinTimeRemaining = spinDuration;
                spinRotation = Vector3.zero;
                spinRotation[Random.Range(0, 3)] = Random.value < 0.5f ? -90f : 90f;
            }
        }
    }

    /// <summary>
    /// This ensures that the minimum is always reached, 
    /// unless the player mashes buttons inhumanly fast, but there is no benefit to that.
    /// </summary>
    public void EndJumping() => jumpTimeRemaining += jumpDuration.min - jumpDuration.max;


    /// <summary>
    /// When the game ends the runner explodes
    /// </summary>
    void Explode()
    {
        // Disables the renderer, light, and trail emission
        meshRenderer.enabled = false;
        pointLight.enabled = false;
        SetTrailEmission(false);

        // Updates the game object's position
        transform.localPosition = position;

        // Triggers the explosion system to emit its maximum amount of particles
        explosionSystem.Emit(explosionSystem.main.maxParticles);
    }
}