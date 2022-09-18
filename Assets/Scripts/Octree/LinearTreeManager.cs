//-----------------------------------------------------------------------------
// File: LinearTreeManager.cs
//
// 線形8分木空間管理マネージャー
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

/// <summary>線形8分木空間管理マネージャー</summary>
public class LinearTreeManager<T>
{
    private readonly int MAX_LEVEL = 6;// 分割最大数

    private int m_level = 1;// 分割レベル
    private Vector3 m_width = new(1, 1, 1);
    private Vector3 m_unit = new(1, 1, 1);
    private Vector3 m_offset = new(0, 0, 0);

    private Cell<T>[] m_cellList;

    private int m_cellNum = 0;// 分割されたセル数の最大値
    private int m_parentShift;

    /// <summary>初期化</summary>
    /// <param name="level">分割レベル</param>
    /// <returns>成功したかどうか</returns>
    public bool Initialize(int level, Vector3 min, Vector3 max)
    {
        if (level > MAX_LEVEL + 1) return false;

        int[] m_pow = new int[MAX_LEVEL + 2];

        // 各レベルでの空間数を算出
        m_pow[0] = 1;
        for (int i = 1; i <= MAX_LEVEL + 1; i++)
            m_pow[i] = m_pow[i - 1] * 8;

        m_cellNum = (m_pow[level + 1] - 1) / 7;
        m_cellList = new Cell<T>[m_cellNum];

        // 有効領域を登録
        m_offset = min;
        m_width = max - min;
        m_unit = m_width / (1 << level);
        m_level = level;

        m_parentShift = (int)Mathf.Log(8.0f, 2.0f);

        return true;
    }

    /// <summary>指定の空間番号に所属するTのリストを返す</summary>
    /// TODO: OutOfMemoryException: Out of memory
    public bool GetCellRegisterList(int elem, List<T> collisionList)
    {
        // ルート空間に登録されているリンクリストの最初の要素を取り出す
        TreeData<T> data = m_cellList[elem].FirstData;

        // データがなくなるまで繰り返す
        while (data != null)
        {
            // まず、リンクリストの次を取り出す
            TreeData<T> next = data.Next;
            while (next != null)
            {
                // 衝突リスト作成
                collisionList.Add(data.Object);
                next = next.Next;
            }

            data = data.Next;
        }

        // 小空間を巡る
        for (int i = 0; i < 8; i++)
        {
            int nextElem = elem * 8 + 1 + i;

            // 空間分割数以上 or 対象空間がない場合はスキップ
            bool needsSkip = (nextElem >= m_cellNum ||
                             m_cellList[nextElem] == null);
            if (needsSkip) continue;

            // 子空間を検索
            GetCellRegisterList(nextElem, collisionList);
        }

        return true;
    }

    /// <summary>モートン番号からBoundsを計算して返す</summary>
    public Bounds GetBoundsFromMortonNumber(int number)
    {
        int level = 0;

        // ハッシュ値から所属する最小空間のモートンオーダーに変換
        while (number >= Mathf.Pow(8, level))
        {
            number -= (int)Mathf.Pow(8, level);
            level++;
        }

        int s = 0;
        for (int i = level; i > 0; i--) s |= (number >> (3 * i - 2 - i) & (1 << i - 1));
        int x = s; s = 0;
        for (int i = level; i > 0; i--) s |= (number >> (3 * i - 1 - i) & (1 << i - 1));
        int y = s; s = 0;
        for (int i = level; i > 0; i--) s |= (number >> (3 * i - 0 - i) & (1 << i - 1));
        int z = s;

        {
            if (level == 6 && x != 63) x -= 63 - x;
            if (level == 6 && y != 63) y -= 63 - y;
            if (level == 6 && z != 63) z -= 63 - z;// 31 + 32 = 63

            if (level == 5 && x != 31) x -= 31 - x;
            if (level == 5 && y != 31) y -= 31 - y;
            if (level == 5 && z != 31) z -= 31 - z;// 15 + 16 = 31

            if (level == 4 && x != 15) x -= 15 - x;
            if (level == 4 && y != 15) y -= 15 - y;
            if (level == 4 && z != 15) z -= 15 - z;// 7 + 8 = 15

            if (level == 3 && x != 7) x -= 7 - x;
            if (level == 3 && y != 7) y -= 7 - y;
            if (level == 3 && z != 7) z -= 7 - z;// 3 + 4 = 7

            if (level == 2 && x != 3) x -= 3 - x;
            if (level == 2 && y != 3) y -= 3 - y;
            if (level == 2 && z != 3) z -= 3 - z;// 1 + 2 = 3

            if (level == 1 && x != 1) x -= 1 - x;
            if (level == 1 && y != 1) y -= 1 - y;
            if (level == 1 && z != 1) z -= 1 - z;// root = 0
        }

        Vector3 boxPos = new(x * 0.5f, y * 0.5f, z * 0.5f);
        Vector3 boxSize = m_width / (1 << level);
        Vector3 center = new(boxPos.x * boxSize.x, boxPos.y * boxSize.y, boxPos.z * boxSize.z);

        return new Bounds(center, boxSize);
    }

