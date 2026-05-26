/*
驱动角色自动寻路的组件
*/

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class ActionPair
{
    public string cmd;
    public Vector2 target;
    public string targetKey;
    public int facingDirection;
    public Action actionCallBack;
    public float cost;
}

public class AutoMove : MonoBehaviour, IAutoNavigator,IPortalTraveller
{
    [Header("References")]
    public Camera cam;
    public CinemachineCamera vcam;
    public Grid grid;
    public Tilemap obstacleTilemap;
    public PlayerHUD hud;

    [SerializeField] 
    private float speed = 10f;
    private float suspendTimer = 0f;
    private float res_time = 0f;

    private Vector2 autoMove = Vector2.zero;
    private readonly Queue<ActionPair> actionList = new();
    private Action currentCallback;

    private Animator ani;
    private Rigidbody2D rb;
    private Vector2 lastFacing = Vector2.down;
    private bool frozen = false;
    private bool isTeleporting = false; 
    private float frozenUntil = 0f;
    private string curCmd = "";
    private int currentFacingDirection = 0;
    private bool idleMoving = false;
    private Vector3Int idleTargetCell;
    private float nextIdleMoveAt = 0f;
    private bool hasStepReservation = false;
    private Vector3Int reservedStepCell;
    private float reservationWaitTimer = 0f;
    private float agentBlockedByAgentTimer = 0f;
   
    [Header("Idle Wander")]
    public bool enableIdleWander = true;
    public float idleMoveIntervalMin = 2f;
    public float idleMoveIntervalMax = 5f;

    [Header("Agent Avoidance")]
    [Min(0)]
    public int agentAvoidanceCellPadding = 1;
    [Min(1)]
    public int queueCellSpacing = 2;
    public float agentPassThroughAfterBlockedSeconds = 1.2f;
    public float agentPassThroughDuration = 2.5f;
    private float agentPassThroughUntil = 0f;

    [Header("Path Following")]
    public float arriveCellEpsilon = 0.2f;
    public float repathIfBlockedAfterSec = 0.5f;
    public float hardStuckAfterSec = 2f;
    public int hardStuckSnapRadius = 2;
    [Min(256)]
    public int maxPathSearchNodes = 4096;
    [Min(16)]
    public int maxPathSearchDistance = 96;
    private readonly List<Vector3Int> pathCells = new();
    private int pathIndex = 0;
    private float stuckTimer = 0f;
    private float hardStuckTimer = 0f;
    private Vector3 lastPos;
    private Vector3Int currentGoalCell;
    private bool hasGoal = false;
    private string currentTargetKey = "";
    private bool currentCommandUsesQueue = false;
    private string currentQueueKey = "";
    private Vector3Int currentFinalTargetCell;
    private Vector3Int currentMoveDestinationCell;
    private bool waitingAtQueueSlot = false;
    private int lastQueueRank = -2;
    private const string CheckoutTargetKey = "收银台";

    [Header("Obstacle Physics Check")]
    public Collider2D playerCollider;
    public LayerMask obstacleMask;
    [Range(0f, 0.2f)]
    public float extraClearance = 0.02f;

    

    void Awake()
    {
        ani = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (!cam) cam = Camera.main;
        if (ani) ani.SetInteger("move", 0);
        lastPos = transform.position;
        if (grid != null)
            AgentCrowdCoordinator.SyncCell(this, grid.WorldToCell(transform.position));
        ScheduleNextIdleMove();
    }

    void OnDisable()
    {
        AgentCrowdCoordinator.Unregister(this);
    }

    void OnEnable()
    {
        if (grid != null)
            AgentCrowdCoordinator.SyncCell(this, grid.WorldToCell(transform.position));
    }

    void FixedUpdate()
    {
        if (frozen)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = autoMove * speed;
    }

