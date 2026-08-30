using System;
using System.Collections.Generic;

namespace Bloodhound.Engine
{
    /// <summary>
    /// A named, versioned writer of meaningful state — the only kind of writer
    /// there is. Implementations declare one Name (unique in the registry, one
    /// owner module), Validate as a pure precondition over (state, input), and
    /// Execute as the single mutation site. Mechanical reads live in query
    /// classes, never here; policy lives in pure decision functions the action
    /// calls, not in adapter branches.
    /// </summary>
    public abstract class GameAction<TState>
    {
        /// <summary>Registry name, "aggregate.verb" (e.g. "piece.place").</summary>
        public abstract string Name { get; }

        /// <summary>
        /// Contract version. Bump when input schema or semantics change;
        /// change is additive — a new behavior is a new version or a new
        /// action, never an in-place break of a logged contract.
        /// </summary>
        public virtual int Version => 1;

        /// <summary>Null when the action may run; otherwise a reason string.</summary>
        public abstract string Validate(TState state, ActionInput input);

        /// <summary>Apply the action. Runs only after Validate returned null.</summary>
        public abstract void Execute(TState state, ActionInput input);
    }

    /// <summary>Result of a dispatch: applied (with its log entry) or rejected (with a reason).</summary>
    public readonly struct ActionResult
    {
        public readonly ActionEntry Entry;   // null when rejected
        public readonly string Rejection;    // null when applied

        ActionResult(ActionEntry entry, string rejection)
        {
            Entry = entry;
            Rejection = rejection;
        }

        public bool Applied => Rejection == null;

        public static ActionResult Ok(ActionEntry entry) => new ActionResult(entry, null);
        public static ActionResult Rejected(string reason) => new ActionResult(null, reason ?? "rejected");
    }

    /// <summary>
    /// The action registry — the second founding artifact (the schema being the
    /// first). Maps every action name to its single implementation.
    /// </summary>
    public sealed class ActionRegistry<TState>
    {
        readonly Dictionary<string, GameAction<TState>> _actions =
            new Dictionary<string, GameAction<TState>>(StringComparer.Ordinal);

        public IEnumerable<GameAction<TState>> All => _actions.Values;
        public int Count => _actions.Count;

        public void Register(GameAction<TState> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (string.IsNullOrEmpty(action.Name))
                throw new ArgumentException("action must have a name");
            if (_actions.ContainsKey(action.Name))
                throw new InvalidOperationException($"duplicate action name '{action.Name}'");
            _actions.Add(action.Name, action);
        }

        public GameAction<TState> Get(string name)
        {
            if (_actions.TryGetValue(name, out var action)) return action;
            throw new KeyNotFoundException($"unknown action '{name}'");
        }

        public bool TryGet(string name, out GameAction<TState> action) => _actions.TryGetValue(name, out action);
    }

    /// <summary>
    /// Owns the state, the registry, and the log; every meaningful mutation
    /// goes through <see cref="Dispatch"/>. Replay folds a stored log over a
    /// fresh state through the exact same path.
    /// </summary>
    public sealed class Dispatcher<TState>
    {
        public TState State { get; }
        public ActionRegistry<TState> Registry { get; }
        public ActionLog Log { get; } = new ActionLog();

        /// <summary>Fired after an entry is applied and logged (persistence hooks live here).</summary>
        public event Action<ActionEntry> Applied;

        public Dispatcher(TState state, ActionRegistry<TState> registry)
        {
            State = state;
            Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public ActionResult Dispatch(string name, Dictionary<string, object> input = null)
        {
            var action = Registry.Get(name);
            var wrapped = new ActionInput(input);
            string reason;
            try
            {
                reason = action.Validate(State, wrapped);
            }
            catch (ActionSchemaException e)
            {
                reason = e.Message;
            }
            if (reason != null) return ActionResult.Rejected($"{name}: {reason}");

            action.Execute(State, wrapped);
            var entry = Log.Append(name, action.Version, input);
            Applied?.Invoke(entry);
            return ActionResult.Ok(entry);
        }

        /// <summary>
        /// Re-apply a stored log against this dispatcher's (fresh) state.
        /// A replay failure means the log and the code disagree — surface it,
        /// never skip entries.
        /// </summary>
        public void Replay(IEnumerable<ActionEntry> entries)
        {
            foreach (var stored in entries)
            {
                var result = Dispatch(stored.Action, stored.Input);
                if (!result.Applied)
                    throw new InvalidOperationException(
                        $"replay diverged at seq {stored.Seq} ({stored.Action}): {result.Rejection}");
            }
        }
    }
}
