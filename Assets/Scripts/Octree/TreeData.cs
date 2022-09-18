//-----------------------------------------------------------------------------
// File: TreeData.cs
//
// 分木に登録されるデータオブジェクト
//-----------------------------------------------------------------------------

/// <summary>分木に登録されるデータオブジェクト</summary>
public class TreeData<T>
{
    public Cell<T> Cell
    {
        get { return m_cell; }
        set
        {
            if (value == m_cell) return;
            if (value == null) return;

            // Remove from current cell.
            Remove();
            m_cell = value;
        }
    }

    private Cell<T> m_cell;

    public T Object { get; private set; }
    public TreeData<T> Previous { get; set; }
    public TreeData<T> Next { get; set; }

    // コンストラクタ
    public TreeData(T target)
    {
        Object = target;
    }

    /// <summary>空間から逸脱する</summary>
    public bool Remove()
    {
        if (Cell == null) return false;
        if (!Cell.OnRemove(this)) return false;

        // 逸脱処理
        // リンクリストの前後をつなぎ、自身のリンクを外す
        if (Previous != null)
            Previous.Next = Next;

        if (Next != null)
            Next.Previous = Previous;

        Previous = null;
        Next = null;
        Cell = null;

        return true;
    }

}
