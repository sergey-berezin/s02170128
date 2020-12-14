using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ImageRecognition;
using LibraryServer.DataBase;
using Contracts;

namespace LibraryServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PredictionController : ControllerBase
    {

        private readonly ILogger<PredictionController> _logger;
        private ILibraryDB dB;
        private OnnxClassifier clf;

        public PredictionController(ILogger<PredictionController> logger)
        {
            _logger = logger;
            this.dB = new InMemoryLibrary();
            this.clf = new OnnxClassifier(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\model\resnet50-v2-7.onnx");
        }

        [HttpPost("Old")]
        public ActionResult<Tuple<List<PredictionResponse>, List<PredictionRequest>>> SplitPost([FromBody] List<PredictionRequest> mpr)
        {
            var NewImages = dB.GetNewImages(mpr);
            var OldImages = dB.GetOldImages(mpr);
            return new ActionResult<Tuple<List<PredictionResponse>, List<PredictionRequest>>>(new Tuple<List<PredictionResponse>, List<PredictionRequest>>(OldImages, NewImages));
        }


        [HttpPost("New")]
        public ActionResult<List<PredictionResult>> GetNew([FromBody] List<PredictionRequest> mpr) // Base64
        {
            var cq = new PredictionQueue();
            clf.PredictAll(cq, mpr.Select(i => new Tuple<string, byte[]>(i.FilePath, Convert.FromBase64String(i.Image))).ToList());
            var res = cq.Queue.ToList();
            res.ForEach(delegate (PredictionResult pr) { dB.AddToDataBase(pr); });
            return new ActionResult<List<PredictionResult>>(cq.Queue.ToList());
        }



        [HttpGet]
        public ActionResult<List<Tuple<string, int>>> Get()
        {
            return new ActionResult<List<Tuple<string, int>>>(dB.GetStatistics());
        }

        [HttpDelete]
        public IActionResult ClearDataBase()
        {
            dB.ClearDataBase();
            return Ok();
        }
    }
}
