using UnityEngine;

public class TrackingCamera : MonoBehaviour
{
    // We use the camera's starting position as the fixed offset.
    Vector3 offset, position;

    // To fill the entire view with our skyline we need to know how wide it is in world space.
    // This factor tells us the half-width or extents of the view in the X dimension per unit of distance from the camera.
    // It is found by taking the tangent of half the field of view converted to radians.
    float viewFactorX;

    ParticleSystem stars;

    private void Awake()
    {
        offset = transform.localPosition;

        Camera c = GetComponent<Camera>();
        float viewFactorY = Mathf.Tan(c.fieldOfView * 0.5f * Mathf.Deg2Rad);
        viewFactorX = viewFactorY * c.aspect;

        // Sets stars position halfway up the view and set its scale so it covers then entire width and half the height.
        stars = GetComponent<ParticleSystem>();
        ParticleSystem.ShapeModule shape = stars.shape;
        Vector3 position = shape.position;
        position.y = viewFactorY * position.z * 0.5f;
        shape.position = position;
        shape.scale = new Vector3(2f * viewFactorX, viewFactorY) * position.z;
    }

    public void StartNewGame()
    {
        // Set the camera back to its initial position.
        Track(Vector3.zero);


        stars.Clear();
        stars.Emit(stars.main.maxParticles);
    }

    /// <summary>
    /// Makes the camera track a focus point.
    /// </summary>
    /// <param name="focusPoint"></param>
    public void Track(Vector3 focusPoint)
    {
        position = focusPoint + offset;
        transform.localPosition = position;
    }

    public FloatRange VisibleX(float z) =>
        FloatRange.PositionExtents(position.x, viewFactorX * (z - position.z));
}
