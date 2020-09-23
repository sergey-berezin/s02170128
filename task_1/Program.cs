using System;
using System.IO;
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
        static void PredictionCaught(object sender, EventArgs e)
        {
            Console.WriteLine((sender as PredictionQueue).TryDequeue());
        }

        static void Main(string[] args)
        {
            string ProjPath = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName;
            string DirPath = ProjPath + @"\pics_to_recognize\";

            OnnxClassifier clf = new OnnxClassifier(ProjPath + @"\model\resnet50-v2-7.onnx");

            PredictionQueue cq = new PredictionQueue();
            cq.Enqueued += PredictionCaught;
            clf.PredictAll(cq, DirPath);
        }
    }
}
