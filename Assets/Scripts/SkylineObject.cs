using System.Collections.Generic;
using UnityEngine;

public class SkylineObject : MonoBehaviour
{
    /// <summary>
    /// To move across an object we need to know the vertical position of its floor.
    /// </summary>
    [SerializeField]
    FloatRange gapY;

    [SerializeField, Min(1f)]
    float extents;

    /// <summary>
    /// As we'll be creating and removing these objects to fill the skyline while the camera moves, 
    /// let's include a simple object-pooling system to reuse these objects.
    /// </summary>
    [System.NonSerialized]
    Stack<SkylineObject> pool;

    /// <summary>
    /// Get vertical gap shifted to match the object's vertical position.
    /// </summary>
    public FloatRange GapY => gapY.Shift(transform.localPosition.y);

    /// <summary>
    /// Get the rightmost X position filled by the object
    /// </summary>
    public float MaxX => transform.localPosition.x + extents;

    /// <summary>
    /// Positions its directly after a give position and returns the position directly after itself.
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Vector3 PlaceAfter(Vector3 position)
    {
        position.x += extents;
        transform.localPosition = position;
        position.x += extents;
        return position;
    }

    /// <summary>
    /// Keep track of by having each object contain a reference to the next one
    /// </summary>
    public SkylineObject Next
    { get; set; }

    /// <summary>
    /// It creates a new pool for itself if needed, 
    /// then tries to pop an instance from the pool and activate it.
    /// 
    /// If an instance isn't available a new one is created and its pool it set.
    /// 
    /// This will work when playing after a domain reload, 
    /// but if you have those disabled the pools will persist across plays and contain references to destroyed game objects.
    /// </summary>
    /// <returns></returns>
    public SkylineObject GetInstance()
    {
        if (pool == null)
        {
            pool = new();
#if UNITY_EDITOR
            pools.Add(pool);
#endif
        }
        if (pool.TryPop(out SkylineObject instance))
        {
            instance.gameObject.SetActive(true);
        }
        else
        {
            instance = Instantiate(this);
            instance.pool = pool;
        }
        return instance;
    }

    /// <summary>
    /// To avoid that we can keep track of the pools via a static list and clear them before the scene is loaded, 
    /// which is only needed in the editor.
    /// </summary>
#if UNITY_EDITOR
    static List<Stack<SkylineObject>> pools;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ClearPools()
    {
        if (pools == null)
        {
            pools = new();
        }
        else
        {
            for (int i = 0; i < pools.Count; i++)
            {
                pools[i].Clear();
            }
        }
    }
#endif

    /// <summary>
    /// We'll make the runner keep track of which skyline object it is currently traversing.
    /// To make this easy and ensure that an objects never gets recycled while the runner is using it we'll introduce extra gap objects.
    /// 
    /// Uses a position and gap length to set its own extents and then positions itself.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="gap"></param>
    public void FillGap(Vector3 position, float gap)
    {
        extents = gap * 0.5f;
        position.x += extents;
        transform.localPosition = position;
    }

    /// <summary>
    /// It pushes the instance onto the pool, deactivates it, and clears its next reference. 
    /// Also have it return the original next reference as that will always be needed when an instance gets recycled.
    /// </summary>
    /// <returns></returns>
    public SkylineObject Recycle()
    {
        pool.Push(this);
        gameObject.SetActive(false);
        SkylineObject n = Next;
        Next = null;
        return n;
    }
}