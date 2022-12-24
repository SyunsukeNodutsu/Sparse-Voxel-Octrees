using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using Debug = UnityEngine.Debug;
using MBaske.Octree;

public class OctreeTest : MonoBehaviour
{
    private class TestContent : INodeContentNNS
    {
        public Bounds Bounds { get; private set; }
        public Vector3 Position => Bounds.center;
        public float NNDistance { get; set; }
        public INodeContentNNS Nearest { get; set; }
        float INodeContentNNS.SqrDistanceNN { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

        public TestContent()
        {
            Bounds = RandomBounds(0.5f, 6f);
            NNDistance = Mathf.Infinity;
            Nearest = null;
        }
    }

    private struct ThickRay
    {
        public Ray Ray;
        public float Radius;
        public float Length;

        private Vector3 m_Perp;

        public void Draw()
        {
            if (Radius > 0)
            {
                m_Perp = m_Perp == default 
                    ? Vector3.Cross(Random.onUnitSphere, Ray.direction).normalized * Radius
                    : m_Perp;

                for (int i = 0; i < 360; i += 10)
                {
                    var rot = Quaternion.AngleAxis(i, Ray.direction);
                    Gizmos.DrawLine(Ray.origin + rot * m_Perp,
                        Ray.origin + Ray.direction * Length + rot * m_Perp);
                }
            }
            else
            {
                Gizmos.DrawRay(Ray.origin, Ray.direction * Length);
            }
        }
    }

    private struct Sphere
    {
        public Vector3 Center;
        public float Radius;

        public void Draw()
        {
            Gizmos.DrawSphere(Center, Radius);
        }
    }

    private enum Method
    {
        Add, 
        AddCalcNearestNeighbor,
        Remove, 
        RemoveCalcNearestNeighbor,
        Count,
        FindAll,
        FindBoundsIntersectBounds,
        FindBoundsIntersectSphere,
        FindBoundsIntersectRay,
        FindBoundsIntersectFrustum,
        FindBoundsInsideBounds,
        FindPositionsInsideBounds,
        FindBoundsInsideSphere,
        FindPositionsInsideSphere,
        FindPositionsNearby,
        FindBoundsInsideRay,
        FindPositionsInsideRay,
        FindContainsPoint
    }

    private enum Type
    {
        SpeedTest, Visualization
    }

    [SerializeField]
    private Method m_Method = Method.Add;
    [SerializeField]
    private Type m_Type = Type.SpeedTest;

    [Space]
    [SerializeField]
    private int m_NumSpeedTestIterations = 100;
    [SerializeField]
    private int m_NumSpeedTestObjects = 10000;
   
    [Space]
    [SerializeField]
    private int m_NumVisualizedObjects = 600;
    [SerializeField]
    private int m_RandomizationInterval = 60;
    [SerializeField]
    private bool m_DrawNodeBounds = true;
    [SerializeField]
    private bool m_DrawContentBounds = true;
    [SerializeField]
    private bool m_DrawContentPositions = true;

    [Space]
    [SerializeField]
    private float m_NearestNeighborMaxDistance = 8;
    [SerializeField]
    private bool m_NearestNeighborSpaceIsSphere = true;

    private Node<TestContent> m_Tree;
    private List<TestContent> m_Contents;
    private HashSet<TestContent> m_Buffer;
    private TestContent m_NNContent;
    private bool m_UseFrameInterval;
    private int m_FrameCount;
    private Vector3 m_VisPoint;
    private Bounds m_VisBounds;
    private Sphere m_VisSphere;
    private ThickRay m_VisRay;

