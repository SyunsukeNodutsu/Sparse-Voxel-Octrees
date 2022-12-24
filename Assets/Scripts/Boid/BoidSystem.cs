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
    [SerializeField] private BoidParam m_param;
    [SerializeField] private int m_boidCount = 100;

    private OctreeSystem m_octree;
    private readonly List<Boid> m_boidList = new();
    public ReadOnlyCollection<Boid> BoidList { get { return m_boidList.AsReadOnly(); } }

    private void Start()
    {
        TryGetComponent(out m_octree);
        if (!m_octree)
            Debug.LogError("BoidSystem取得失敗 アタッチを確認してください.");

        for (int i = 0; i < m_boidCount; i++)
            AddBoid(i);
    }

    void OnDrawGizmos()
    {
        if (!m_param) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * m_param.wallScale);
    }

    void AddBoid(int index)
    {
        var instance = Instantiate(m_boidPrefab, Random.insideUnitSphere, Random.rotation);
        instance.transform.SetParent(transform);
        instance.name = "Boid_" + index.ToString();

        var boid = instance.GetComponent<Boid>();
        if (boid)
        {
            boid.BoidSystem = this;
            boid.LinearTreeManager = m_octree.GetLinearTreeManager();
            boid.BoidParam = m_param;
            m_boidList.Add(boid);

            if (m_octree)
                m_octree.RegisterObject(instance);
        }
    }

}
