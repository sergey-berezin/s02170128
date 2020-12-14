using System;
using System.Globalization;
using System.IO;

namespace Contracts
{
    public class PredictionRequest
    {
        private string filePath;
        public string FilePath { get { return filePath; } set { filePath = value; } }

        private string image;
        public string Image { get { return image; } set { image = value; } }
        public PredictionRequest() { }
        public PredictionRequest(string FilePath)
        {
            this.FilePath = FilePath;
            this.Image = Convert.ToBase64String(File.ReadAllBytes(FilePath));
        }

        public PredictionRequest(string FilePath, byte[] Image)
        {
            this.FilePath = FilePath;
            this.Image = Convert.ToBase64String(Image);
        }

        public PredictionRequest(string FilePath, string Image)
        {
            this.FilePath = FilePath;
            this.Image = Image;
        }
    }
}
