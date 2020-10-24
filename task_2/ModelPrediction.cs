using ImageRecognition;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace task_2
{
    public interface IDeepCopy
    {
        object DeepCopy();
    }

    public class ModelPrediction : IDeepCopy
    {
        private string className;
        public string ClassName { get { return className; } set { className = value; } }
        private float proba;
        private string filepath;

        private BitmapImage imageData;
        public BitmapImage ImageData
        {
            get { return imageData; }
            set { imageData = value; }
        }

        public ModelPrediction(PredictionResult pr)
        {
            ClassName = pr.ClassName;
            proba = pr.Proba;
            filepath = pr.FilePath;
            ImageData = new BitmapImage(new Uri(filepath));
        }

        public ModelPrediction(string ClassName, float proba, string filepath, BitmapImage ImageData)
        {
            this.ClassName = ClassName;
            this.proba = proba;
            this.filepath = filepath;
            this.ImageData = ImageData;
        }

        public virtual object DeepCopy()
        {
            ModelPrediction buf = new ModelPrediction(ClassName, proba, filepath, ImageData);
            return buf;
        }
    }
}
