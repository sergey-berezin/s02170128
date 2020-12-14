using ImageRecognition;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Contracts
{
    public interface IDeepCopy
    {
        object DeepCopy();
    }

    public class ModelPrediction : PredictionResult, IDeepCopy
    {

        private byte[] imageData;
        public byte[] ImageData
        {
            get { return imageData; }
            set { imageData = value; }
        }
        
        public ModelPrediction() { }
        public ModelPrediction(PredictionResult pr) : base(pr.ClassName, pr.FilePath, pr.Proba)
        {

            ImageData = File.ReadAllBytes(pr.FilePath);
        }

        public ModelPrediction(string ClassName, float proba, string filepath, byte[] ImageData)
        {
            this.ClassName = ClassName;
            this.Proba = proba;
            this.FilePath = filepath;
            this.ImageData = ImageData;
        }

        public virtual object DeepCopy()
        {
            ModelPrediction buf = new ModelPrediction(ClassName, Proba, FilePath, ImageData);
            return buf;
        }
    }
}
