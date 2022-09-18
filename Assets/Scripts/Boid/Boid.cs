//-----------------------------------------------------------------------------
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

    private Vector3 accel = Vector3.zero;
    private readonly List<Boid> neighborList = new();

    private void Start()
    {
        Position = transform.position;
        Velocity = transform.forward * BoidParam.initSpeed;
    }

    private void Update()
    {
        UpdateNeighbors();

        UpdateWalls();

        UpdateSeparation();
        UpdateAlignment();
        UpdateCohesion();

        UpdateMove();
    }

    /// <summary>求めたアクセル値をもとに移動</summary>
    private void UpdateMove()
    {
        var dt = Time.deltaTime;

        Velocity += accel * dt;
        var dir = Velocity.normalized;
        var speed = Velocity.magnitude;
        Velocity = Mathf.Clamp(speed, BoidParam.minSpeed, BoidParam.maxSpeed) * dir;
        Position += Velocity * dt;

        var rot = Quaternion.LookRotation(Velocity);
        transform.SetPositionAndRotation(Position, rot);

        accel = Vector3.zero;
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
        accel +=
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
        neighborList.Clear();

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
                    neighborList.Add(other);
            }
        }
    }

    /// <summary>分離</summary>
    void UpdateSeparation()
    {
        if (neighborList.Count == 0) return;

        Vector3 force = Vector3.zero;
        foreach (var neighbor in neighborList)
        {
            force += (Position - neighbor.Position).normalized;
        }
        force /= neighborList.Count;

        accel += force * BoidParam.separationWeight;
    }

    /// <summary>整列</summary>
    void UpdateAlignment()
    {
        if (neighborList.Count == 0) return;

        var averageVelocity = Vector3.zero;
        foreach (var neighbor in neighborList)
        {
            averageVelocity += neighbor.Velocity;
        }
        averageVelocity /= neighborList.Count;

        accel += (averageVelocity - Velocity) * BoidParam.alignmentWeight;
    }

    /// <summary>結合</summary>
    void UpdateCohesion()
    {
        if (neighborList.Count == 0) return;

        var averagePos = Vector3.zero;
        foreach (var neighbor in neighborList)
        {
            averagePos += neighbor.Position;
        }
        averagePos /= neighborList.Count;

        accel += (averagePos - Position) * BoidParam.cohesionWeight;
    }

}