    void Update()
    {

        if (frozen && Time.time >= frozenUntil && !isTeleporting)
            frozen = false;

        if (frozen)
        {
            if (ani) ani.SetInteger("move", 0);
            autoMove = Vector2.zero;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }
        if (suspendTimer > 0f)
        {
            suspendTimer -= Time.deltaTime;
            CancelAuto();
            return;
        }
        // 驱动动画
        if (autoMove.sqrMagnitude > 0.0001f)
        {
            if (ani)
            {
                ani.SetInteger("move", 1);
                if (Mathf.Abs(autoMove.x) >= Mathf.Abs(autoMove.y))
                {
                    ani.SetFloat("Horizontal", Mathf.Sign(autoMove.x));
                    ani.SetFloat("Vertical", 0f);
                    lastFacing = new Vector2(Mathf.Sign(autoMove.x), 0f);
                }
                else
                {
                    ani.SetFloat("Horizontal", 0f);
                    ani.SetFloat("Vertical", Mathf.Sign(autoMove.y));
                    lastFacing = new Vector2(0f, Mathf.Sign(autoMove.y));
                }
            }
        }
        else
        {
            if (ani)
            {
                ani.SetFloat("Horizontal", lastFacing.x);
                ani.SetFloat("Vertical", lastFacing.y);
                ani.SetInteger("move", 0);
            }
        }

        // 若空闲且有新命令，取队头开始寻路
        if (curCmd == "" && actionList.Count > 0)
        {
            StopIdleWander();
            var pair = actionList.Dequeue();
            currentCallback = pair.actionCallBack;
            curCmd = pair.cmd;
            currentTargetKey = pair.targetKey ?? "";
            currentFacingDirection = pair.facingDirection;
            if (curCmd == "go_to")
            {
                var startCell = grid.WorldToCell(transform.position);
                var targetCell = grid.WorldToCell((Vector3)pair.target);
                AgentCrowdCoordinator.SyncCell(this, startCell);

                if (!IsWalkable(targetCell))
                {
                    if (!FindNearestWalkable(ref targetCell, 8))
                    {
                        CancelAuto();
                        CompleteCurrent();
                        return;
                    }
                }

                currentCommandUsesQueue = IsQueueTarget(currentTargetKey);
                currentQueueKey = currentCommandUsesQueue ? currentTargetKey : "";
                if (currentCommandUsesQueue && currentFacingDirection == 0)
                    currentFacingDirection = 4;
                currentFinalTargetCell = targetCell;
                waitingAtQueueSlot = false;
                lastQueueRank = -2;
                if (currentCommandUsesQueue)
                {
                    AgentCrowdCoordinator.JoinQueue(this, currentQueueKey);
                    targetCell = ResolveQueueDestinationCell();
                }
                currentMoveDestinationCell = targetCell;
                hasGoal = true;
                currentGoalCell = targetCell;

                var path = AStar(startCell, targetCell, !CanPassThroughAgents());
                if ((path == null || path.Count == 0) && TryEnableAgentPassThrough(startCell, targetCell))
                    return;
                if (path != null && path.Count > 0)
                {
                    pathCells.Clear();
                    pathCells.AddRange(path);
                    pathIndex = 0;
                    reservationWaitTimer = 0f;
                    stuckTimer = 0f;
                    lastPos = transform.position;
                    NextStep();
                }
                else
                {
                    CancelAuto();
                    CompleteCurrent();
                }
            }else if(curCmd == "waiting")
            {
                //等待或者工作中
                res_time = pair.cost;
                hud.StartWork(res_time);
                
            }else if(pair.cmd == "sleeping")
            {
                res_time = pair.cost;
                //播放睡觉动画
                ani.SetInteger("sleep", 1);

            }
            else if (pair.cmd == "pick_up")
            {
                ani.SetTrigger("pick_up");
                res_time = pair.cost;

            }
            else if (pair.cmd == "fishing")
            {
                
            }
            else
            {
                
                CancelAuto();
                CompleteCurrent();
                print("error:no such command");
            }

        }
        if (curCmd == "" && actionList.Count == 0 && enableIdleWander)
        {
            HandleIdleWander();
        }
        else if (!idleMoving)
        {
            autoMove = Vector2.zero;
        }

        if(curCmd == "go_to")
        {
            if (currentCommandUsesQueue && RefreshQueueDestinationIfNeeded())
                return;

            if (currentCommandUsesQueue && waitingAtQueueSlot && pathCells.Count == 0)
            {
                autoMove = Vector2.zero;
                return;
            }
            //向着某处走动
            if (pathCells.Count > 0)
            {
                var stepCell = pathCells[pathIndex];
                var currentCell = grid.WorldToCell(transform.position);
                if (stepCell != currentCell && !hasStepReservation && !CanPassThroughAgents())
                {
                    if (!AgentCrowdCoordinator.TryReserveStep(this, currentCell, stepCell, agentAvoidanceCellPadding))
                    {
                        autoMove = Vector2.zero;
                        reservationWaitTimer += Time.deltaTime;
                        agentBlockedByAgentTimer += Time.deltaTime;
                        if (agentBlockedByAgentTimer > agentPassThroughAfterBlockedSeconds && TryEnableAgentPassThrough(currentCell, currentGoalCell))
                            return;
                        if (reservationWaitTimer > repathIfBlockedAfterSec)
                        {
                            RepathFromHere();
                            reservationWaitTimer = 0f;
                        }
                        return;
                    }

                    hasStepReservation = true;
                    reservedStepCell = stepCell;
                    reservationWaitTimer = 0f;
                    agentBlockedByAgentTimer = 0f;
                }

                var targetWorld = grid.GetCellCenterWorld(stepCell);
                targetWorld.z = 0f;
                Vector2 dir = (Vector2)(targetWorld - transform.position);

                float snapDist = Mathf.Max(arriveCellEpsilon, speed * Time.fixedDeltaTime * 1.1f);
                if (dir.magnitude <= snapDist)
                {
                    if (rb != null) rb.position = targetWorld;
                    else transform.position = targetWorld;
                    AgentCrowdCoordinator.CommitStep(this, stepCell);
                    hasStepReservation = false;
                    agentBlockedByAgentTimer = 0f;
                    pathIndex++;
                    if (pathIndex >= pathCells.Count)
                    {
                        if (currentCommandUsesQueue && !AgentCrowdCoordinator.IsQueueHead(this, currentQueueKey))
                        {
                            ApplyFacingDirection(currentFacingDirection);
                            ClearPathOnly();
                            waitingAtQueueSlot = true;
                            return;
                        }

                        // 到达终点
                        frozen = true;
                        frozenUntil = Time.time + 3;
                        ApplyFacingDirection(currentFacingDirection);
                        CancelAuto();
                        CompleteCurrent();
                        return;
                    }
                    else
                    {
                        NextStep();
                    }
                }
                else
                {
                    autoMove = dir.normalized;
                }

                float moved = (transform.position - lastPos).sqrMagnitude;
                lastPos = transform.position;
                if (moved < 0.0001f)
                {
                    stuckTimer += Time.deltaTime;
                    hardStuckTimer += Time.deltaTime;
                    if (stuckTimer > repathIfBlockedAfterSec)
                    {
                        RepathFromHere();
                        stuckTimer = 0f;
                    }
                    if (hardStuckTimer > hardStuckAfterSec)
                    {
                        var curCell = grid.WorldToCell(transform.position);
                        if (FindNearestWalkable(ref curCell, hardStuckSnapRadius))
                        {
                            var snap = grid.GetCellCenterWorld(curCell);
                            snap.z = 0f;
                            if (rb != null) rb.position = snap;
                            else transform.position = snap;
                            AgentCrowdCoordinator.SyncCell(this, curCell);
                            RepathFromHere();
                        }
                        else
                        {
                            CancelAuto();
                            CompleteCurrent();
                            return;
                        }
                        hardStuckTimer = 0f;
                    }
                }
                else
                {
                    stuckTimer = 0f;
                    hardStuckTimer = 0f;
                }
            }
            else
            {
                CancelAuto();
                CompleteCurrent();
            }
        }else if(curCmd == "waiting")
        {
            //原地等待或等待工作完成
            res_time -= Time.deltaTime;
            if(res_time <= 0)
            {
                hud.StopWork();
                CompleteCurrent();
            }

        }else if (curCmd == "sleeping")
        {
            //睡觉动画
            res_time -= Time.deltaTime;
            if (res_time <= 0)
            {
                print("stop");
                ani.SetInteger("sleep", 0);
                CompleteCurrent();
            }
        }else if (curCmd == "pick_up")
        {
            //捡东西
            res_time -= Time.deltaTime;
            if (res_time <= 0)
            {
                print("stop");
                CompleteCurrent();
            }
        }


    }

