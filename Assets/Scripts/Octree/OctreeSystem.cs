//-----------------------------------------------------------------------------
// File: OctreeSystem.cs
//
// アプリサイドで使用する汎用的な空間分割システム
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>アプリサイドで使用する汎用的な空間分割システム</summary>
public class OctreeSystem : MonoBehaviour
{
    //インスペクター =====================================================
    [Header("Octree Param")]
    [SerializeField][Range(0, 6)] private int m_level = 1;
    [SerializeField] private Vector3 m_areaMin = new(-10, -10, -10);
    [SerializeField] private Vector3 m_areaMax = new( 10,  10,  10);

    [Header("Debug Param")]
    [SerializeField] private bool m_viewMaxCells = false;
    [SerializeField] private int m_mortonNumber = 0;
    [SerializeField] private int m_belongLevel = 0;

    //変数群 =============================================================
    [NonSerialized] public List<GameObject> test = new();

    private LinearTreeManager<GameObject> m_linearTreeManager;
    private readonly List<GameObject> m_collisionList = new();

    //関数群 =============================================================
    private void Awake()
    {
        m_linearTreeManager = new LinearTreeManager<GameObject>();
        if (!m_linearTreeManager.Initialize(m_level, m_areaMin, m_areaMax))
            Debug.LogError("線分8分木空間の生成失敗.");
    }

    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.Space))
        {
            m_linearTreeManager.GetCellRegisterList(0, m_collisionList);
            Debug.Log("m_collisionList.Count: " + m_collisionList.Count);
            m_collisionList.Clear();
        }*/
    }

    /// <summary>ビューアーとしても最低限提供</summary>
    private void OnDrawGizmos()
    {
        // 分割が最大の場合を可視化
        if (m_viewMaxCells)
        {
            int lv = 1 << m_level;
            int lvHalf = lv / 2;

            Vector3 unit = (m_areaMax - m_areaMin) / lv;

            Vector3 tow = transform.right * (m_areaMax.x - m_areaMin.x);
            Vector3 toh = transform.up * (m_areaMax.y - m_areaMin.y);
            Vector3 tod = transform.forward * (m_areaMax.z - m_areaMin.z);

            for (int i = 0; i <= lv; i++)
            {
                for (int j = 0; j <= lv; j++)
                {
                    // XY平面
                    {
                        bool isCenter = (i == lvHalf || j == lvHalf);
                        Gizmos.color = isCenter ? Color.red : Color.blue;

                        Vector3 offset = (transform.right * (unit.x * i + m_areaMin.x)) + (transform.up * (unit.y * j + m_areaMin.y)) + (transform.forward * m_areaMin.z);
                        Vector3 from = transform.position + offset;
                        Vector3 to = from + tod;
                        Gizmos.DrawLine(from, to);
                    }
                    // YZ平面
                    {
                        bool isCenter = (i == lvHalf || j == lvHalf);
                        Gizmos.color = isCenter ? Color.red : Color.blue;

                        Vector3 offset = (transform.forward * (unit.z * i + m_areaMin.z)) + (transform.up * (unit.y * j + m_areaMin.y)) + (transform.right * m_areaMin.x);
                        Vector3 from = transform.position + offset;
                        Vector3 to = from + tow;
                        Gizmos.DrawLine(from, to);
                    }
                    // XZ平面
                    {
                        bool isCenter = (i == lvHalf || j == lvHalf);
                        Gizmos.color = isCenter ? Color.red : Color.blue;

                        Vector3 offset = (transform.forward * (unit.z * i + m_areaMin.z)) + (transform.right * (unit.x * j + m_areaMin.x)) + (transform.up * m_areaMin.y);
                        Vector3 from = transform.position + offset;
                        Vector3 to = from + toh;
                        Gizmos.DrawLine(from, to);
                    }
                }
            }
        }

        if (Application.isPlaying)
        {
            foreach (var b in test)
            {
                var bounds = b.GetComponent<Collider>().bounds;
                Vector3 min = bounds.min;
                Vector3 max = bounds.max;
                m_linearTreeManager.OffsetPosition(ref min, ref max);
                m_mortonNumber = m_linearTreeManager.GetMortonNumber(min, max, out m_belongLevel);
                m_mortonNumber = m_linearTreeManager.ToLinearSpace(m_mortonNumber, m_belongLevel);

                var aabb = m_linearTreeManager.GetBoundsFromMortonNumber(m_mortonNumber);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(aabb.center, aabb.size);
            }
        }
    }

    public void RegisterObject(GameObject target)
    {
        if (!target.TryGetComponent<MortonAgent>(out var agent)) return;
        agent.Manager = m_linearTreeManager;
    }

}
