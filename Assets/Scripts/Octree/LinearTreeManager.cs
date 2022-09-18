//-----------------------------------------------------------------------------
// File: LinearTreeManager.cs
//
// 線形8分木空間管理マネージャー
//-----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

/// <summary>線形8分木空間管理マネージャー</summary>
public class LinearTreeManager<T>
{
    private readonly int _MaxLevel = 4;// 分割最大数

    private int m_level = 1;
    private Vector3 m_width = new Vector3(1, 1, 1);
    private Vector3 m_unit = new Vector3(1, 1, 1);
    private Vector3 m_offset = new Vector3(0, 0, 0);

    private Cell<T>[] m_cellList;

    private int m_cellNum = 0;// 分割されたセル数の最大値
    private int m_parentShift;

    /// <summary>初期化</summary>
    /// <param name="level">分割レベル</param>
    /// <returns>成功したかどうか</returns>
    public bool Initialize(int level, Vector3 min, Vector3 max)
    {
        if (level > _MaxLevel + 1) return false;

        int[] m_pow = new int[_MaxLevel + 2];

        // 各レベルでの空間数を算出
        m_pow[0] = 1;
        for (int i = 1; i <= _MaxLevel + 1; i++)
            m_pow[i] = m_pow[i - 1] * 8;

        int denom = 8 - 1;
        m_cellNum = (m_pow[level + 1] - 1) / denom;
        m_cellList = new Cell<T>[m_cellNum];

        // 有効領域を登録
        m_offset.x = min.x;
        m_offset.y = min.y;
        m_offset.z = min.z;

        m_width = max - min;
        m_unit = m_width / (1 << level);

        m_level = level;

        m_parentShift = (int)Mathf.Log(8.0f, 2.0f);

        return true;
    }

    public bool OffsetPosition(ref Vector3 min, ref Vector3 max)
    {
        // 指定領域の分、オフセットして計算する
        min.x -= m_offset.x;
        max.x -= m_offset.x;
        min.y -= m_offset.y;
        max.y -= m_offset.y;
        min.z -= m_offset.z;
        max.z -= m_offset.z;

        if (min.x < 0)
        {
            Debug.LogError("All argumetns must be in initialized range.");
            return false;
        }
        if (max.x > m_width.x)
        {
            Debug.LogError("All argumetns must be in initialized range.");
            return false;
        }
        if (min.y < 0)
        {
            Debug.LogError("All argumetns must be in initialized range.");
            return false;
        }
        if (max.y > m_width.y)
        {
            Debug.LogError("All argumetns must be in initialized range.");
            return false;
        }
        if (min.z < 0)
        {
            Debug.LogError("All argumetns must be in initialized range.");
            return false;
        }
        if (max.z > m_width.z)
        {
            Debug.LogError("All argumetns must be in initialized range.");
            return false;
        }

        return true;
    }

    /// <summary>Boundsを元にオブジェクトを登録</summary>
    public bool Register(Bounds bounds, TreeData<T> data)
    {
        return Register(bounds.min, bounds.max, data);
    }

    /// <summary>指定範囲にオブジェクトを登録</summary>
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
            Debug.LogErrorFormat("Calcurated moton number is over the splited number. [MotonNumber: {0}]", elem);

            // 登録失敗
            return false;
        }

        // 算出されたモートン番号の空間がない場合は作成
        if (m_cellList[elem] == null)
        {
            CreateNewCell(elem);
        }

        return m_cellList[elem].Push(data);
    }

    /// <summary>指定された番号の空間を新規生成</summary>
    bool CreateNewCell(int elem)
    {
        while (m_cellList[elem] == null)
        {
            m_cellList[elem] = new Cell<T>();

            // 親空間を作成する（存在していなければ）
            //
            // 親空間の算出は「親番号 = (int)((子番号 - 1) / 8)」で算出できる。
            // ※ 2Dの場合は「4」で割る。空間分割数から。
            //
            // 結果として、8（4）で割るということは、3bit（2bit）シフトしていることに等しいため、（8（4）が1（単位）になる計算）
            // 計算では高速化のためビットシフトで計算する
            elem = (elem - 1) >> m_parentShift;

            // ルート空間の場合は-1になるためそこで終了
            if (elem == -1)
            {
                break;
            }
            // 空間分割数以上になったら終了
            else if (elem >= m_cellNum)
            {
                break;
            }
        }

        return true;
    }

    /// <summary>モートン番号を算出</summary>
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

        // TODO: ここだけm_divisionNumberに応じて変化しないため、あとで最適化する
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

    /// <summary>モートン番号を線形配列の空間番号に変換</summary>
    public int ToLinearSpace(int mortonNumber, int level)
    {
        int denom = 8 - 1;
        int additveNum = (int)((Mathf.Pow(8, level) - 1) / denom);
        return mortonNumber + additveNum;
    }

    static int BitSeparate3D(int n)
    {
        n = (n | (n << 8)) & 0x0000f00f;
        n = (n | (n << 4)) & 0x000c30c3;
        return (n | (n << 2)) & 0x00249249;
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

        // まぁ、ビジュアライザー使用だからええか...
        {
            if (level == 3 && x != 7) x -= (level + 4) - x;
            if (level == 3 && y != 7) y -= (level + 4) - y;
            if (level == 3 && z != 7) z -= (level + 4) - z;

            if (level == 2 && x != 3) x -= (level + 1) - x;
            if (level == 2 && y != 3) y -= (level + 1) - y;
            if (level == 2 && z != 3) z -= (level + 1) - z;

            if (level == 1 && x != 1) x -= (level + 0) - x;
            if (level == 1 && y != 1) y -= (level + 0) - y;
            if (level == 1 && z != 1) z -= (level + 0) - z;
        }

        Vector3 boxPos = new(x * 0.5f, y * 0.5f, z * 0.5f);
        Vector3 boxSize = m_width / (1 << level);
        Vector3 bpos = new(boxPos.x * boxSize.x, boxPos.y * boxSize.y, boxPos.z * boxSize.z);

        return new Bounds(bpos, boxSize);
    }

}