    public void AddCommand(float cost_time,string cmd, List<Vector3> target, Action onArrived, string targetKey = null)
    {
        //Vector3Int startCell = grid.WorldToCell(transform.position);
        //Vector2 temp_target = new Vector2(startCell.x+10f, startCell.y+10f);
        
        for (int i = 0; i < target.Count - 1; i++)
        {
            var waypoint = target[i];
            actionList.Enqueue(new ActionPair { cost =  cost_time,cmd= cmd,target = new Vector2(waypoint.x, waypoint.y), targetKey = "", facingDirection = 0, actionCallBack = null});
        }
        if(target.Count > 0)
        {
            var finalTarget = target[^1];
            actionList.Enqueue(new ActionPair { cost = cost_time, cmd = cmd, target = new Vector2(finalTarget.x, finalTarget.y), targetKey = targetKey ?? "", facingDirection = Mathf.RoundToInt(finalTarget.z), actionCallBack = onArrived });
        }
        else
        {
            actionList.Enqueue(new ActionPair { cost = cost_time, cmd = cmd, target = Vector2.zero, targetKey = targetKey ?? "", facingDirection = 0, actionCallBack = onArrived });
        }
    }
    public void Suspend(float seconds)
    {
        //短暂屏蔽自动寻路
        CancelAuto();
        //传送必定完成某次移动，返回成功响应
        CompleteCurrent();
        suspendTimer = Mathf.Max(suspendTimer, seconds);
    }
    //public void SetFrozen(bool frozen)
    //{
    //    this.frozen = frozen;
    //}
    public void CancelAuto()
    {
        AgentCrowdCoordinator.ReleaseStepReservation(this);
        hasStepReservation = false;
        pathCells.Clear();
        pathIndex = 0;
        autoMove = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        idleMoving = false;
        stuckTimer = 0f;
        hardStuckTimer = 0f;
        reservationWaitTimer = 0f;
        agentBlockedByAgentTimer = 0f;
        waitingAtQueueSlot = false;
        hasGoal = false;
    }

