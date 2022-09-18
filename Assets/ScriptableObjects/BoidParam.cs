//-----------------------------------------------------------------------------
// File: BoidParam.cs
//
// Boidパラメータ
//-----------------------------------------------------------------------------
using UnityEngine;

[CreateAssetMenu(menuName = "Boid/Param")]
public class BoidParam : ScriptableObject
{
    public float initSpeed = 2.0f;
    public float minSpeed = 2.0f;
    public float maxSpeed = 5.0f;
    public float neighborDistance = 1.0f;   // 群れを形成しようとする距離
    public float neighborFov = 90.0f;       // 群れを形成しようとする角度
    public float wallScale = 5.0f;
    public float wallDistance = 3.0f;
    public float wallWeight = 1.0f;
    public float separationWeight = 5.0f;   // 分離
    public float alignmentWeight = 2.0f;    // 整列
    public float cohesionWeight = 3.0f;     // 結合
}
