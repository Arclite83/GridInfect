using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Bloodhound.Engine
{
    // A handle on a job handed to Work: poll it, block on it, or take its
    // result on the main thread through Work.Pump.
    public sealed class WorkItem<T>
    {
        internal readonly TaskCompletionSource<T> Source =
            new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        internal Action<T> OnDone;

        public string Key { get; internal set; }
        public bool IsDone => Source.Task.IsCompleted;
        public bool Cancelled => Source.Task.IsCanceled;
        public bool Faulted => Source.Task.IsFaulted;
        public Exception Error => Source.Task.IsFaulted ? Source.Task.Exception?.InnerException : null;

        // The result once it landed; default until then, or if the job failed.
        public T Result => Source.Task.Status == TaskStatus.RanToCompletion ? Source.Task.Result : default;

        // Blocks the caller until the job lands; rethrows the job's exception.
        public T Wait()
        {
            try { return Source.Task.Result; }
            catch (AggregateException e) { throw e.InnerException ?? e; }
        }
    }

    // Background work for the kernel. Hand it a lambda and a priority; it
    // runs on a worker (lowest priority first, then submission order), the
    // handle answers on any thread, and a completion callback runs on
    // whichever thread calls Pump, which the adapter does once a frame, so
    // results reach the main thread without a job ever touching it.
    //
    // Workers are a count of jobs in flight at once, one by default, so a
    // queue of expensive jobs keeps its priority order meaningful: the
    // most likely thing next is what is being worked on. A job that is
    // itself wide fans its inner loop across the cores with Map instead.
    // A key names a job: submitting the same key again while it is still
    // queued raises its priority rather than queueing it twice, Cancel
    // takes a queued job back, and Raise moves one up.
    public sealed class Work
    {
        public static Work Shared = new Work(1);

        public int Workers { get; }

        // How wide Map fans out: the cores, unless told otherwise.
        public int Parallelism { get; set; }

        abstract class Job
        {
            public int Priority;
            public long Order;
            public string Key;
            public abstract void Execute();
            public abstract void Cancel();
            public abstract Action Completion();
        }

        sealed class Job<T> : Job
        {
            public Func<T> Body;
            public WorkItem<T> Item;

            public override void Execute()
            {
                try { Item.Source.TrySetResult(Body()); }
                catch (Exception e) { Item.Source.TrySetException(e); }
            }

            public override void Cancel() => Item.Source.TrySetCanceled();

            public override Action Completion()
            {
                if (Item.OnDone == null || Item.Source.Task.Status != TaskStatus.RanToCompletion) return null;
                var item = Item;
                return () => item.OnDone(item.Result);
            }
        }

        readonly object _gate = new object();
        readonly List<Job> _queue = new List<Job>();
        readonly Dictionary<string, Job> _queued = new Dictionary<string, Job>(StringComparer.Ordinal);
        readonly Queue<Action> _completions = new Queue<Action>();
        int _workersActive;
        int _running;
        long _order;

        public Work(int workers, int parallelism = 0)
        {
            Workers = Math.Max(1, workers);
            Parallelism = parallelism > 0 ? parallelism : Math.Max(1, Environment.ProcessorCount);
        }

        // Jobs queued or running.
        public int Pending { get { lock (_gate) return _queue.Count + _running; } }

        public WorkItem<T> Run<T>(Func<T> body, int priority = 10, string key = null, Action<T> onDone = null)
        {
            Job<T> job;
            lock (_gate)
            {
                if (key != null && _queued.TryGetValue(key, out Job existing) && existing is Job<T> same)
                {
                    if (priority < same.Priority) same.Priority = priority;
                    if (onDone != null) same.Item.OnDone += onDone;
                    return same.Item;
                }
                job = new Job<T>
                {
                    Body = body, Priority = priority, Order = ++_order, Key = key,
                    Item = new WorkItem<T> { Key = key, OnDone = onDone },
                };
                _queue.Add(job);
                if (key != null) _queued[key] = job;
                if (_workersActive >= Workers) return job.Item;
                _workersActive++;
            }
            Task.Run((Action)Drain);
            return job.Item;
        }

        // Takes a queued job back. True if it had not started; a running
        // job runs to its end and this returns false.
        public bool Cancel(string key)
        {
            Job job;
            lock (_gate)
            {
                if (key == null || !_queued.TryGetValue(key, out job)) return false;
                _queued.Remove(key);
                _queue.Remove(job);
            }
            job.Cancel();
            return true;
        }

        // Moves a queued job up (never down). False if it is not queued.
        public bool Raise(string key, int priority)
        {
            lock (_gate)
            {
                if (key == null || !_queued.TryGetValue(key, out Job job)) return false;
                if (priority < job.Priority) job.Priority = priority;
                return true;
            }
        }

        public void CancelQueued()
        {
            List<Job> jobs;
            lock (_gate)
            {
                jobs = new List<Job>(_queue);
                _queue.Clear();
                _queued.Clear();
            }
            foreach (Job job in jobs) job.Cancel();
        }

        // Runs the completion callbacks of landed jobs on the calling thread.
        public void Pump()
        {
            while (true)
            {
                Action completion;
                lock (_gate)
                {
                    if (_completions.Count == 0) return;
                    completion = _completions.Dequeue();
                }
                completion();
            }
        }

        // A job's own wide loop: body(i) for i in [0, count) across the
        // cores, returning once every index has run.
        public void Map(int count, Action<int> body)
        {
            if (count <= 0) return;
            if (Parallelism <= 1 || count == 1)
            {
                for (int i = 0; i < count; i++) body(i);
                return;
            }
            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = Parallelism }, body);
        }

        void Drain()
        {
            while (true)
            {
                Job job;
                lock (_gate)
                {
                    if (_queue.Count == 0) { _workersActive--; return; }
                    int best = 0;
                    for (int n = 1; n < _queue.Count; n++)
                    {
                        if (_queue[n].Priority < _queue[best].Priority ||
                            (_queue[n].Priority == _queue[best].Priority && _queue[n].Order < _queue[best].Order)) best = n;
                    }
                    job = _queue[best];
                    _queue.RemoveAt(best);
                    if (job.Key != null) _queued.Remove(job.Key);
                    _running++;
                }
                job.Execute();
                Action completion = job.Completion();
                lock (_gate)
                {
                    _running--;
                    if (completion != null) _completions.Enqueue(completion);
                }
            }
        }
    }
}