    void CompleteCurrent()
    {
        var cb = currentCallback;
        currentCallback = null;
        ReleaseCurrentQueue();
        curCmd = "";
        currentFacingDirection = 0;
        currentTargetKey = "";
        currentCommandUsesQueue = false;
        currentQueueKey = "";
        lastQueueRank = -2;
        cb?.Invoke();
    }

    void ClearPathOnly()
    {
        AgentCrowdCoordinator.ReleaseStepReservation(this);
        hasStepReservation = false;
        pathCells.Clear();
        pathIndex = 0;
        autoMove = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        stuckTimer = 0f;
        hardStuckTimer = 0f;
        reservationWaitTimer = 0f;
        agentBlockedByAgentTimer = 0f;
    }

    void ReleaseCurrentQueue()
    {
        if (!currentCommandUsesQueue) return;
        AgentCrowdCoordinator.LeaveQueue(this);
    }

    bool IsQueueTarget(string targetKey)
    {
        return string.Equals(targetKey, CheckoutTargetKey, StringComparison.Ordinal);
    }

    bool CanPassThroughAgents()
    {
        return !currentCommandUsesQueue && Time.time < agentPassThroughUntil;
    }

    bool TryEnableAgentPassThrough(Vector3Int startCell, Vector3Int goalCell)
    {
        if (currentCommandUsesQueue) return false;

        var staticPath = AStar(startCell, goalCell, false);
        if (staticPath == null || staticPath.Count == 0)
            return false;

        agentPassThroughUntil = Time.time + Mathf.Max(0.1f, agentPassThroughDuration);
        AgentCrowdCoordinator.ReleaseStepReservation(this);
        hasStepReservation = false;
        pathCells.Clear();
        pathCells.AddRange(staticPath);
        pathIndex = 0;
        reservationWaitTimer = 0f;
        agentBlockedByAgentTimer = 0f;
        stuckTimer = 0f;
        hardStuckTimer = 0f;
        hasGoal = true;
        currentGoalCell = goalCell;
        NextStep();
        return true;
    }

    Vector3Int ResolveQueueDestinationCell()
    {
        int rank = AgentCrowdCoordinator.GetQueueRank(this, currentQueueKey);
        lastQueueRank = rank;

        if (rank <= 0)
            return currentFinalTargetCell;

        return FindQueueWaitCell(rank);
    }

