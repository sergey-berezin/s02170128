using System;
using System.Collections.Generic;
using System.Text;

namespace ImageRecognition
{
    public class PredictionResult
    {
        public string ClassName { get; set; }
        public string FilePath { get; set; }
        public float Proba { get; set; }
        public PredictionResult() { }
        public PredictionResult(string ClassName, string FilePath, float Proba)
        {
            this.ClassName = ClassName;
            this.FilePath = FilePath;
            this.Proba = Proba;
        }
    }
}
