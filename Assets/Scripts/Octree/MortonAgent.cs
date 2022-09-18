//-----------------------------------------------------------------------------
// File: OctreeSystem.cs
//
// アプリサイドで使用する汎用的な分割空間システム
//-----------------------------------------------------------------------------
using UnityEngine;

public class MortonAgent : MonoBehaviour
{
    public LinearTreeManager<GameObject> Manager
    {
        get { return m_manager; }
        set
        {
            if (m_manager == value) return;

            // Remove from current manager.
            TreeData.Remove();

            // Change to new manager and register myself.
            m_manager = value;
            RegisterUpdate();
            Debug.Log("Set Register.");
        }
    }
    public Bounds Bounds
    {
        get
        {
            if (m_collider == null)
                m_collider = GetComponent<Collider>();
            return m_collider.bounds;
        }
    }

    private LinearTreeManager<GameObject> m_manager;
    private Collider m_collider;

    public TreeData<GameObject> TreeData { get; private set; }

    private void Awake()
    {
        TreeData = new TreeData<GameObject>(gameObject);
    }

    private void OnDestroy()
    {
        TreeData.Remove();
    }

    private void Update()
    {
        if (m_manager == null) return;
        RegisterUpdate();
    }

    private void RegisterUpdate()
    {
        m_manager.Register(Bounds, TreeData);
    }

}