    private void Start()
    {
        m_Tree = new Node<TestContent>();
        m_Contents = new List<TestContent>();
        m_Buffer = new HashSet<TestContent>();

        if (m_Type == Type.SpeedTest)
        {
            switch (m_Method)
            {
                case Method.AddCalcNearestNeighbor:
                case Method.RemoveCalcNearestNeighbor:
                    var msg = "";
                    if (m_NumSpeedTestObjects > 1000)
                    {
                        msg += " Reducing to 1000 objects.";
                        m_NumSpeedTestObjects = 1000;
                    }
                    if (m_NumSpeedTestIterations > 10)
                    {
                        msg += " Reducing to 10 iterations.";
                        m_NumSpeedTestIterations = 10;
                    }
                    if (msg.Length > 0)
                    {
                        Debug.LogWarning("Calculating nearest neighbors can be slow, especially with larger " +
                        "'Nearest Neighbor Max Distance' values. Too many objects and iterations might freeze Unity." + msg);
                    }
                    break;
            }
            SpeedTest();
        }
        else
        {
            switch (m_Method)
            {
                case Method.Count:
                case Method.FindAll:
                    Debug.LogError("No visualization available.");
                    break;
                default:
                    Visualize();
                    break;
            }
        }
    }

    private void TryPreAddContents()
    {
        if (m_Method != Method.Add && m_Method != Method.AddCalcNearestNeighbor)
        {
            if (m_Method == Method.RemoveCalcNearestNeighbor)
            {
                m_Tree.Add(m_Contents, m_NearestNeighborMaxDistance, m_NearestNeighborSpaceIsSphere);
            }
            else
            {
                m_Tree.Add(m_Contents);
            }
        }
    }

    private void SpeedTest()
    {
        var sw = new Stopwatch();
        var ellapsed = new List<float>(m_NumSpeedTestIterations);

        for (int i = 0; i < m_NumSpeedTestIterations; i++)
        {
            m_Tree.Dispose();
            m_Contents.Clear();
            for (int j = 0; j < m_NumSpeedTestObjects; j++)
            {
                m_Contents.Add(new TestContent());
            }
            TryPreAddContents();

            sw.Restart();

            switch (m_Method)
            {
                case Method.Add:
                    SpeedTest_Add();
                    break;
                case Method.AddCalcNearestNeighbor:
                    SpeedTest_AddCalcNearestNeighbor();
                    break;
                case Method.Remove:
                    SpeedTest_Remove();
                    break;
                case Method.RemoveCalcNearestNeighbor:
                    SpeedTest_RemoveCalcNearestNeighbor();
                    break;
                case Method.Count:
                    SpeedTest_Count();
                    break;
                case Method.FindAll:
                    SpeedTest_FindAll();
                    break;
                case Method.FindBoundsIntersectBounds:
                    SpeedTest_FindBoundsIntersectBounds();
                    break;
                case Method.FindBoundsIntersectSphere:
                    SpeedTest_FindBoundsIntersectSphere();
                    break;
                case Method.FindBoundsIntersectRay:
                    SpeedTest_FindBoundsIntersectRay();
                    break;
                case Method.FindBoundsIntersectFrustum:
                    SpeedTest_FindBoundsIntersectFrustum();
                    break;
                case Method.FindBoundsInsideBounds:
                    SpeedTest_FindBoundsInsideBounds();
                    break;
                case Method.FindPositionsInsideBounds:
                    SpeedTest_FindPositionsInsideBounds();
                    break;
                case Method.FindBoundsInsideSphere:
                    SpeedTest_FindBoundsInsideSphere();
                    break;
                case Method.FindPositionsInsideSphere:
                case Method.FindPositionsNearby:
                    SpeedTest_FindPositionsInsideSphere();
                    break;
                case Method.FindBoundsInsideRay:
                    SpeedTest_FindBoundsInsideRay();
                    break;
                case Method.FindPositionsInsideRay:
                    SpeedTest_FindPositionsInsideRay();
                    break;
                case Method.FindContainsPoint:
                    SpeedTest_FindContainingPoint();
                    break;
            }

            ellapsed.Add(sw.ElapsedMilliseconds);
        }

        Debug.Log($"Method '{m_Method}' took {ellapsed.Average()} ms (avg) for {m_NumSpeedTestObjects} objects.");
    }

    private void SpeedTest_Add()
    {
        foreach (var obj in m_Contents)
        {
            m_Tree.Add(obj);
        }
    }

    private void SpeedTest_AddCalcNearestNeighbor()
    {
        foreach (var obj in m_Contents)
        {
            m_Tree.Add(obj, m_NearestNeighborMaxDistance, m_NearestNeighborSpaceIsSphere);
        }
    }

    private void SpeedTest_Remove()
    {
        foreach (var obj in m_Contents)
        {
            m_Tree.Remove(obj);
        }
    }

