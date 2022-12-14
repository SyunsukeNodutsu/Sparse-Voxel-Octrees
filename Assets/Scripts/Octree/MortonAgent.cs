//-----------------------------------------------------------------------------
// File: MortonAgent.cs
//
// 分木登録エージェント
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

            // 現在のTreeDataから削除
            TreeData.Remove();

            // 新規TreeDataを登録
            m_manager = value;
            RegisterUpdate();
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
        //m_manager.Register(Bounds.min, Bounds.max, TreeData);
        m_manager.Register(transform.position, transform.position, TreeData);
    }

}
