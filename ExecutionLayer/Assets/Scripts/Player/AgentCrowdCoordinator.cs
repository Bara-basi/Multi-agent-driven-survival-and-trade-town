using System.Collections.Generic;
using UnityEngine;

public static class AgentCrowdCoordinator
{
    private sealed class StepReservation
    {
        public Vector3Int From;
        public Vector3Int To;
    }

    private sealed class QueueState
    {
        public readonly List<AutoMove> Agents = new();
    }

    private static readonly Dictionary<AutoMove, Vector3Int> agentCells = new();
    private static readonly Dictionary<Vector3Int, HashSet<AutoMove>> cellOccupants = new();
    private static readonly Dictionary<AutoMove, StepReservation> reservationsByAgent = new();
    private static readonly Dictionary<Vector3Int, AutoMove> reservedCells = new();
    private static readonly Dictionary<string, QueueState> queues = new();
    private static readonly Dictionary<AutoMove, string> queueByAgent = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ResetSceneState()
    {
        ClearAll();
    }

    public static void ClearAll()
    {
        agentCells.Clear();
        cellOccupants.Clear();
        reservationsByAgent.Clear();
        reservedCells.Clear();
        queues.Clear();
        queueByAgent.Clear();
    }

    public static void SyncCell(AutoMove agent, Vector3Int cell)
    {
        if (agent == null) return;

        if (agentCells.TryGetValue(agent, out var oldCell) && oldCell != cell)
            RemoveOccupant(oldCell, agent);

        agentCells[agent] = cell;
        if (!cellOccupants.TryGetValue(cell, out var occupants))
        {
            occupants = new HashSet<AutoMove>();
            cellOccupants[cell] = occupants;
        }
        occupants.Add(agent);
    }

    public static bool TryReserveStep(AutoMove agent, Vector3Int from, Vector3Int to, int clearanceCells = 0)
    {
        if (agent == null) return false;
        if (from == to) return true;

        ReleaseStepReservation(agent);

        if (IsOccupiedByOther(to, agent, clearanceCells)) return false;
        if (IsReservedByOther(to, agent, clearanceCells)) return false;

        foreach (var kv in reservationsByAgent)
        {
            if (kv.Key == agent) continue;
            var other = kv.Value;
            if (other.From == to && other.To == from) return false;
        }

        reservationsByAgent[agent] = new StepReservation { From = from, To = to };
        reservedCells[to] = agent;
        return true;
    }

    public static void CommitStep(AutoMove agent, Vector3Int cell)
    {
        if (agent == null) return;

        ReleaseStepReservation(agent);

        if (agentCells.TryGetValue(agent, out var oldCell))
            RemoveOccupant(oldCell, agent);

        agentCells[agent] = cell;
        if (!cellOccupants.TryGetValue(cell, out var occupants))
        {
            occupants = new HashSet<AutoMove>();
            cellOccupants[cell] = occupants;
        }
        occupants.Add(agent);
    }

    public static void ReleaseStepReservation(AutoMove agent)
    {
        if (agent == null) return;

        if (!reservationsByAgent.TryGetValue(agent, out var reservation)) return;

        reservationsByAgent.Remove(agent);
        if (reservedCells.TryGetValue(reservation.To, out var reservedBy) && reservedBy == agent)
            reservedCells.Remove(reservation.To);
    }

    public static bool IsDynamicallyBlocked(Vector3Int cell, AutoMove requester, int clearanceCells = 0)
    {
        if (IsOccupiedByOther(cell, requester, clearanceCells)) return true;
        return IsReservedByOther(cell, requester, clearanceCells);
    }

    public static void JoinQueue(AutoMove agent, string queueKey)
    {
        if (agent == null || string.IsNullOrEmpty(queueKey)) return;

        if (queueByAgent.TryGetValue(agent, out var currentKey) && currentKey == queueKey)
            return;

        LeaveQueue(agent);

        if (!queues.TryGetValue(queueKey, out var state))
        {
            state = new QueueState();
            queues[queueKey] = state;
        }

        if (!state.Agents.Contains(agent))
            state.Agents.Add(agent);

        queueByAgent[agent] = queueKey;
    }

    public static void LeaveQueue(AutoMove agent)
    {
        if (agent == null) return;

        if (!queueByAgent.TryGetValue(agent, out var key)) return;
        queueByAgent.Remove(agent);

        if (!queues.TryGetValue(key, out var state)) return;

        state.Agents.Remove(agent);
        state.Agents.RemoveAll(a => a == null);
        if (state.Agents.Count == 0)
            queues.Remove(key);
    }

    public static int GetQueueRank(AutoMove agent, string queueKey)
    {
        if (agent == null || string.IsNullOrEmpty(queueKey)) return -1;
        if (!queues.TryGetValue(queueKey, out var state)) return -1;

        state.Agents.RemoveAll(a => a == null);
        return state.Agents.IndexOf(agent);
    }

    public static bool IsQueueHead(AutoMove agent, string queueKey)
    {
        return GetQueueRank(agent, queueKey) == 0;
    }

    public static void Unregister(AutoMove agent)
    {
        if (agent == null) return;

        ReleaseStepReservation(agent);
        LeaveQueue(agent);

        if (agentCells.TryGetValue(agent, out var cell))
        {
            RemoveOccupant(cell, agent);
            agentCells.Remove(agent);
        }
    }

    private static bool IsOccupiedByOther(Vector3Int cell, AutoMove requester, int clearanceCells = 0)
    {
        var emptyCells = new List<Vector3Int>();
        int clearance = Mathf.Max(0, clearanceCells);

        foreach (var kv in cellOccupants)
        {
            if (CellDistance(cell, kv.Key) > clearance) continue;

            var occupants = kv.Value;
            occupants.RemoveWhere(a => a == null);
            if (occupants.Count == 0)
            {
                emptyCells.Add(kv.Key);
                continue;
            }

            foreach (var occupant in occupants)
            {
                if (occupant != requester)
                    return true;
            }
        }

        for (int i = 0; i < emptyCells.Count; i++)
            cellOccupants.Remove(emptyCells[i]);

        return false;
    }

    private static bool IsReservedByOther(Vector3Int cell, AutoMove requester, int clearanceCells = 0)
    {
        int clearance = Mathf.Max(0, clearanceCells);
        foreach (var kv in reservedCells)
        {
            if (kv.Value == requester) continue;
            if (CellDistance(cell, kv.Key) <= clearance)
                return true;
        }
        return false;
    }

    private static int CellDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static void RemoveOccupant(Vector3Int cell, AutoMove agent)
    {
        if (!cellOccupants.TryGetValue(cell, out var occupants)) return;

        occupants.Remove(agent);
        occupants.RemoveWhere(a => a == null);
        if (occupants.Count == 0)
            cellOccupants.Remove(cell);
    }
}
