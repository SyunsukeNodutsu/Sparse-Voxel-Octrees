//-----------------------------------------------------------------------------
// File: BoidSystem.cs
//
// Boidの管理
// TODO：
// ・Box外に出た際に復帰する処理をかく
// ・Octreeから魚群を作成 オーダー数の改善
// ・魚のZ軸回転をさせない or 船みたいに戻し処理入れる
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

    private void Start()
    {
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
            boid.BoidParam = m_param;
            m_boidList.Add(boid);

            if (m_octree)
            {
                m_octree.RegisterObject(instance);
                m_octree.test.Add(instance);// TODO: とりあえずデモ使用
            }
        }
    }

}
