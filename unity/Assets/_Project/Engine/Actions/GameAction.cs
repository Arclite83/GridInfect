using System;
using System.Collections.Generic;

namespace Bloodhound.Engine
{
    // The only kind of writer there is: one name, one owner module, Validate as
    // a pure precondition, Execute as the sole mutation site.
    public abstract class GameAction<TState>
    {
        public abstract string Name { get; }

        public virtual int Version => 1;

        public abstract string Validate(TState state, ActionInput input);

        public abstract void Execute(TState state, ActionInput input);
    }

    // A rejection is an answer, not an error: nothing logged, nothing mutated.
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

    public sealed class Dispatcher<TState>
    {
        public TState State { get; }
        public ActionRegistry<TState> Registry { get; }
        public ActionLog Log { get; } = new ActionLog();

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
