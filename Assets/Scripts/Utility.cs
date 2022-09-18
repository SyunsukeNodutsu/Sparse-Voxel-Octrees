//-----------------------------------------------------------------------------
// File: Utility.cs
//
// �֗��@�\�܂Ƃ�
//-----------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>�֗��@�\�܂Ƃ߃N���X</summary>
public static class Utility
{
    /// <summary>float��min�`max�̊ԂŐ��K��(���`�⊮)</summary>
    /// <param name="val">�ݒ�Ώۂ̒l</param>
    /// <param name="min">�ŏ��l</param>
    /// <param name="max">�ő�l</param>
    /// <returns></returns>
    public static float GetNormalizeVal(float val, float min, float max)
    {
        //val = Mathf.Clamp(val, min, max);
        if (val > 0) return (val - min) / (max - min);
        else if (val < 0) return (val - min) / (max - min);
        return 0;
    }

    /// <summary>�ő�l��"1"�ŏ��l��"0"�ɐ��K�����Ԃ�</summary>
    /// <param name="t">���K������l</param>
    /// /// <returns>���K�����ꂽ����</returns>
    public static float GetZeroOneLinear(float min, float max, float t) { return (t - min) / (max - min); }

    /// <summary>"min"�`"max"�̒l��"newMin"�`"newMax"�̒l�ɐ��K�����Ԃ�</summary>
    /// <param name="t">���K������l</param>
    /// <returns>���K�����ꂽ����</returns>
    public static float GetZeroOneLinearEx(float min, float max, float newMin, float newMax, float t) { return (t - min) / (max - min) * (newMax - newMin) + newMin; }

    /// <summary>XZ���ʂł̋�����Ԃ�</summary>
    /// <param name="a">���WA</param>
    /// <param name="b">���WB</param>
    /// <returns>����</returns>
    public static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float num = a.x - b.x;
        float num2 = a.z - b.z;
        return Mathf.Sqrt(num * num + num2 * num2);
    }

    /// <summary>Image�̕s�����x��ݒ肷��</summary>
    /// <param name="image">�ݒ�Ώۂ�Image�R���|�[�l���g</param>
    /// <param name="alpha">�s�����x(0�`1)</param>
    public static void SetOpacity(Image image, float alpha)
    {
        var c = image.color;
        image.color = new Color(c.r, c.g, c.b, alpha);
    }

    /// <summary>�J����(main)�����ʒ�����Ray���΂��Փˍ��W���i�[</summary>
    /// <param name="ignoreDistance">Ray�𖳎�����J��������̋���</param>
    /// <param name="retPos">�i�[�����Փˍ��W</param>
    /// <returns>�Փ˂������ǂ���</returns>
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

            //��ԋ߂�Object��D��
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

    /// <summary>���ʃt�F�[�h��ݒ�</summary>
    /// <note>�����I��SetVolume���ǂ������Ă���H ->�������ω��ʂ͔�΂��Ȃ��Əd���Ȃ�</note>
    /// <param name="targetVolume">�ڕW����</param>
    /// <param name="fadeTime">�t�F�[�h�ɂ����鎞��</param>
    public static IEnumerator SetFade(float targetVolume, float fadeTime)
    {
        float startVolume = AudioListener.volume;
        float timeCount = 0.0f;

        //���Ԃ��Ƃ̕ω���
        float fadeVolume = (targetVolume - startVolume) / fadeTime;

        while (timeCount <= fadeTime)
        {
            AudioListener.volume = startVolume + (fadeVolume * timeCount);
            timeCount += Time.unscaledDeltaTime;
            yield return null;
        }

        AudioListener.volume = targetVolume;
    }

    /// <summary>���ʃt�F�[�h��ݒ�</summary>
    /// <note>�����I��SetVolume���ǂ������Ă���H ->�������ω��ʂ͔�΂��Ȃ��Əd���Ȃ�</note>
    /// <param name="source">�t�F�[�h������I�[�f�B�I�\�[�X</param>
    /// <param name="targetVolume">�ڕW����</param>
    /// <param name="fadeTime">�t�F�[�h�ɂ����鎞��</param>
    public static IEnumerator SetFade(AudioSource source, float targetVolume, float fadeTime)
    {
        float startVolume = source.volume;
        float timeCount = 0.0f;

        //���Ԃ��Ƃ̕ω���
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
    /// <summary>UnityEditor �R���\�[�����O�̃N���A</summary>
    /// <note>����.Release�r���h�̍ۂ͏��O����K�v������</note>
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

/// <summary>�e���C���֘A�֗̕��@�\</summary>
public static class TerrainUtility
{
    /// <summary>�A���t�@�}�b�v�̃f�[�^</summary>
    public class AlphamapsData
    {
        /// <summary>�e�N�X�`�����C���[�̖��O</summary>
        public string layerName;
        /// <summary>�y�C���g�̊���</summary>
        public float element;
    }

    /// <summary>TerrainData����A���t�@�}�b�v�f�[�^���擾���Ԃ�</summary>
    /// <param name="terrainData">��͂���e���C���f�[�^</param>
    /// <param name="hitInfo">���C�̌��ʏ��</param>
    /// <param name="sizeX">�擾���镝 (Default: 1)</param>
    /// <param name="sizeY">�擾���鍂�� (Default: 1)</param>
    /// <returns>�A���t�@�}�b�v�̃f�[�^</returns>
    public static List<AlphamapsData> GetAlphamapsDataListFromRay(TerrainData terrainData, RaycastHit hitInfo, int sizeX = 1, int sizeY = 1)
    {
        List<AlphamapsData> alphamapsDataList = new();

        //�ǂݍ��ލ��W
        int posX = Mathf.FloorToInt(hitInfo.textureCoord.x * terrainData.alphamapWidth);
        int posY = Mathf.FloorToInt(hitInfo.textureCoord.y * terrainData.alphamapHeight);

        //�A���t�@�}�b�v���擾
        var alphamaps = terrainData.GetAlphamaps(posX, posY, sizeX, sizeY);
        //�A���t�@�}�b�v�̐�
        int numMaps = terrainData.alphamapLayers;

        for (int i = 0; i < numMaps; i++)
        {
            //�y�C���g�̊���(���v1)���m�F�� �h���Ă��郌�C���[�̂ݒǉ�
            float element = alphamaps[0, 0, i];
            if (element <= 0) continue;

            //�A���t�@�}�b�v�̕K�v�ȃf�[�^�𒊏o�� ���X�g�ɒǉ�
            var alphamapsData = new AlphamapsData
            {
                layerName = terrainData.terrainLayers[i].name,
                element = element
            };

            alphamapsDataList?.Add(alphamapsData);
        }
        return alphamapsDataList;
    }

    //TODO: ��������ԑ������C���[��Ԃ�

}
