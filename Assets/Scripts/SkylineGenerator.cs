using UnityEngine;

/// <summary>
/// To generate a skyline we introduce a SkylineGenerator component type. 
/// For configuration it needs an array of SkylineObject prefabs, 
/// a distance along the Z dimension where it should place instances, 
/// and an altitude range along the Y dimension to control the height of the skyline.
/// </summary>
public class SkylineGenerator : MonoBehaviour
{
    /// <summary>
    /// Uses to fill the gaps it creates, but only if a prefab is provided.
    /// </summary>
    [SerializeField]
    SkylineObject gapPrefab;

    [SerializeField]
    SkylineObject[] prefabs;

    [SerializeField]
    float distance;

    [SerializeField]
    FloatRange altitude;

    /// <summary>
    /// Obstacles that are directly adjacent to each other should have the same elevation.
    /// We'll make room for elevation changes that can be navigated by including gaps in the skylines. 
    /// </summary>
    [SerializeField]
    FloatRange gapLength, sequenceLength;

    // To determine the visible X range take the camera's range and add a 10-unit border to it.
    const float border = 10f;

    Vector3 endPosition;

    SkylineObject leftmost, rightmost;

    /// <summary>
    /// Track of the end of the current sequence in the X dimension.
    /// </summary>
    float sequenceEndX;

    /// <summary>
    /// 
    /// </summary>
    /// <returns> Returns a random prefab instance and makes it a child of the generator. </returns>
    SkylineObject GetInstance()
    {
        SkylineObject instance = prefabs[Random.Range(0, prefabs.Length)].GetInstance();
        instance.transform.SetParent(transform, false);
        return instance;
    }

    /// <summary>
    /// When starting a new game the generator should recycle all its objects, 
    /// determine the start position, place an initial object, and then fill the view.
    /// </summary>
    /// <param name="view"></param>
    /// <returns></returns>
    public SkylineObject StartNewGame(TrackingCamera view)
    {
        while (leftmost != null)
        {
            leftmost = leftmost.Recycle();
        }

        FloatRange visibleX = view.VisibleX(distance).GrowExtents(border);
        endPosition = new Vector3(visibleX.min, altitude.RandomValue, distance);
        sequenceEndX = sequenceLength.RandomValue;                                  // Start with a random sequence length

        leftmost = rightmost = GetInstance();
        endPosition = rightmost.PlaceAfter(endPosition);
        FillView(view);
        
        return leftmost;
    }

    /// <summary>
    /// Fills the current view based on the tracking camera.
    /// </summary>
    /// <param name="view"></param>
    public void FillView(TrackingCamera view)
    {
        FloatRange visibleX = view.VisibleX(distance).GrowExtents(border);

        // Begin by recycling the leftmost objects as long as they are out of view and aren't the rightmost object.
        while (leftmost != rightmost && leftmost.MaxX < visibleX.min)
        {
            leftmost = leftmost.Recycle();
        }

        // Then add new rightmost objects as long as the view isn't filled yet.
        // First check whether the current sequence end has been passed.
        while (endPosition.x < visibleX.max)
        {
            if (endPosition.x > sequenceEndX)
            {
                // Start a new sequence with a random gap and sequence length.
                StartNewSequence(gapLength.RandomValue, sequenceLength.RandomValue);
            }
            rightmost = rightmost.Next = GetInstance();
            endPosition = rightmost.PlaceAfter(endPosition);
        }
    }

    /// <summary>
    /// Starts a new sequence at an arbitrary moment, given a gap and sequence length.
    /// The gap is made by moving the end position forward. 
    /// Then a new altitude can be chosen and the sequence end determined.
    /// </summary>
    /// <param name="gap"></param>
    /// <param name="sequence"></param>
    void StartNewSequence(float gap, float sequence)
    {
        if (gapPrefab != null)
        {
            rightmost = rightmost.Next = gapPrefab.GetInstance();
            rightmost.transform.SetParent(transform, false);
            rightmost.FillGap(endPosition, gap);
        }

        endPosition.x += gap;
        endPosition.y = altitude.RandomValue;
        sequenceEndX = endPosition.x + sequence;
    }
}
