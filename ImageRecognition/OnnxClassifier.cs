using System;
using System.IO;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;


namespace ImageRecognition
{
    public struct PredictionResult
    {
        public string ClassName;
        public string FilePath;
        public float Proba;
        public PredictionResult(string ClassName, string FilePath, float Proba)
        {
            this.ClassName = ClassName;
            this.FilePath = FilePath;
            this.Proba = Proba;
        }
    }

    public class OnnxClassifier
    {
        public InferenceSession Session { get; set; }
        static readonly string[] classLabels = System.IO.File.ReadAllLines(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\ImageRecognition\classLabels.txt");
        public CancellationTokenSource CTSource = new CancellationTokenSource();
        public OnnxClassifier(string ModelPath)
        {
            this.Session = new InferenceSession(ModelPath);
        }

        private DenseTensor<float> ProcessImage(string ImgPath="sample.jpg")
        {
            using var image = Image.Load<Rgb24>(ImgPath);

            const int TargetWidth = 224;
            const int TargetHeight = 224;

            // Изменяем размер картинки до 224 x 224
            image.Mutate(x =>
            {
                x.Resize(new ResizeOptions
                {
                    Size = new Size(TargetWidth, TargetHeight),
                    Mode = ResizeMode.Crop // Сохраняем пропорции обрезая лишнее
                });
            });

            // Перевод пикселов в тензор и нормализация
            var res = new DenseTensor<float>(new[] { 1, 3, TargetHeight, TargetWidth });
            var mean = new[] { 0.485f, 0.456f, 0.406f };
            var stddev = new[] { 0.229f, 0.224f, 0.225f };
            for (int y = 0; y < TargetHeight; y++)
            {
                Span<Rgb24> pixelSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < TargetWidth; x++)
                {
                    res[0, 0, y, x] = ((pixelSpan[x].R / 255f) - mean[0]) / stddev[0];
                    res[0, 1, y, x] = ((pixelSpan[x].G / 255f) - mean[1]) / stddev[1];
                    res[0, 2, y, x] = ((pixelSpan[x].B / 255f) - mean[2]) / stddev[2];
                }
            }

            return res;
        }

        public PredictionResult Predict(string ImgPath)
        {
            var input = ProcessImage(ImgPath);

            // Вычисляем предсказание нейросетью

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(Session.InputMetadata.Keys.First(),input)
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = Session.Run(inputs);

            // Получаем 1000 выходов и считаем для них softmax
            var output = results.First().AsEnumerable<float>().ToArray();
            var sum = output.Sum(x => (float)Math.Exp(x));
            var softmax = output.Select(x => (float)Math.Exp(x) / sum);

            return new PredictionResult(classLabels[softmax.ToList().IndexOf(softmax.Max())], ImgPath, softmax.ToList().Max());
        }
        public void StopPrediction()
        {
            CTSource.Cancel();
        }
        public void PredictAll(PredictionQueue cq, FileInfo[] Files)
        {
            var tasks = Task.Factory.StartNew(() =>
            {
                try
                {
                    Parallel.ForEach(
                        Files,
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = CTSource.Token },
                        f =>
                        {
                        cq.Enqueue(Predict(f.FullName));
                        });
                }
                catch (OperationCanceledException)
                {
                    Trace.WriteLine("*** Tasks were cancelled");
                }
            });
            tasks.Wait();
        }
    }
}
