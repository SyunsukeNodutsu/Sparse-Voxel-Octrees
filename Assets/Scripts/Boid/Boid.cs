﻿//-----------------------------------------------------------------------------
// File: Boid.cs
//
// Boid単位
//-----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

/// <summary>Boid単位</summary>
public class Boid : MonoBehaviour
{
    public BoidSystem BoidSystem { get; set; }
    public BoidParam BoidParam { get; set; }

    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }

    private Vector3 m_accel = Vector3.zero;
    private readonly List<Boid> m_neighborList = new();

    private void Start()
    {
        Position = transform.position;
        Velocity = transform.forward * BoidParam.initSpeed;
    }

    private void Update()
    {
        UpdateNeighbors();
        UpdateWalls();
        UpdateAccel();
        UpdateMove();
    }

    /// <summary>求めたアクセル値をもとに移動</summary>
    private void UpdateMove()
    {
        Velocity += m_accel * Time.deltaTime;

        var dir = Velocity.normalized;
        var speed = Velocity.magnitude;
        Velocity = Mathf.Clamp(speed, BoidParam.minSpeed, BoidParam.maxSpeed) * dir;

        Position += Velocity * Time.deltaTime;

        var rot = Quaternion.LookRotation(Velocity);
        transform.SetPositionAndRotation(Position, rot);

        m_accel = Vector3.zero;
    }

    /// <summary>壁に反発</summary>
    private void UpdateWalls()
    {
        if (!BoidSystem) return;

        Vector3 CalcAccelAgainstWall(float distance, Vector3 dir)
        {
            if (distance < BoidParam.wallDistance)
                return dir * (BoidParam.wallWeight / Mathf.Abs(distance / BoidParam.wallDistance));
            return Vector3.zero;
        }

        var scale = BoidParam.wallScale * 0.5f;
        m_accel +=
            CalcAccelAgainstWall(-scale - Position.x, Vector3.right) +
            CalcAccelAgainstWall(-scale - Position.y, Vector3.up) +
            CalcAccelAgainstWall(-scale - Position.z, Vector3.forward) +
            CalcAccelAgainstWall(+scale - Position.x, Vector3.left) +
            CalcAccelAgainstWall(+scale - Position.y, Vector3.down) +
            CalcAccelAgainstWall(+scale - Position.z, Vector3.back);
    }

    /// <summary>群れの形成</summary>
    void UpdateNeighbors()
    {
        m_neighborList.Clear();

        if (!BoidSystem) return;

        var prodThresh = Mathf.Cos(BoidParam.neighborFov * Mathf.Deg2Rad);
        var distThresh = BoidParam.neighborDistance;

        foreach (var other in BoidSystem.BoidList)
        {
            if (other == this) continue;

            var to = other.Position - Position;
            var dist = to.magnitude;
            if (dist < distThresh)
            {
                var dir = to.normalized;
                var fwd = Velocity.normalized;
                var prod = Vector3.Dot(fwd, dir);
                if (prod > prodThresh)
                    m_neighborList.Add(other);
            }
        }
    }

    /// <summary>分離.整列.結合の3要素を計算</summary>
    void UpdateAccel()
    {
        if (m_neighborList.Count == 0) return;

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;
        foreach (var neighbor in m_neighborList)
        {
            separation += (Position - neighbor.Position).normalized;
            alignment += neighbor.Velocity;
            cohesion += neighbor.Position;
        }

        // 平均値計算
        separation /= m_neighborList.Count;
        alignment /= m_neighborList.Count;
        cohesion /= m_neighborList.Count;

        // アクセル値計算
        m_accel += separation * BoidParam.separationWeight;
        m_accel += (alignment - Velocity) * BoidParam.alignmentWeight;
        m_accel += (cohesion - Position) * BoidParam.cohesionWeight;
    }

}
