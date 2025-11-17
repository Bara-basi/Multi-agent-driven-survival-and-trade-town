using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using System.Collections;
using System;
using UnityEngine.InputSystem.Controls;

public class ClickToMove2D : MonoBehaviour
{
    [Header("References")]
    public Camera cam;                 
    public Grid grid;                
    public Tilemap groundTilemap;     
    public Tilemap obstacleTilemap; 
    public PlayerController player;    
    private float suspendTimer = 0f;

    [Header("Obstacle Physics Check")]
    public Collider2D playerCollider;   
    public LayerMask obstacleMask;
    [Range(0f, 0.2f)]
    public float extraClearance = 0.02f;  // 给探测盒再收一点边，避免贴墙

    [Header("Path Following")]
    public float arriveCellEpsilon = 0.2f;   // 认为到达格中心的距离
    public float repathIfBlockedAfterSec = 0.5f;

    private readonly List<Vector3Int> pathCells = new();
    private int pathIndex = 0;
    private float stuckTimer = 0f;
    private Vector3 lastPos;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        lastPos = transform.position;
        if (player != null) player.ManualControlStarted += CancelAuto;
    }


    void Update()
    {
        // --- 1) 键盘抢占：有人为输入就立刻丢弃自动寻路 ---
        if (player != null && player.IsManualControlling)
        {
            CancelAuto();
            return; 
        }
        if(suspendTimer > 0f)
        {
            suspendTimer -= Time.deltaTime;
            CancelAuto();
            return;
        }
        // --- 2) 鼠标点击发起寻路 ---
        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            Vector3 world = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            world.z = 0f;

            Vector3Int startCell = grid.WorldToCell(transform.position);
            Vector3Int targetCell = grid.WorldToCell(world);

            if (!IsWalkable(targetCell))
            {
                if (!FindNearestWalkable(ref targetCell, 8))
                {
                    CancelAuto();
                    return;
                }
            }

            var newPath = AStar(startCell, targetCell);
            if (newPath != null && newPath.Count > 0)
            {
                pathCells.Clear();
                pathCells.AddRange(newPath);
                pathIndex = 0;
                AdvanceTowardsCurrentNode();
            }
            else
            {
                CancelAuto();
            }
        }

        // --- 3) 自动沿路行走 ---
        if (pathCells.Count > 0)
        {
            Vector3 targetWorld = grid.GetCellCenterWorld(pathCells[pathIndex]);
            targetWorld.z = 0f;
            Vector2 dir = (targetWorld - transform.position);

            if (dir.magnitude <= arriveCellEpsilon)
            {
                pathIndex++;
                if (pathIndex >= pathCells.Count)
                {
                    CancelAuto(); // 终点
                    return;
                }
                else
                {
                    AdvanceTowardsCurrentNode();
                }
            }
            else
            {
                player.SetAutoMove(dir.normalized);
            }

            // 简单卡住检测：基本没位移则重算
            float moved = (transform.position - lastPos).sqrMagnitude;
            lastPos = transform.position;
            if (moved < 0.0001f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer > repathIfBlockedAfterSec)
                {
                    RepathFromHere();
                    stuckTimer = 0f;
                }
            }
            else stuckTimer = 0f;
        }
        else
        {
            player.SetAutoMove(Vector2.zero);
        }
    }

    public void CancelAuto()
    {
        pathCells.Clear();
        pathIndex = 0;
        player.SetAutoMove(Vector2.zero);
    }

    public void Suspend(float seconds)
    {
        //短暂屏蔽自动寻路
        CancelAuto();
        suspendTimer = Mathf.Max(suspendTimer,seconds);
    }
    void AdvanceTowardsCurrentNode()
    {
        if (pathCells.Count == 0) { player.SetAutoMove(Vector2.zero); return; }
        Vector3 targetWorld = grid.GetCellCenterWorld(pathCells[pathIndex]);
        Vector2 dir = (targetWorld - transform.position);
        player.SetAutoMove(dir.normalized);
    }

    void RepathFromHere()
    {
        if (pathCells.Count == 0) return;
        Vector3Int cur = grid.WorldToCell(transform.position);
        Vector3Int goal = pathCells[pathCells.Count - 1];
        var newPath = AStar(cur, goal);
        if (newPath != null && newPath.Count > 0)
        {
            pathCells.Clear();
            pathCells.AddRange(newPath);
            pathIndex = 0;
        }
        else
        {
            CancelAuto();
        }
    }

    // ---------- 可走性：Tile + 物理碰撞双重判定 ----------
    bool IsWalkable(Vector3Int cell)
    {
        // 只允许走在地板瓦上
        //if (groundTilemap && !groundTilemap.HasTile(cell)) return false;

        // 目标格的世界中心
        Vector3 center = grid.GetCellCenterWorld(cell);

        // 用“玩家碰撞盒的世界尺寸 - 一点余量”当探测尺寸
        Vector2 probeSize;
        if (playerCollider != null)
        {
            var sz = playerCollider.bounds.size;
            probeSize = new Vector2(
                Mathf.Max(0.01f, sz.x - 2f * extraClearance),
                Mathf.Max(0.01f, sz.y - 2f * extraClearance)
            );
        }
        else
        {
            // 兜底：用格子大小 - 余量
            probeSize = new Vector2(
                Mathf.Max(0.01f, Mathf.Abs(grid.cellSize.x) - 2f * extraClearance),
                Mathf.Max(0.01f, Mathf.Abs(grid.cellSize.y) - 2f * extraClearance)
            );
        }

        // 1) 瓦片障碍（如果你把墙都画在 Tilemap_Collision 上）
        if (obstacleTilemap && obstacleTilemap.HasTile(cell)) return false;

        // 2) 物理障碍（TilemapCollider2D/CompositeCollider2D 也在 Obstacles 层）
        //    只查 obstacleMask，避免撞到自己或其他层
        if (Physics2D.OverlapBox(center, probeSize, 0f, obstacleMask) != null) return false;

        return true;
    }

    // 在目标周围找最近可走格
    bool FindNearestWalkable(ref Vector3Int cell, int maxRadius)
    {
        if (IsWalkable(cell)) return true;

        Queue<Vector3Int> q = new();
        HashSet<Vector3Int> vis = new() { cell };
        q.Enqueue(cell);

        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                var n = cur + d;
                if (!vis.Add(n)) continue;

                int r = Mathf.Abs(n.x - cell.x) + Mathf.Abs(n.y - cell.y);
                if (r > maxRadius) continue;

                if (IsWalkable(n))
                {
                    cell = n;
                    return true;
                }
                q.Enqueue(n);
            }
        }
        return false;
    }

    // ---------- 简易 A*（4邻接 / 曼哈顿启发） ----------
    List<Vector3Int> AStar(Vector3Int start, Vector3Int goal)
    {
        
        var open = new List<Vector3Int> { start };
        var came = new Dictionary<Vector3Int, Vector3Int>();
        var g = new Dictionary<Vector3Int, int> { [start] = 0 };
        var f = new Dictionary<Vector3Int, int> { [start] = Heu(start, goal) };

        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        while (open.Count > 0)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (f[open[i]] < f[open[best]]) best = i;

            var cur = open[best];
            if (cur == goal) return Reconstruct(came, cur);
            open.RemoveAt(best);

            foreach (var d in dirs)
            {
                var nx = cur + d;
                if (!IsWalkable(nx)) continue;

                int candG = g[cur] + 1;
                if (!g.ContainsKey(nx) || candG < g[nx])
                {
                    g[nx] = candG;
                    f[nx] = candG + Heu(nx, goal);
                    came[nx] = cur;
                    if (!open.Contains(nx)) open.Add(nx);
                }
            }
        }
        return null;
    }

    int Heu(Vector3Int a, Vector3Int b)
        => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    List<Vector3Int> Reconstruct(Dictionary<Vector3Int, Vector3Int> came, Vector3Int cur)
    {
        var list = new List<Vector3Int> { cur };
        while (came.ContainsKey(cur)) { cur = came[cur]; list.Add(cur); }
        list.Reverse();
        return list;
    }

    // 调试：可视化路径
    void OnDrawGizmos()
    {
        if (pathCells == null || pathCells.Count == 0 || grid == null) return;
        Gizmos.color = Color.cyan;
        Vector3 prev = transform.position;
        for (int i = pathIndex; i < pathCells.Count; i++)
        {
            Vector3 p = grid.GetCellCenterWorld(pathCells[i]);
            Gizmos.DrawSphere(p, 0.05f);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}
