using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ImageRecognition;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Data;
using System.Linq;
using System.Collections.ObjectModel;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;

namespace task_2
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        public ApplicationContext DataBaseContext { get; set; } = new ApplicationContext();
        public DelegateCommand OpenCommand { protected set; get; }
        public DelegateCommand StopCommand { protected set; get; }
        public DelegateCommand ClearDataBaseCommand { protected set; get; }
        public ObservableCollection<ModelPrediction> ObservableModelPrediction { get; set; }
        public ICollectionView FilteredObservableModelPrediction { get; set; }
        public ObservableCollection<Tuple<string, int>> AvailableClasses { get; set; }
        public ObservableCollection<Tuple<string, int>> Statistics { 
            get
            {
                var query = from img in DataBaseContext.Images
                            group img by img.ImageClassID into g
                            select new { name = DataBaseContext.Classes.FirstOrDefault(p => p.ImageClassID == g.Key).ClassName, count = g.Count() };
                return new ObservableCollection<Tuple<string, int>>(query.Select(c => new Tuple<string, int>(c.name, c.count)).ToList());
            }}
        private PredictionQueue cq { get; set; }
        private OnnxClassifier clf { get; set; }

        private bool isRunning = false;
        public bool IsRunning
        {
            get
            {
                return isRunning;
            }
            set
            {
                isRunning = value;
                OpenCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
                ClearDataBaseCommand.RaiseCanExecuteChanged();
            }
        }

        private Tuple<string, int> selectedCLass;
        public Tuple<string, int> SelectedClass
        {
            get
            {
                return selectedCLass;
            }
            set
            {
                selectedCLass = value;
                if (value != null)
                {
                    FilteredObservableModelPrediction.Refresh();
                }
            }
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void AddToDataBase(PredictionResult pr)
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
            NotifyPropertyChanged("Statistics");
        }

        private void PredictionCaught(object sender, PredictionEventArgs e)
        {
            dispatcher.BeginInvoke(new Action(() =>
            {
                ObservableModelPrediction.Add(new ModelPrediction(e.PredictionResult));
                var buf = AvailableClasses.Where(x => x.Item1 == e.PredictionResult.ClassName).FirstOrDefault(); 

                if (buf != null) {
                    AvailableClasses[AvailableClasses.IndexOf(buf)] = new Tuple<string, int>(buf.Item1, buf.Item2 + 1);
                } else
                {
                    AvailableClasses.Add(new Tuple<string, int>(e.PredictionResult.ClassName, 1));
                }
                AddToDataBase(e.PredictionResult);
            }));
        }

        private void AddPrecomputedPrediction(DbImage di)
        {
            var mp = new ModelPrediction(DataBaseContext.Classes.ToList().Find(p => p.ImageClassID == di.ImageClassID).ClassName, di.Proba, di.FilePath, di.ImageData);
            dispatcher.BeginInvoke(new Action(() =>
            {
                ObservableModelPrediction.Add(mp);
                var buf = AvailableClasses.Where(x => x.Item1 == mp.ClassName).FirstOrDefault();

                if (buf != null)
                {
                    AvailableClasses[AvailableClasses.IndexOf(buf)] = new Tuple<string, int>(buf.Item1, buf.Item2 + 1);
                }
                else
                {
                    AvailableClasses.Add(new Tuple<string, int>(mp.ClassName, 1));
                }
            }));
        }

        private void ExecuteOpen(object param)
        {
            ObservableModelPrediction.Clear();
            AvailableClasses.Clear();
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                this.clf = new OnnxClassifier(Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.FullName + @"\model\resnet50-v2-7.onnx");
                ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
                {
                    DirectoryInfo d = new DirectoryInfo(fbd.SelectedPath);
                    FileInfo[] Files = d.GetFiles("*.jpg");
                    var NewImages = Files.Where(p => !DataBaseContext.Images.AsEnumerable().Any(p2 => p2.FilePath == p.FullName && p2.ImageData.SequenceEqual(File.ReadAllBytes(p.FullName)))).ToArray();
                    var OldImages = DataBaseContext.Images.AsEnumerable().Where(p => Files.Any(p2 => p2.FullName == p.FilePath && p.ImageData.SequenceEqual(File.ReadAllBytes(p2.FullName)))).ToArray();

                    foreach(var i in OldImages)
                    {
                        AddPrecomputedPrediction(i);
                    }

                    if (NewImages.Length > 0)
                    {
                        this.dispatcher.BeginInvoke(new Action(() => { this.IsRunning = true; }));
                        clf.PredictAll(cq, NewImages);
                        this.dispatcher.BeginInvoke(new Action(() => { this.IsRunning = false; DataBaseContext.SaveChanges();
                        }));
                    }
                    
                }));
            }
        }

        private bool CanExecuteOpen(object param)
        {
            return !IsRunning;
        }

        private void ExecuteStop(object param)
        {
            clf.StopPrediction();
        }

        private bool CanExecuteStop(object param)
        {
            return IsRunning;
        }

        private void ExecuteClear(object param)
        {
            ObservableModelPrediction.Clear();
            AvailableClasses.Clear();
            DataBaseContext.Database.ExecuteSqlCommand("delete from Images");
            DataBaseContext.Database.ExecuteSqlCommand("delete from Classes");
            NotifyPropertyChanged("Statistics");
        }

        private bool CanExecuteClear(object param)
        {
            return !IsRunning;
        }

        public ViewModel()
        {
            this.ObservableModelPrediction = new ObservableCollection<ModelPrediction>();
            ObservableModelPrediction.CollectionChanged += ((sender, e) => { NotifyPropertyChanged("AvailableClasses"); });
            this.FilteredObservableModelPrediction = CollectionViewSource.GetDefaultView(ObservableModelPrediction);
            this.FilteredObservableModelPrediction.Filter = delegate (object item)
            {
                ModelPrediction tmp = item as ModelPrediction;
                if (SelectedClass == null)
                    return false;
                return (tmp.ClassName == SelectedClass.Item1);
            };
            this.AvailableClasses = new ObservableCollection<Tuple<string, int>>();
            
            this.cq = new PredictionQueue();
            cq.Enqueued += PredictionCaught;

            this.OpenCommand = new DelegateCommand(ExecuteOpen, CanExecuteOpen);
            this.StopCommand = new DelegateCommand(ExecuteStop, CanExecuteStop);
            this.ClearDataBaseCommand = new DelegateCommand(ExecuteClear, CanExecuteClear);
        }
    }
}
