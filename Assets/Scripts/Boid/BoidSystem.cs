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
    bool m_bSetupBoids = false;

    private void Start()
    {
        TryGetComponent(out m_octree);
        if (!m_octree)
            Debug.LogError("BoidSystem取得失敗 アタッチを確認してください.");
    }

    private void Update()
    {
        if (!m_bSetupBoids)
            m_bSetupBoids = SetupBoids();
    }

    private void OnDrawGizmos()
    {
        if (!m_param) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one * m_param.wallScale);
    }

    private bool SetupBoids()
    {
        if (!m_octree) return false;

        var scale = m_param.wallScale * 0.5f;

        for (int i = 0; i < m_boidCount; i++)
        {
            var emitPos = new Vector3(Random.Range(-scale, scale), Random.Range(-scale, scale), Random.Range(-scale, scale));

            var instance = Instantiate(m_boidPrefab, emitPos, Random.rotation);
            //var instance = Instantiate(m_boidPrefab, Vector3.zero, Quaternion.identity);
            instance.name = "Boid_" + i.ToString();

            var boid = instance.GetComponent<Boid>();
            if (boid)
            {
                boid.BoidSystem = this;
                boid.LinearTreeManager = m_octree.GetLinearTreeManager();
                boid.BoidParam = m_param;

                m_boidList.Add(boid);
                m_octree.RegisterObject(instance);
            }
        }

        return true;
    }

}
