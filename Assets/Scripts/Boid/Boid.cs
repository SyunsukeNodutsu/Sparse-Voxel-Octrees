//-----------------------------------------------------------------------------
// File: Boid.cs
//
// Boid単位
//-----------------------------------------------------------------------------
using Unity.VisualScripting;
using UnityEngine;

/// <summary>Boid単位</summary>
public class Boid : MonoBehaviour
{
    public BoidSystem BoidSystem { get; set; }
    public LinearTreeManager<GameObject> LinearTreeManager { get; set; }
    public BoidParam BoidParam { get; set; }

    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }

    private Vector3 m_accel = Vector3.zero;

    private void Start()
    {
        Position = transform.position;
        Velocity = transform.forward * BoidParam.initSpeed;
    }

    private void Update()
    {
        // カメラから離れすぎていれば処理をしない
        if (OverStopDistance()) return;

        UpdateWalls();
        UpdateNeighbors();
        UpdateMove();
    }

    /// <summary>求めたアクセル値をもとに移動</summary>
    private void UpdateMove()
    {
        Velocity += m_accel * Time.deltaTime;

        // X回転(Y移動量)制限
        {
            //var newY = Mathf.Clamp(Velocity.y, -BoidParam.clampVelocityY, BoidParam.clampVelocityY);
            //Velocity = new Vector3(Velocity.x, newY, Velocity.z);
        }

        var dir = Velocity.normalized;
        var speed = Velocity.magnitude;
        Velocity = Mathf.Clamp(speed, BoidParam.minSpeed, BoidParam.maxSpeed) * dir;

        Position += Velocity * Time.deltaTime;

        // BOX外の場合は補正
        // ここの処理が入った場合、ENVの方にパラメータを見直してもらいたいのでLogを出す
        if (true)
        {
            var distX = Mathf.Abs(BoidSystem.transform.position.x - Position.x);
            var distY = Mathf.Abs(BoidSystem.transform.position.y - Position.y);
            var distZ = Mathf.Abs(BoidSystem.transform.position.z - Position.z);

            if (distX >= BoidParam.returnDistance || distY >= BoidParam.returnDistance || distZ >= BoidParam.returnDistance)
            {
                Debug.Log("遊泳エリア外 復帰処理を行います パラメータの見直しを推奨.");
                var newDir = BoidSystem.transform.position - Position;
                Velocity = Mathf.Clamp(speed, BoidParam.minSpeed, BoidParam.maxSpeed) * newDir;
            }
        }

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
            if (Mathf.Abs(distance) < BoidParam.wallDistance)
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

    /// <summary>群れの形成分離.整列.結合の3要素を計算</summary>
    void UpdateNeighbors()
    {
        if (!BoidSystem) return;
        if (BoidSystem.BoidList.Count <= 0) return;

        Vector3 separation = Vector3.zero;
        Vector3 alignment = Vector3.zero;
        Vector3 cohesion = Vector3.zero;

        // 要素で分離してもいいかも 例) separationは視界の判定を行わない
        int numNeighbors = 0;

        // TODO: オーダー数
        foreach (var other in BoidSystem.BoidList)
        {
            if (other == this) continue;

            var to = other.Position - Position;
            var dist = to.magnitude;
            if (dist < BoidParam.neighborDistance)
            {
                var dir = to.normalized;
                var fwd = Velocity.normalized;
                var prod = Vector3.Dot(fwd, dir);
                if (prod > Mathf.Cos(BoidParam.neighborFov * Mathf.Deg2Rad))
                {
                    // 群れと判断
                    separation += (Position - other.Position).normalized;
                    alignment += other.Velocity;
                    cohesion += other.Position;

                    numNeighbors++;
                }
            }
        }

        // 単独の場合はアクセル値の計算を行わない
        // TODO: ネイバーリストが形成されない場合、壁との反発で更新されたVelocityが原因で
        // 異様に早くなってしまう ->単体の場合はVelocityに制限をかけたほうがいい？
        // ※仲間を探してるみたいでかわいいけど...
        if (numNeighbors <= 0) return;

        // アクセル値計算
        separation /= numNeighbors;
        m_accel += separation * BoidParam.separationWeight;

        alignment /= numNeighbors;
        m_accel += (alignment - Velocity) * BoidParam.alignmentWeight;

        cohesion /= numNeighbors;
        m_accel += (cohesion - Position) * BoidParam.cohesionWeight;
    }

    /// <summary>停止距離を過ぎているかを返す</summary>
    bool OverStopDistance()
    {
        var cameraPos = Camera.main.transform.position;
        var distance = Vector3.Distance(cameraPos, Position);
        return distance >= BoidParam.stopDistance;
    }

}
