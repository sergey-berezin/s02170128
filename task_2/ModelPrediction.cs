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
        public float proba;
        private string filepath;
        public string FilePath { get { return filepath; } set { filepath = value; } }


        private byte[] imageData;
        public byte[] ImageData
        {
            get { return imageData; }
            set { imageData = value; }
        }
        
        public ModelPrediction() { }
        public ModelPrediction(PredictionResult pr)
        {
            ClassName = pr.ClassName;
            proba = pr.Proba;
            filepath = pr.FilePath; 

            ImageData = File.ReadAllBytes(pr.FilePath);
        }

        public ModelPrediction(string ClassName, float proba, string filepath, byte[] ImageData)
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