    /// <summary>モートン番号を計算して返す</summary>
    /// <param name="belongLevel">空間のレベル(root = 0)</param>
    public int GetMortonNumber(Vector3 min, Vector3 max, out int belongLevel)
    {
        // 左上手前のモートン番号を算出
        int ltd_x = (int)(min.x / m_unit.x);
        int ltd_y = (int)(min.y / m_unit.y);
        int ltd_z = (int)(min.z / m_unit.z);
        int ltd = BitSeparate3D(ltd_x) | (BitSeparate3D(ltd_y) << 1) | (BitSeparate3D(ltd_z) << 2);

        // 右下奥のモートン番号を算出
        int rbd_x = (int)(max.x / m_unit.x);
        int rbd_y = (int)(max.y / m_unit.y);
        int rbd_z = (int)(max.z / m_unit.z);
        int rbd = BitSeparate3D(rbd_x) | (BitSeparate3D(rbd_y) << 1) | BitSeparate3D(rbd_z) << 2;

        // 左上と右下のモートン番号のXORを取る
        int xor = ltd ^ rbd;
        int i = 0;
        int shift = 0;
        int spaceIndex = 0;

        while (xor != 0)
        {
            if ((xor & 0x7) != 0)
            {
                // 空間シフト数を採用
                spaceIndex = (i + 1);
                shift = spaceIndex * 3;
            }

            // 3bitシフトさせて再チェック
            xor >>= 3;
            i++;
        }

        // モートン番号
        int morton = rbd >> shift;

        // 所属する空間のレベル
        belongLevel = m_level - spaceIndex;

        return morton;
    }

    /// <summary>空間にオブジェクトを登録</summary>
    public bool Register(Vector3 min, Vector3 max, TreeData<T> data)
    {
        // 指定領域の分、オフセットして計算する
        if (!OffsetPosition(ref min, ref max)) return false;

        // オブジェクトの境界範囲からモートン番号を算出
        int belongLevel;
        int elem = GetMortonNumber(min, max, out belongLevel);
        elem = ToLinearSpace(elem, belongLevel);

        // 算出されたモートン番号が、生成した空間分割数より大きい場合はエラー
        if (elem >= m_cellNum)
        {
            Debug.LogError("分割数オーバー(MotonNumber" + elem + ").");
            return false;
        }

        // 算出されたモートン番号の空間がない場合は作成
        if (m_cellList[elem] == null)
            CreateNewCell(elem);

        return m_cellList[elem].Push(data);
    }

    /// <summary>指定された番号の空間を新規生成</summary>
    bool CreateNewCell(int elem)
    {
        while (m_cellList[elem] == null)
        {
            m_cellList[elem] = new Cell<T>();

            elem = (elem - 1) >> m_parentShift;

            // ルート空間の場合は-1になるためそこで終了
            if (elem == -1) break;
            // 空間分割数以上になったら終了
            else if (elem >= m_cellNum) break;
        }

        return true;
    }

    /// <summary>モートン番号を線形配列の空間番号に変換</summary>
    public int ToLinearSpace(int mortonNumber, int level)
    {
        int additveNum = (int)((Mathf.Pow(8, level) - 1) / 7);
        return mortonNumber + additveNum;
    }

    /// <summary>指定領域の分 オフセットして計算する</summary>
    public bool OffsetPosition(ref Vector3 min, ref Vector3 max)
    {
        min -= m_offset;
        max -= m_offset;

        if (min.x < 0) { Debug.LogError("範囲オーバー."); return false; }
        if (min.y < 0) { Debug.LogError("範囲オーバー."); return false; }
        if (min.z < 0) { Debug.LogError("範囲オーバー."); return false; }
        if (max.x > m_width.x) { Debug.LogError("範囲オーバー."); return false; }
        if (max.y > m_width.y) { Debug.LogError("範囲オーバー."); return false; }
        if (max.z > m_width.z) { Debug.LogError("範囲オーバー."); return false; }
        return true;
    }

    static int BitSeparate3D(int n)
    {
        n = (n | (n << 8)) & 0x0000f00f;
        n = (n | (n << 4)) & 0x000c30c3;
        return (n | (n << 2)) & 0x00249249;
    }

}
