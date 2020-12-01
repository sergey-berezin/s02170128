using ImageRecognition;
using System;
using System.Collections.Generic;
using System.Linq;
using Contracts;
using Microsoft.EntityFrameworkCore;

namespace LibraryServer.DataBase
{
    public interface ILibraryDB
    {
        List<Tuple<string, int>> GetStatistics();
        void ClearDataBase();
        void AddToDataBase(PredictionResult pr);
        public List<PredictionRequest> GetNewImages(List<PredictionRequest> mpr);
        public List<PredictionResponse> GetOldImages(List<PredictionRequest> mpr);
    }
    public class InMemoryLibrary : ILibraryDB
    {
        public InMemoryLibrary() { }
        private ApplicationContext DataBaseContext = new ApplicationContext();
        public List<Tuple<string, int>> GetStatistics()
        {
            var DataBaseContext = new ApplicationContext();
            var query = from img in DataBaseContext.Images
                        group img by img.ImageClassID into g
                        select new { name = DataBaseContext.Classes.FirstOrDefault(p => p.ImageClassID == g.Key).ClassName, count = g.Count() };

            return query.Select(c => new Tuple<string, int>(c.name, c.count)).ToList();
        }

        public void ClearDataBase()
        {
            foreach (var item in DataBaseContext.Images)
            {
                DataBaseContext.Images.Remove(item);
            }

            foreach (var item in DataBaseContext.Classes)
            {
                DataBaseContext.Classes.Remove(item);
            }

            foreach (var item in DataBaseContext.Details)
            {
                DataBaseContext.Details.Remove(item);
            }
            DataBaseContext.SaveChanges();
        }

        public void AddToDataBase(PredictionResult pr)
        {
            var tmp = new DbImage(pr);
            var ImageClass = DataBaseContext.Classes.Where(p => p.ClassName == pr.ClassName).FirstOrDefault();
            if (ImageClass is null)
            {
                ImageClass = new DbImageClass(pr.ClassName);
                DataBaseContext.Classes.Add(ImageClass);
            }
            tmp.ImageClassID = ImageClass.ImageClassID;
            ImageClass.Images.Add(tmp);
            DataBaseContext.SaveChanges();
        }

        public List<PredictionRequest> GetNewImages(List<PredictionRequest> mpr) {
            List<PredictionRequest> NewImages = new List<PredictionRequest>();

            var tmp = DataBaseContext.Images.Include(p => p.ImageDetails);

         
                foreach (var item in mpr)
                {
                    if (!tmp.Any(p => p.FilePath == item.FilePath && p.ImageDetails.ImageData.SequenceEqual(Convert.FromBase64String(item.Image))))
                    {
                        NewImages.Add(item);
                    }
                }
            return NewImages;
        }

        public List<PredictionResponse> GetOldImages(List<PredictionRequest> mpr)
        {
            List<PredictionResponse> OldImages = new List<PredictionResponse>();

            var tmp = DataBaseContext.Images.Include(p => p.ImageDetails).Include(p => p.ImageClass);
            foreach (var item in mpr)
            {
                if (tmp.Any(p => p.FilePath == item.FilePath && p.ImageDetails.ImageData.SequenceEqual(Convert.FromBase64String(item.Image))))
                {
                    var buf = tmp.FirstOrDefault(p => p.FilePath == item.FilePath && p.ImageDetails.ImageData.SequenceEqual(Convert.FromBase64String(item.Image)));
                    OldImages.Add(new PredictionResponse(item, buf.ImageClass.ClassName, buf.Proba));

                }
            }

            return OldImages;
        }

    }
}
