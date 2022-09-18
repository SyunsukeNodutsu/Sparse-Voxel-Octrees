//-----------------------------------------------------------------------------
// File: Cell.cs
//
// 分木の分割された空間クラス
//-----------------------------------------------------------------------------

/// <summary>分木の分割された空間クラス</summary>
public class Cell<T>
{
    private TreeData<T> m_latestData;
    public TreeData<T> FirstData { get { return m_latestData; } }

    /// <summary>TreeDataが抜ける際に通知</summary>
    public bool OnRemove(TreeData<T> data)
    {
        if (m_latestData != data) return false;
        m_latestData = data.Next;
        return true;
    }

    /// <summary>空間にTreeDataオブジェクトを登録する</summary>
    public bool Push(TreeData<T> data)
    {
        if (data.Cell == this) return false;

        // 空間を登録
        data.Cell = this;

        // まだ空間にひとつも登録がない場合は、
        // リンクリストの初めのデータとして登録する
        if (m_latestData == null)
        {
            m_latestData = data;
            return true;
        }

        // 最新のTreeDataの参照を更新
        data.Next = m_latestData;
        m_latestData.Previous = data;
        m_latestData = data;

        return true;
    }

}
