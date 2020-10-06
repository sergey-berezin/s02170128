using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ImageRecognition
{
    public class PredictionEventArgs : EventArgs
    {
        private PredictionResult _predictionResult;
        public PredictionResult PredictionResult { get { return _predictionResult; } }
        public PredictionEventArgs(PredictionResult predictionResult)
        {
            _predictionResult = predictionResult;
        }

    }

    public class PredictionQueue
    {
            private readonly ConcurrentQueue<PredictionResult> queue = new ConcurrentQueue<PredictionResult>();
            public event EventHandler<PredictionEventArgs> Enqueued;
            protected virtual void OnEnqueued(PredictionEventArgs e)
            {
                if (Enqueued != null)
                    Enqueued(this, e);
            }
            public virtual void Enqueue(PredictionResult item)
            {
                queue.Enqueue(item);
                OnEnqueued(new PredictionEventArgs(item));
            }
            public virtual PredictionResult TryDequeue()
            {
                PredictionResult item; 
                queue.TryDequeue(out item);
                return item;
            }
    }
}
