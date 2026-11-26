
using System;
using System.Collections.Generic;
using UnityEngine;

public interface IUndoableAction { void Undo(); void Redo(); }

public class UndoRedoManager : MonoBehaviour
{
    private readonly Stack<IUndoableAction> undoStack = new();
    private readonly Stack<IUndoableAction> redoStack = new();
    private List<IUndoableAction> transactionActions;
    private bool isApplyingHistory;

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;
    public bool IsTransactionActive => transactionActions != null;
    public bool IsApplyingHistory => isApplyingHistory;
    public int UndoCount => undoStack.Count;
    public int RedoCount => redoStack.Count;
    public event Action OnHistoryChanged;

    public void Execute(IUndoableAction action)
    {
        if (action == null || isApplyingHistory) return;

        try { action.Redo(); Record(action); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }

    public void Record(IUndoableAction action)
    {
        if (action == null || isApplyingHistory) return;
        if (transactionActions != null) { transactionActions.Add(action); return; }

        undoStack.Push(action);
        redoStack.Clear();
        OnHistoryChanged?.Invoke();
    }

    public void RecordAction(Action undo, Action redo)
    {
        if (undo == null || redo == null) return;
        Record(new DelegateAction(undo, redo));
    }

    public void BeginTransaction()
    {
        if (isApplyingHistory || transactionActions != null) return;
        transactionActions = new List<IUndoableAction>();
    }

    public void EndTransaction()
    {
        if (transactionActions == null || isApplyingHistory) return;

        List<IUndoableAction> actions = transactionActions;
        transactionActions = null;

        if (actions.Count == 0) return;
        if (actions.Count == 1) Record(actions[0]);
        else Record(new CompositeAction(actions));
    }

    public void CancelTransaction()
    {
        if (transactionActions == null || isApplyingHistory) return;

        List<IUndoableAction> actions = transactionActions;
        isApplyingHistory = true;

        try
        {
            for (int i = actions.Count - 1; i >= 0; i--)
                actions[i].Undo();
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
        finally
        {
            transactionActions = null;
            isApplyingHistory = false;
        }

        OnHistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo || isApplyingHistory || transactionActions != null) return;

        IUndoableAction action = undoStack.Peek();
        bool succeeded = false;
        isApplyingHistory = true;

        try
        {
            action.Undo();
            undoStack.Pop();
            redoStack.Push(action);
            succeeded = true;
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
        finally { isApplyingHistory = false; }

        if (succeeded) OnHistoryChanged?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo || isApplyingHistory || transactionActions != null) return;

        IUndoableAction action = redoStack.Peek();
        bool succeeded = false;
        isApplyingHistory = true;

        try
        {
            action.Redo();
            redoStack.Pop();
            undoStack.Push(action);
            succeeded = true;
        }
        catch (Exception exception) { Debug.LogException(exception, this); }
        finally { isApplyingHistory = false; }

        if (succeeded) OnHistoryChanged?.Invoke();
    }

    public void ClearHistory()
    {
        if (isApplyingHistory || transactionActions != null) return;

        undoStack.Clear();
        redoStack.Clear();
        OnHistoryChanged?.Invoke();
    }

    public class ObjectTransformSnapshot
    {
        private struct State
        {
            public VoxelObject Object;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            public State(VoxelObject obj)
            {
                Object = obj;
                Position = obj.transform.position;
                Rotation = obj.transform.rotation;
                Scale = obj.transform.localScale;
            }

            public bool SameAs(State other)
            {
                return Object == other.Object &&
                       (Position - other.Position).sqrMagnitude < 0.00000001f &&
                       Quaternion.Angle(Rotation, other.Rotation) < 0.001f &&
                       (Scale - other.Scale).sqrMagnitude < 0.00000001f;
            }
        }

        private readonly List<State> states = new();

        public ObjectTransformSnapshot(IEnumerable<VoxelObject> objects)
        {
            if (objects == null) return;
            foreach (VoxelObject obj in objects)
                if (obj != null) states.Add(new State(obj));
        }

        public ObjectTransformSnapshot CaptureCurrent()
        {
            var objects = new List<VoxelObject>();
            foreach (State state in states)
            {
                if (state.Object == null)
                    throw new InvalidOperationException("Transform object no longer exists.");

                objects.Add(state.Object);
            }
            return new ObjectTransformSnapshot(objects);
        }

        public bool SameAs(ObjectTransformSnapshot other)
        {
            if (other == null || states.Count != other.states.Count) return false;

            for (int i = 0; i < states.Count; i++)
                if (!states[i].SameAs(other.states[i])) return false;

            return true;
        }

        public void Apply()
        {
            foreach (State state in states)
                if (state.Object == null)
                    throw new InvalidOperationException("Cannot restore destroyed object.");

            foreach (State state in states)
            {
                state.Object.transform.position = state.Position;
                state.Object.transform.rotation = state.Rotation;
                state.Object.transform.localScale = state.Scale;
            }
        }
    }

    public ObjectTransformSnapshot CaptureObjectTransforms(IEnumerable<VoxelObject> objects)
    {
        return new ObjectTransformSnapshot(objects);
    }

    public void RecordObjectTransforms(ObjectTransformSnapshot before)
    {
        if (before == null || isApplyingHistory) return;

        ObjectTransformSnapshot after = before.CaptureCurrent();
        if (before.SameAs(after)) return;

        RecordAction(
            undo: () => before.Apply(),
            redo: () => after.Apply()
        );
    }

    public class VoxelStateSnapshot
    {
        public readonly VoxelModel Model;
        public readonly Dictionary<Vector3Int, Color> Voxels = new();

        public VoxelStateSnapshot(VoxelModel model)
        {
            Model = model;
            if (model == null) return;

            foreach (var voxel in model.Voxels)
                Voxels.Add(voxel.Key, voxel.Value);
        }
    }

    public VoxelStateSnapshot CaptureVoxelState(VoxelModel model)
    {
        return new VoxelStateSnapshot(model);
    }


public void RecordVoxelChanges(VoxelStateSnapshot before, VoxelGroupManager groupManager = null, VoxelGroupManager.GroupStateSnapshot beforeGroups = null)
{
    if (before == null || before.Model == null || isApplyingHistory) return;

    VoxelStateSnapshot after = CaptureVoxelState(before.Model);
    var cells = new HashSet<Vector3Int>(before.Voxels.Keys);
    cells.UnionWith(after.Voxels.Keys);

    var changed = new List<Vector3Int>();

    foreach (Vector3Int cell in cells)
    {
        bool hadBefore = before.Voxels.TryGetValue(cell, out Color oldColor);
        bool hasAfter = after.Voxels.TryGetValue(cell, out Color newColor);

        if (hadBefore != hasAfter || (hadBefore && !oldColor.Equals(newColor)))
            changed.Add(cell);
    }

    if (changed.Count == 0) return;

    VoxelModel model = before.Model;
    var afterGroups = groupManager != null && beforeGroups != null ? groupManager.CaptureState() : null;

    RecordAction(
        undo: () =>
        {
            ApplyVoxelChanges(model, changed, after.Voxels, before.Voxels);
            if (afterGroups != null) groupManager.RestoreState(beforeGroups);
        },
        redo: () =>
        {
            ApplyVoxelChanges(model, changed, before.Voxels, after.Voxels);
            if (afterGroups != null) groupManager.RestoreState(afterGroups);
        }
    );
}


    private static void ApplyVoxelChanges(
        VoxelModel model,
        List<Vector3Int> cells,
        Dictionary<Vector3Int, Color> expected,
        Dictionary<Vector3Int, Color> target)
    {
        if (model == null)
            throw new InvalidOperationException("Voxel model no longer exists.");

        foreach (Vector3Int cell in cells)
        {
            bool shouldExist = expected.TryGetValue(cell, out Color expectedColor);
            bool exists = model.TryGetVoxelColor(cell, out Color currentColor);

            if (exists != shouldExist || (exists && !currentColor.Equals(expectedColor)))
                throw new InvalidOperationException($"Voxel state conflict at {cell}.");
        }

        foreach (Vector3Int cell in cells)
        {
            if (target.ContainsKey(cell)) continue;

            if (!model.RemoveVoxel(cell))
                throw new InvalidOperationException($"Cannot remove voxel at {cell}.");
        }

        foreach (Vector3Int cell in cells)
        {
            if (!target.TryGetValue(cell, out Color color)) continue;

            if (model.HasVoxel(cell))
            {
                if (!model.SetVoxelColor(cell, color))
                    throw new InvalidOperationException($"Cannot recolor voxel at {cell}.");
            }
            else if (!model.AddVoxel(cell, color))
            {
                throw new InvalidOperationException($"Cannot restore voxel at {cell}.");
            }
        }
    }


public bool DeleteFreeObject(VoxelObjectManager manager, VoxelObject obj, VoxelGroupManager groups, SelectionManager selection)
{
    if (manager == null || obj == null || isApplyingHistory) return false;

    var beforeGroups = groups != null ? groups.CaptureState() : null;

    if (!manager.UnregisterObject(obj)) return false;

    if (groups != null) groups.RemoveObjectFromGroup(obj);
    if (selection != null) selection.RemoveFromSelection(obj);
    obj.gameObject.SetActive(false);

    var afterGroups = groups != null ? groups.CaptureState() : null;
    VoxelObject savedObject = obj;

    RecordAction(
        undo: () =>
        {
            if (savedObject == null || !manager.RegisterObject(savedObject))
                throw new InvalidOperationException("Cannot restore deleted Free object.");

            savedObject.gameObject.SetActive(true);
            if (beforeGroups != null) groups.RestoreState(beforeGroups);
        },
        redo: () =>
        {
            if (savedObject == null || !manager.UnregisterObject(savedObject))
                throw new InvalidOperationException("Cannot redo Free object deletion.");

            if (selection != null) selection.RemoveFromSelection(savedObject);
            savedObject.gameObject.SetActive(false);
            if (afterGroups != null) groups.RestoreState(afterGroups);
        }
    );

    return true;
}



public void RecordGroupChanges(VoxelGroupManager manager, VoxelGroupManager.GroupStateSnapshot before)
{
    if (manager == null || before == null || isApplyingHistory) return;

    var after = manager.CaptureState();

    RecordAction(
        undo: () => manager.RestoreState(before),
        redo: () => manager.RestoreState(after)
    );
}


    private class DelegateAction : IUndoableAction
    {
        private readonly Action undo;
        private readonly Action redo;

        public DelegateAction(Action undo, Action redo)
        {
            this.undo = undo;
            this.redo = redo;
        }

        public void Undo() => undo();
        public void Redo() => redo();
    }

    private class CompositeAction : IUndoableAction
    {
        private readonly List<IUndoableAction> actions;

        public CompositeAction(List<IUndoableAction> actions) { this.actions = new List<IUndoableAction>(actions); }

        public void Undo() { for (int i = actions.Count - 1; i >= 0; i--) actions[i].Undo(); }
        public void Redo() { foreach (IUndoableAction action in actions) action.Redo(); }
    }
}
