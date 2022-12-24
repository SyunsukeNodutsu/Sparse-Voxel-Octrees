//-----------------------------------------------------------------------------
// File: Utility.cs
//
// 便利機能まとめ
//-----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>便利機能まとめクラス</summary>
public static class Utility
{
    /// <summary>floatをmin～maxの間で正規化(線形補完)</summary>
    /// <param name="val">設定対象の値</param>
    /// <param name="min">最小値</param>
    /// <param name="max">最大値</param>
    /// <returns></returns>
    public static float GetNormalizeVal(float val, float min, float max)
    {
        //val = Mathf.Clamp(val, min, max);
        if (val > 0) return (val - min) / (max - min);
        else if (val < 0) return (val - min) / (max - min);
        return 0;
    }

    /// <summary>最大値を"1"最小値を"0"に正規化し返す</summary>
    /// <param name="t">正規化する値</param>
    /// /// <returns>正規化された結果</returns>
    public static float GetZeroOneLinear(float min, float max, float t) { return (t - min) / (max - min); }

    /// <summary>"min"～"max"の値を"newMin"～"newMax"の値に正規化し返す</summary>
    /// <param name="t">正規化する値</param>
    /// <returns>正規化された結果</returns>
    public static float GetZeroOneLinearEx(float min, float max, float newMin, float newMax, float t) { return (t - min) / (max - min) * (newMax - newMin) + newMin; }

    /// <summary>XZ平面での距離を返す</summary>
    /// <param name="a">座標A</param>
    /// <param name="b">座標B</param>
    /// <returns>距離</returns>
    public static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float num = a.x - b.x;
        float num2 = a.z - b.z;
        return Mathf.Sqrt(num * num + num2 * num2);
    }

    /// <summary>Imageの不透明度を設定する</summary>
    /// <param name="image">設定対象のImageコンポーネント</param>
    /// <param name="alpha">不透明度(0～1)</param>
    public static void SetOpacity(Image image, float alpha)
    {
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, alpha);
    }

    /// <summary>カメラ(main)から画面中央にRayを飛ばし衝突座標を格納</summary>
    /// <param name="ignoreDistance">Rayを無視するカメラからの距離</param>
    /// <param name="retPos">格納される衝突座標</param>
    /// <returns>衝突したかどうか</returns>
    public static bool GetCameraRayPoint(float ignoreDistance, out Vector3 retPos)
    {
        var cameraPos = Camera.main.transform.position;
        var cameraDir = Camera.main.transform.forward;

        Vector3 tmpPos = new();
        float distance = float.MaxValue;

        bool checkOnceHit = false;

        var hitList = Physics.RaycastAll(cameraPos, cameraDir);
        foreach (var hit in hitList)
        {
            float hitDistance = Vector3.Distance(cameraPos, hit.point);

            if (hitDistance < ignoreDistance)
                continue;

            // 一番近いObjectを優先
            if (hitDistance < distance)
            {
                tmpPos = hit.point;
                distance = hitDistance;
                checkOnceHit = true;
            }
        }

        retPos = tmpPos;

        return checkOnceHit;
    }

    /// <summary>音量フェードを設定</summary>
    /// <note>内部的にSetVolumeがどう動いている？ ->小さい変化量は飛ばさないと重くなる</note>
    /// <param name="targetVolume">目標音量</param>
    /// <param name="fadeTime">フェードにかかる時間</param>
    public static IEnumerator SetFade(float targetVolume, float fadeTime)
    {
        float startVolume = AudioListener.volume;
        float timeCount = 0.0f;

        // 時間ごとの変化量
        float fadeVolume = (targetVolume - startVolume) / fadeTime;

        while (timeCount <= fadeTime)
        {
            AudioListener.volume = startVolume + (fadeVolume * timeCount);
            timeCount += Time.unscaledDeltaTime;
            yield return null;
        }

        AudioListener.volume = targetVolume;
    }

    /// <summary>音量フェードを設定</summary>
    /// <note>内部的にSetVolumeがどう動いている？ ->小さい変化量は飛ばさないと重くなる</note>
    /// <param name="source">フェードさせるオーディオソース</param>
    /// <param name="targetVolume">目標音量</param>
    /// <param name="fadeTime">フェードにかかる時間</param>
    public static IEnumerator SetFade(AudioSource source, float targetVolume, float fadeTime)
    {
        float startVolume = source.volume;
        float timeCount = 0.0f;

        // 時間ごとの変化量
        float fadeVolume = (targetVolume - startVolume) / fadeTime;

        while (timeCount <= fadeTime)
        {
            source.volume = startVolume + (fadeVolume * timeCount);
            timeCount += Time.unscaledDeltaTime;
            yield return null;
        }

        source.volume = targetVolume;
    }

#if UNITY_EDITOR
    /// <summary>UnityEditor コンソールログのクリア</summary>
    /// <note>注意.Releaseビルドの際は除外する必要がある</note>
    public static void ClearLog()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
#else
    public static void ClearLog(){}
#endif

}

/// <summary>テレイン関連の便利機能</summary>
public static class TerrainUtility
{
    /// <summary>アルファマップのデータ</summary>
    public class AlphamapsData
    {
        /// <summary>テクスチャレイヤーの名前</summary>
        public string layerName;
        /// <summary>ペイントの割合</summary>
        public float element;
    }

    /// <summary>TerrainDataからアルファマップデータを取得し返す</summary>
    /// <param name="terrainData">解析するテレインデータ</param>
    /// <param name="hitInfo">レイの結果情報</param>
    /// <param name="sizeX">取得する幅 (Default: 1)</param>
    /// <param name="sizeY">取得する高さ (Default: 1)</param>
    /// <returns>アルファマップのデータ</returns>
    public static List<AlphamapsData> GetAlphamapsDataListFromRay(TerrainData terrainData, RaycastHit hitInfo, int sizeX = 1, int sizeY = 1)
    {
        List<AlphamapsData> alphamapsDataList = new();

        // 読み込む座標
        int posX = Mathf.FloorToInt(hitInfo.textureCoord.x * terrainData.alphamapWidth);
        int posY = Mathf.FloorToInt(hitInfo.textureCoord.y * terrainData.alphamapHeight);

        // アルファマップを取得
        var alphamaps = terrainData.GetAlphamaps(posX, posY, sizeX, sizeY);
        // アルファマップの数
        int numMaps = terrainData.alphamapLayers;

        for (int i = 0; i < numMaps; i++)
        {
            // ペイントの割合(合計1)を確認し 塗られているレイヤーのみ追加
            float element = alphamaps[0, 0, i];
            if (element <= 0) continue;

            // アルファマップの必要なデータを抽出し リストに追加
            var alphamapsData = new AlphamapsData
            {
                layerName = terrainData.terrainLayers[i].name,
                element = element
            };

            alphamapsDataList?.Add(alphamapsData);
        }
        return alphamapsDataList;
    }

    // TODO: 割合が一番多いレイヤーを返す

}
