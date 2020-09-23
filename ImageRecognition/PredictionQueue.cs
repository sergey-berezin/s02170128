using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ImageRecognition
{
    public class PredictionQueue
    {
            private readonly ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
            public event EventHandler Enqueued;
            protected virtual void OnEnqueued(EventArgs e)
            {
                if (Enqueued != null)
                    Enqueued(this, e);
            }
            public virtual void Enqueue(string item)
            {
                queue.Enqueue(item);
                OnEnqueued(new EventArgs());
            }
            public virtual string TryDequeue()
            {
                string item; 
                queue.TryDequeue(out item);
                return item;
            }
    }
}
