using System;
using System.Collections.Generic;
using UnityEngine;

namespace GridInfect.Game
{
    public sealed class TweenRunner
    {
        sealed class Move
        {
            public Transform Target;
            public Vector3 From, To;
            public float Elapsed, Duration;
            public Action OnDone;
        }

        readonly List<Move> _moves = new List<Move>(16);

        public void MoveTo(Transform target, Vector3 to, float duration, Action onDone = null)
        {
            // One tween per transform: a new move supersedes the old.
            Cancel(target);
            if (duration <= 0f)
            {
                target.localPosition = to;
                onDone?.Invoke();
                return;
            }
            _moves.Add(new Move
            {
                Target = target, From = target.localPosition, To = to, Duration = duration, OnDone = onDone,
            });
        }

        public void Cancel(Transform target)
        {
            for (int n = _moves.Count - 1; n >= 0; n--)
            {
                if (_moves[n].Target == target) _moves.RemoveAt(n);
            }
        }

        public void Update(float dt)
        {
            for (int n = _moves.Count - 1; n >= 0; n--)
            {
                var move = _moves[n];
                if (move.Target == null)
                {
                    _moves.RemoveAt(n);
                    continue;
                }
                move.Elapsed += dt;
                float t = move.Duration <= 0f ? 1f : Mathf.Clamp01(move.Elapsed / move.Duration);
                move.Target.localPosition = Vector3.Lerp(move.From, move.To, t); // linear, always
                if (t >= 1f)
                {
                    _moves.RemoveAt(n);
                    move.OnDone?.Invoke();
                }
            }
        }
    }
}
