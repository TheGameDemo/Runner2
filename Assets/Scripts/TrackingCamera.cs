using UnityEngine;

public class TrackingCamera:MonoBehaviour
{
    Vector3 offset, position;

    float viewFactorX;

    private void Awake()
    {
        offset = transform.localPosition;

        Camera c = GetComponent<Camera>();
        float viewFactorY = Mathf.Tan(c.fieldOfView * 0.5f * Mathf.Deg2Rad);
        viewFactorX = viewFactorY * c.aspect;
    }

    public void StartNewGame()
    {
        Track(Vector3.zero);
    }

    public void Track(Vector3 focusPoint)
    {
        position = focusPoint + offset;
        transform.localPosition = position;
    }

    public FloatRange VisibleX(float z) =>
    FloatRange.PositionExtents(position.x, viewFactorX * (z - position.z));
}
