namespace Contracts
{
    public class PredictionResponse : PredictionRequest
    {
        private string className;
        public string ClassName { get { return className; } set { className = value; } }
        private float proba;
        public float Proba { get { return proba; } set { proba = value; } }

        public PredictionResponse() { }
        public PredictionResponse(PredictionRequest prq, string ClassName, float Proba) : base(prq.FilePath, prq.Image)
        {
            this.ClassName = ClassName;
            this.Proba = Proba;
        }
    }
}
