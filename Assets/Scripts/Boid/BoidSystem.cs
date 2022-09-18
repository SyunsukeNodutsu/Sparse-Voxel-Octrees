//-----------------------------------------------------------------------------
// File: BoidSystem.cs
//
// Boidの管理
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

/// <summary>Boidの管理</summary>
public class BoidSystem : MonoBehaviour
{
    [SerializeField] private GameObject m_boidPrefab;
    [SerializeField] private OctreeSystem m_octree;
    [SerializeField] private BoidParam m_param;
    [SerializeField] private int m_boidCount = 100;

    private readonly List<Boid> m_boidList = new();
    public ReadOnlyCollection<Boid> BoidList { get { return m_boidList.AsReadOnly(); } }

    private void Update()
    {
        while (m_boidList.Count < m_boidCount)
            AddBoid();

        while (m_boidList.Count > m_boidCount)
            RemoveBoid();
    }

    void OnDrawGizmos()
    {
        if (!m_param) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * m_param.wallScale);
    }

    void AddBoid()
    {
        var instance = Instantiate(m_boidPrefab, Random.insideUnitSphere, Random.rotation);
        instance.transform.SetParent(transform);

        var boid = instance.GetComponent<Boid>();
        if (boid)
        {
            boid.BoidSystem = this;
            boid.BoidParam = m_param;
            m_boidList.Add(boid);

            if (m_octree)
            {
                m_octree.RegisterObject(instance);
                m_octree.test.Add(instance);// TODO: とりあえずデモ使用
            }
        }
    }

    void RemoveBoid()
    {
        if (m_boidList.Count == 0) return;

        var lastIndex = m_boidList.Count - 1;
        var boid = m_boidList[lastIndex];

        Destroy(boid.gameObject);
        m_boidList.RemoveAt(lastIndex);
    }

}