    bool RefreshQueueDestinationIfNeeded()
    {
        int rank = AgentCrowdCoordinator.GetQueueRank(this, currentQueueKey);
        if (rank < 0)
        {
            AgentCrowdCoordinator.JoinQueue(this, currentQueueKey);
            rank = AgentCrowdCoordinator.GetQueueRank(this, currentQueueKey);
        }

        var desiredCell = rank <= 0 ? currentFinalTargetCell : FindQueueWaitCell(rank);
        if (desiredCell == currentMoveDestinationCell && rank == lastQueueRank)
            return false;

        currentMoveDestinationCell = desiredCell;
        currentGoalCell = desiredCell;
        lastQueueRank = rank;
        waitingAtQueueSlot = false;
        RepathFromHere();
        return true;
    }

    Vector3Int FindQueueWaitCell(int queueRank)
    {
        int spacing = Mathf.Max(1, queueCellSpacing);
        int targetDistance = Mathf.Max(1, queueRank) * spacing;
        var behind = -FacingToCellDirection(currentFacingDirection);
        if (behind == Vector3Int.zero)
            behind = Vector3Int.down;
        var side = new Vector3Int(-behind.y, behind.x, 0);

        for (int distance = targetDistance; distance <= 12; distance++)
        {
            int[] sideOffsets = { 0, 1, -1, 2, -2, 3, -3 };
            for (int i = 0; i < sideOffsets.Length; i++)
            {
                var cell = currentFinalTargetCell + behind * distance + side * sideOffsets[i];
                if (cell == currentFinalTargetCell) continue;
                if (IsWalkable(cell) && !AgentCrowdCoordinator.IsDynamicallyBlocked(cell, this, agentAvoidanceCellPadding))
                    return cell;
            }
        }

        return FindNearestQueueWaitCell(Mathf.Max(0, queueRank - 1));
    }

