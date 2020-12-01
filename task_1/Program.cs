using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Threading.Tasks;
using System.Drawing;
using Xunit;
using Xunit.Abstractions;
using ImageRecognition;
using System.Collections.Concurrent;
using System.Threading;

namespace task_1
{
    class Program
    {
        static void PredictionCaught(object sender, PredictionEventArgs e)
        {
            Console.WriteLine(String.Format("{0}\n{1} : {2}\n", e.PredictionResult.FilePath, e.PredictionResult.ClassName, e.PredictionResult.Proba));
        }

        static void Main(string[] args)
        {
            ConsoleTraceListener listener = new ConsoleTraceListener();
            Trace.Listeners.Add(listener);

            string ProjPath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;
            string DirPath = ProjPath + @"\pics_to_recognize\";

            OnnxClassifier clf = new OnnxClassifier(ProjPath + @"\model\resnet50-v2-7.onnx");

            PredictionQueue cq = new PredictionQueue();
            cq.Enqueued += PredictionCaught;
            Task keyBoardTask = Task.Run(() =>
            {
                Trace.WriteLine("*** Press Esc to cancel");
                while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
                {
                }
                clf.StopPrediction();
            });
            clf.PredictAll(cq, new DirectoryInfo(DirPath).GetFiles());

            Trace.Listeners.Remove(listener);
            Trace.Close();
        }
    }
}