    private void SpeedTest_RemoveCalcNearestNeighbor()
    {
        foreach (var obj in m_Contents)
        {
            m_Tree.Remove(obj, m_NearestNeighborMaxDistance, m_NearestNeighborSpaceIsSphere);
        }
    }

    private void SpeedTest_Count()
    {
        Debug.Log($"Counted {m_Tree.Count} object(s).");
    }

    private void SpeedTest_FindAll()
    {
        m_Buffer.Clear();
        m_Tree.FindAll(m_Buffer);
        Debug.Log($"Found {m_Buffer.Count} objects."); 
    }

    private void SpeedTest_FindBoundsIntersectBounds()
    {
        m_Buffer.Clear();
        m_Tree.FindBoundsIntersectBounds(m_Buffer, RandomBounds());
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindBoundsIntersectSphere()
    {
        m_Buffer.Clear();
        var sphere = RandomSphere();
        m_Tree.FindBoundsIntersectSphere(m_Buffer, sphere.Center, sphere.Radius);
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindBoundsIntersectRay()
    {
        m_Buffer.Clear();
        var ray = RandomRay(RandomBool());
        m_Tree.FindBoundsIntersectRay(m_Buffer, ray.Ray, ray.Radius, ray.Length);
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindBoundsIntersectFrustum()
    {
        m_Buffer.Clear();
        m_Tree.FindBoundsIntersectFrustum(m_Buffer, RandomFrustum());
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindBoundsInsideBounds()
    {
        m_Buffer.Clear();
        m_Tree.FindBoundsInsideBounds(m_Buffer, RandomBounds());
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindPositionsInsideBounds()
    {
        m_Buffer.Clear();
        m_Tree.FindPositionsInsideBounds(m_Buffer, RandomBounds());
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindBoundsInsideSphere()
    {
        m_Buffer.Clear();
        var sphere = RandomSphere();
        m_Tree.FindBoundsInsideSphere(m_Buffer, sphere.Center, sphere.Radius);
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindPositionsInsideSphere()
    {
        m_Buffer.Clear();
        var sphere = RandomSphere();
        m_Tree.FindPositionsInsideSphere(m_Buffer, sphere.Center, sphere.Radius);
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindBoundsInsideRay()
    {
        m_Buffer.Clear();
        var ray = RandomRay();
        m_Tree.FindBoundsInsideRay(m_Buffer, ray.Ray, ray.Radius, ray.Length);
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindPositionsInsideRay()
    {
        m_Buffer.Clear();
        var ray = RandomRay();
        m_Tree.FindPositionsInsideRay(m_Buffer, ray.Ray, ray.Radius, ray.Length);
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }

    private void SpeedTest_FindContainingPoint()
    {
        m_Buffer.Clear();
        m_Tree.FindContainsPoint(m_Buffer, RandomPoint());
        Debug.Log($"Found {m_Buffer.Count} objects.");
    }


    private void Visualize()
    {
        Application.targetFrameRate = 60;

        for (int i = 0; i < m_NumVisualizedObjects; i++)
        {
            m_Contents.Add(new TestContent());
        }
        TryPreAddContents();

        m_UseFrameInterval =
            m_Method != Method.Add &&
            m_Method != Method.AddCalcNearestNeighbor &&
            m_Method != Method.Remove &&
            m_Method != Method.RemoveCalcNearestNeighbor;

        if (m_Method == Method.FindBoundsIntersectFrustum)
        {
            Debug.LogWarning("Select camera to gizmo-draw its frustum.");
        }
    }

    private void Update()
    {
        if (m_Type == Type.Visualization)
        {
            if (m_UseFrameInterval)
            {
                if (m_FrameCount % m_RandomizationInterval == 0)
                {
                    m_Buffer.Clear();
                    switch (m_Method)
                    {
                        case Method.FindBoundsIntersectBounds:
                            m_VisBounds = RandomBounds();
                            m_Tree.FindBoundsIntersectBounds(m_Buffer, m_VisBounds);
                            break;
                        case Method.FindBoundsIntersectSphere:
                            m_VisSphere = RandomSphere();
                            m_Tree.FindBoundsIntersectSphere(m_Buffer, m_VisSphere.Center, m_VisSphere.Radius);
                            break;
                        case Method.FindBoundsIntersectRay:
                            m_VisRay = RandomRay(RandomBool());
                            m_Tree.FindBoundsIntersectRay(m_Buffer, m_VisRay.Ray, m_VisRay.Radius, m_VisRay.Length);
                            break;
                        case Method.FindBoundsIntersectFrustum:
                            m_Tree.FindBoundsIntersectFrustum(m_Buffer, RandomFrustum());
                            break;
                        case Method.FindBoundsInsideBounds:
                            m_VisBounds = RandomBounds(8, 32);
                            m_Tree.FindBoundsInsideBounds(m_Buffer, m_VisBounds);
                            break;
                        case Method.FindPositionsInsideBounds:
                            m_VisBounds = RandomBounds(8, 32);
                            m_Tree.FindPositionsInsideBounds(m_Buffer, m_VisBounds);
                            break;
                        case Method.FindBoundsInsideSphere:
                            m_VisSphere = RandomSphere(8, 16);
                            m_Tree.FindBoundsInsideSphere(m_Buffer, m_VisSphere.Center, m_VisSphere.Radius);
                            break;
                        case Method.FindPositionsInsideSphere:
                        case Method.FindPositionsNearby:
                            m_VisSphere = RandomSphere(8, 16);
                            m_Tree.FindPositionsInsideSphere(m_Buffer, m_VisSphere.Center, m_VisSphere.Radius);
                            break;
                        case Method.FindBoundsInsideRay:
                            m_VisRay = RandomRay();
                            m_Tree.FindBoundsInsideRay(m_Buffer, m_VisRay.Ray, m_VisRay.Radius, m_VisRay.Length);
                            break;
                        case Method.FindPositionsInsideRay:
                            m_VisRay = RandomRay();
                            m_Tree.FindPositionsInsideRay(m_Buffer, m_VisRay.Ray, m_VisRay.Radius, m_VisRay.Length);
                            break;
                        case Method.FindContainsPoint:
                            m_VisPoint = RandomPoint();
                            m_Tree.FindContainsPoint(m_Buffer, m_VisPoint);
                            break;
                    }
                }
            }
            else if (m_FrameCount < m_NumVisualizedObjects)
            {
                switch (m_Method)
                {
                    case Method.Add:
                        m_Tree.Add(m_Contents[m_FrameCount]);
                        break;
                    case Method.AddCalcNearestNeighbor:
                        m_NNContent = m_Contents[m_FrameCount];
                        m_Tree.Add(m_NNContent, m_NearestNeighborMaxDistance, m_NearestNeighborSpaceIsSphere);
                        break;
                    case Method.Remove:
                        m_Tree.Remove(m_Contents[m_FrameCount]);
                        break;
                    case Method.RemoveCalcNearestNeighbor:
                        m_NNContent = m_Contents[m_FrameCount];
                        m_Tree.Remove(m_NNContent, m_NearestNeighborMaxDistance, m_NearestNeighborSpaceIsSphere);
                        break;
                }
            }

            m_FrameCount++;
        }
    }


    private Color[] col_Node = new Color[]
    {
        new Color(0, 0.0f, 0.8f, 1),
        new Color(0.0f, 0.1f, 0.6f, 1),
        new Color(0.1f, 0.2f, 0.4f, 1),
        new Color(0.1f, 0.3f, 0.2f, 1)
    };

    private Color col_Bounds = new Color(0.4f, 0.1f, 0, 0.25f);
    private Color col_Search = new Color(0.75f, 1, 0, 0.75f);
    private Color col_Select = new Color(1f, 0.25f, 0.25f, 1);

    private void OnDrawGizmos()
    {
        if (m_Type == Type.Visualization && m_Tree != null)
        {
            if (m_DrawNodeBounds)
            {
                m_Tree.DrawNodeBounds(col_Node);
            }
            if (m_DrawContentBounds)
            {
                Gizmos.color = col_Bounds;
                m_Tree.DrawContentBounds();
            }
            if (m_DrawContentPositions)
            {
                Gizmos.color = Color.white;
                m_Tree.DrawContentPositions();
            }

            Gizmos.color = col_Search;

            switch (m_Method)
            {
                case Method.AddCalcNearestNeighbor:
                case Method.RemoveCalcNearestNeighbor:
                    // NN Space.
                    Gizmos.color = Color.yellow * 0.75f;
                    if (m_NearestNeighborSpaceIsSphere)
                    {
                        Gizmos.DrawWireSphere(m_NNContent.Position, m_NearestNeighborMaxDistance);
                    }
                    else
                    {
                        Gizmos.DrawWireCube(m_NNContent.Position, Vector3.one * m_NearestNeighborMaxDistance * 2);
                    }
                    // NN Connections.
                    m_Buffer.Clear();
                    m_Tree.FindAll(m_Buffer);
                    foreach (var obj in m_Buffer)
                    {
                        if (obj.Nearest != null)
                        {
                            Gizmos.DrawLine(obj.Position, obj.Nearest.Position);
                        }
                    }
                    break;
                case Method.FindBoundsIntersectBounds:
                case Method.FindBoundsInsideBounds:
                case Method.FindPositionsInsideBounds:
                    Gizmos.DrawWireCube(m_VisBounds.center, m_VisBounds.size);
                    break;
                case Method.FindBoundsIntersectSphere:
                case Method.FindBoundsInsideSphere:
                case Method.FindPositionsInsideSphere:
                case Method.FindPositionsNearby:
                    m_VisSphere.Draw();
                    break;
                case Method.FindBoundsIntersectRay:
                case Method.FindBoundsInsideRay:
                case Method.FindPositionsInsideRay:
                    m_VisRay.Draw();
                    break;
                case Method.FindContainsPoint:
                    Gizmos.DrawSphere(m_VisPoint, 1);
                    break;
            }

            switch (m_Method)
            {
                case Method.FindBoundsIntersectBounds:
                case Method.FindBoundsIntersectSphere:
                case Method.FindBoundsIntersectRay:
                case Method.FindBoundsIntersectFrustum:
                case Method.FindBoundsInsideBounds:
                case Method.FindBoundsInsideSphere:
                case Method.FindBoundsInsideRay:
                case Method.FindContainsPoint:
                    Gizmos.color = col_Select;
                    foreach (var obj in m_Buffer)
                    {
                        Gizmos.DrawWireCube(obj.Bounds.center, obj.Bounds.size);
                    }
                    break;

                case Method.FindPositionsInsideBounds:
                case Method.FindPositionsInsideSphere:
                case Method.FindPositionsNearby:
                case Method.FindPositionsInsideRay:
                    Gizmos.color = Color.cyan;
                    foreach (var obj in m_Buffer)
                    {
                        Gizmos.DrawWireSphere(obj.Position, 0.5f);
                    }
                    break;
            }
        }
    }


    private static Vector3 RandomPoint(float maxRadius = 32f)
    {
        return Random.insideUnitSphere * maxRadius;
    }

    private static Bounds RandomBounds(float minSize = 4f, float maxSize = 16f, float maxRadius = 32f)
    {
        return new Bounds(
                Random.insideUnitSphere * maxRadius,
                new Vector3(
                    Random.Range(minSize, maxSize),
                    Random.Range(minSize, maxSize),
                    Random.Range(minSize, maxSize)));
    }

    private static Sphere RandomSphere(float minRadius = 2f, float maxRadius = 8f)
    {
        return new Sphere()
        {
            Center = RandomPoint(),
            Radius = Random.Range(minRadius, maxRadius)
        };
    }

    private static ThickRay RandomRay(bool hasRadius = true)
    {
        return new ThickRay()
        {
            Ray = new Ray(RandomPoint(16), Random.onUnitSphere),
            Radius = hasRadius ? Random.Range(4, 16) : 0,
            Length = Random.Range(8f, 64f)
        };
    }

    private static Camera RandomFrustum()
    {
        var cam = Camera.main;
        cam.transform.position = RandomPoint(16);
        cam.transform.rotation = Quaternion.FromToRotation(Vector3.up, Random.onUnitSphere);
        return cam;
    }

    private static bool RandomBool(float probablility = 0.5f)
    {
        return Random.value < probablility;
    }
}