    Vector3Int FindNearestQueueWaitCell(int candidateIndex)
    {
        var q = new Queue<Vector3Int>();
        var visited = new HashSet<Vector3Int> { currentFinalTargetCell };
        q.Enqueue(currentFinalTargetCell);

        Vector3Int[] dirs = { Vector3Int.down, Vector3Int.right, Vector3Int.left, Vector3Int.up };
        var candidates = new List<Vector3Int>();

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                var n = cur + d;
                if (!visited.Add(n)) continue;

                int r = Mathf.Abs(n.x - currentFinalTargetCell.x) + Mathf.Abs(n.y - currentFinalTargetCell.y);
                if (r > 12) continue;

                if (IsWalkable(n) && !AgentCrowdCoordinator.IsDynamicallyBlocked(n, this, agentAvoidanceCellPadding))
                {
                    candidates.Add(n);
                    if (candidates.Count > candidateIndex)
                        return candidates[candidateIndex];
                }
                q.Enqueue(n);
            }
        }

        return grid.WorldToCell(transform.position);
    }

    Vector3Int FacingToCellDirection(int direction)
    {
        switch (direction)
        {
            case 1:
                return Vector3Int.up;
            case 2:
                return Vector3Int.right;
            case 3:
                return Vector3Int.down;
            case 4:
                return Vector3Int.left;
            default:
                return Vector3Int.zero;
        }
    }

    void ApplyFacingDirection(int direction)
    {
        Vector2 facing;
        switch (direction)
        {
            case 1:
                facing = Vector2.up;
                break;
            case 2:
                facing = Vector2.right;
                break;
            case 3:
                facing = Vector2.down;
                break;
            case 4:
                facing = Vector2.left;
                break;
            default:
                return;
        }

        lastFacing = facing;
        autoMove = Vector2.zero;
        if (ani)
        {
            ani.SetFloat("Horizontal", facing.x);
            ani.SetFloat("Vertical", facing.y);
            ani.SetInteger("move", 0);
        }
    }

    void RepathFromHere()
    {
        if (!hasGoal) return;

        AgentCrowdCoordinator.ReleaseStepReservation(this);
        hasStepReservation = false;
        var startCell = grid.WorldToCell(transform.position);
        var goalCell = currentGoalCell;
        if (!IsWalkable(goalCell))
        {
            if (!FindNearestWalkable(ref goalCell, 8)) return;
            currentGoalCell = goalCell;
        }

        var avoidAgents = !CanPassThroughAgents();
        var path = AStar(startCell, goalCell, avoidAgents);
        if ((path == null || path.Count == 0) && avoidAgents && TryEnableAgentPassThrough(startCell, goalCell))
            return;
        if (path != null && path.Count > 0)
        {
            pathCells.Clear();
            pathCells.AddRange(path);
            pathIndex = 0;
            reservationWaitTimer = 0f;
            NextStep();
        }
    }

    void NextStep()
    {
        if (pathCells.Count == 0)
        {
            autoMove = Vector2.zero;
            return;
        }
        var targetWorld = grid.GetCellCenterWorld(pathCells[pathIndex]);
        Vector2 dir = (Vector2)(targetWorld - transform.position);
        autoMove = dir.normalized;
    }

    void HandleIdleWander()
    {
        if (idleMoving)
        {
            var currentCell = grid.WorldToCell(transform.position);
            if (idleTargetCell != currentCell && !hasStepReservation)
            {
                if (!AgentCrowdCoordinator.TryReserveStep(this, currentCell, idleTargetCell, agentAvoidanceCellPadding))
                {
                    autoMove = Vector2.zero;
                    idleMoving = false;
                    ScheduleNextIdleMove();
                    return;
                }
                hasStepReservation = true;
                reservedStepCell = idleTargetCell;
            }

            var targetWorld = grid.GetCellCenterWorld(idleTargetCell);
            targetWorld.z = 0f;
            Vector2 dir = (Vector2)(targetWorld - transform.position);
            float snapDist = Mathf.Max(arriveCellEpsilon, speed * Time.fixedDeltaTime * 1.1f);

            if (dir.magnitude <= snapDist)
            {
                if (rb != null) rb.position = targetWorld;
                else transform.position = targetWorld;
                AgentCrowdCoordinator.CommitStep(this, idleTargetCell);
                hasStepReservation = false;
                idleMoving = false;
                autoMove = Vector2.zero;
                ScheduleNextIdleMove();
            }
            else
            {
                autoMove = dir.normalized;
            }
            return;
        }

        if (Time.time < nextIdleMoveAt)
        {
            autoMove = Vector2.zero;
            return;
        }

        TryStartIdleMove();
    }

    void TryStartIdleMove()
    {
        var currentCell = grid.WorldToCell(transform.position);
        var dirs = new[] { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };
        var walkableNeighbors = new List<Vector3Int>(4);

        foreach (var d in dirs)
        {
            var cell = currentCell + d;
            if (IsWalkable(cell) && !AgentCrowdCoordinator.IsDynamicallyBlocked(cell, this, agentAvoidanceCellPadding))
                walkableNeighbors.Add(cell);
        }

        if (walkableNeighbors.Count == 0)
        {
            autoMove = Vector2.zero;
            ScheduleNextIdleMove();
            return;
        }

        idleTargetCell = walkableNeighbors[UnityEngine.Random.Range(0, walkableNeighbors.Count)];
        idleMoving = true;
    }

    void StopIdleWander()
    {
        AgentCrowdCoordinator.ReleaseStepReservation(this);
        hasStepReservation = false;
        idleMoving = false;
        autoMove = Vector2.zero;
    }

    void ScheduleNextIdleMove()
    {
        float min = Mathf.Max(0f, idleMoveIntervalMin);
        float max = Mathf.Max(min, idleMoveIntervalMax);
        nextIdleMoveAt = Time.time + UnityEngine.Random.Range(min, max);
    }

    bool IsWalkable(Vector3Int cell)
    {
        var center = grid.GetCellCenterWorld(cell);

        Vector2 probeSize;
        if (playerCollider != null)
        {
            var sz = playerCollider.bounds.size;
            probeSize = new Vector2(
                Mathf.Max(0.01f, sz.x + 2f * extraClearance),
                Mathf.Max(0.01f, sz.y + 2f * extraClearance)
            );
        }
        else
        {
            probeSize = new Vector2(
                Mathf.Max(0.01f, Mathf.Abs(grid.cellSize.x) + 2f * extraClearance),
                Mathf.Max(0.01f, Mathf.Abs(grid.cellSize.y) + 2f * extraClearance)
            );
        }

        if (obstacleTilemap && obstacleTilemap.HasTile(cell)) return false;
        if (Physics2D.OverlapBox(center, probeSize, 0f, obstacleMask) != null) return false;
        return true;
    }

    bool FindNearestWalkable(ref Vector3Int cell, int maxRadius)
    {
        if (IsWalkable(cell)) return true;

        var q = new Queue<Vector3Int>();
        var visited = new HashSet<Vector3Int> { cell };
        q.Enqueue(cell);

        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in dirs)
            {
                var n = cur + d;
                if (!visited.Add(n)) continue;

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

    List<Vector3Int> AStar(Vector3Int start, Vector3Int goal, bool avoidAgents = false)
    {
        var open = new List<Vector3Int> { start };
        var came = new Dictionary<Vector3Int, Vector3Int>();
        var g = new Dictionary<Vector3Int, int> { [start] = 0 };
        var f = new Dictionary<Vector3Int, int> { [start] = Heu(start, goal) };
        var closed = new HashSet<Vector3Int>();
        int searchNodeLimit = Mathf.Max(256, maxPathSearchNodes);
        int searchDistanceLimit = Mathf.Max(16, maxPathSearchDistance, Heu(start, goal) + 16);
        int expandedNodes = 0;

        Vector3Int[] dirs = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        while (open.Count > 0)
        {
            if (++expandedNodes > searchNodeLimit)
            {
                Debug.LogWarning($"{name} AStar aborted after {searchNodeLimit} nodes. start={start}, goal={goal}, avoidAgents={avoidAgents}");
                return null;
            }

            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (f[open[i]] < f[open[best]]) best = i;

            var cur = open[best];
            if (cur == goal) return Reconstruct(came, cur);
            open.RemoveAt(best);
            closed.Add(cur);

            foreach (var d in dirs)
            {
                var nx = cur + d;
                if (closed.Contains(nx)) continue;
                if (Heu(start, nx) > searchDistanceLimit && Heu(goal, nx) > searchDistanceLimit) continue;
                if (!IsWalkable(nx)) continue;
                if (avoidAgents && nx != goal && AgentCrowdCoordinator.IsDynamicallyBlocked(nx, this, agentAvoidanceCellPadding)) continue;

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

    public void PortalRequestTeleport(Transform portal, Vector3 targetPosition,
                                      float preWait, float postWait
                                      )
    {
        if (isTeleporting) return;  // 防止重复触发
        isTeleporting = true;
        StopIdleWander();
        autoMove = Vector2.zero;
        if (rb) rb.linearVelocity = Vector2.zero;
        StartCoroutine(TeleportAfterDelay(targetPosition, preWait, postWait, vcam));
    }

    private IEnumerator TeleportAfterDelay(Vector3 targetPosition, float preWait, float postWait, CinemachineCamera vcam)
    {
        //延迟传送
        bool shouldCompleteGoTo = (curCmd == "go_to");
        var preTeleportPos = transform.position;

        if (rb) rb.linearVelocity = Vector2.zero;
        frozen = true;

        if (preWait > 0f)
            yield return new WaitForSeconds(preWait);

        CancelAuto();
        idleMoving = false;

        targetPosition.z = transform.position.z;
        if (rb != null)
        {
            rb.position = targetPosition;
            transform.position = targetPosition;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = targetPosition;
        }
        Physics2D.SyncTransforms();
        if (grid != null)
            AgentCrowdCoordinator.SyncCell(this, grid.WorldToCell(transform.position));
        lastPos = transform.position;
        if (vcam != null)
        {
            var warpTarget = vcam.Follow != null ? vcam.Follow : transform;
            vcam.OnTargetObjectWarped(warpTarget, transform.position - preTeleportPos);
            vcam.PreviousStateIsValid = false;
        }
        if (cam != null)
        {
            var p = cam.transform.position;
            cam.transform.position = new Vector3(transform.position.x, transform.position.y, p.z);
        }

        // 传送点通常是当前 go_to 的阶段终点，传送后直接进入后续动作
        if (shouldCompleteGoTo)
        {
            CompleteCurrent();
        }

        frozenUntil = Time.time + postWait;
        if (postWait > 0f)
            yield return new WaitForSeconds(postWait);

        isTeleporting = false;
        if (Time.time >= frozenUntil)
            frozen = false;
        

    }
}
