using UnityEngine;

public class SlowdownObject : SkylineObject
{
    [SerializeField]
    Transform item;

    [SerializeField]
    ParticleSystem explosionSystem;

    [SerializeField]
    float radius = 1f;

    [SerializeField]
    float speedFactor = 0.75f;

    [SerializeField]
    float spawnProbability = 0.5f;

    public override void Check(Runner runner)
    {
        // If the item is active and the runner is close enough to it,
        // deactivate the item, trigger an explosion, and adjust the runner's speed.
        if (
            item.gameObject.activeSelf &&
            ((Vector2)item.position - runner.Position).sqrMagnitude < radius * radius
        )
        {
            item.gameObject.SetActive(false);
            explosionSystem.Emit(explosionSystem.main.maxParticles);
            runner.SpeedX *= speedFactor;
        }
    }

    /// <summary>
    /// When the object enables activate the item based on the spawn probability, 
    /// so not all objects have the item. Also clear the explosion system.
    /// </summary>
    void OnEnable()
    {
        item.gameObject.SetActive(Random.value < spawnProbability);
        explosionSystem.Clear();
    }
}
