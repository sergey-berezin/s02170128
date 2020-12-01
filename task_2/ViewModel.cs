using System;
using System.Windows.Forms;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ImageRecognition;
using System.Windows.Threading;
using System.Windows.Data;
using System.Linq;
using System.Collections.ObjectModel;
using Contracts;

namespace task_2
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public CancellationTokenSource cts = new CancellationTokenSource();
        readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;

        public Contracts.ApplicationContext DataBaseContext { get; set; } = new Contracts.ApplicationContext();
        private string SERVER_URI = "http://localhost:5000/prediction";
        public LibraryClient client = new LibraryClient("http://localhost:5000/prediction");
        public DelegateCommand OpenCommand { protected set; get; }
        public DelegateCommand StopCommand { protected set; get; }
        public DelegateCommand ClearDataBaseCommand { protected set; get; }
        public DelegateCommand GetStatsCommand { protected set; get; }
        public ObservableCollection<ModelPrediction> ObservableModelPrediction { get; set; }
        public ICollectionView FilteredObservableModelPrediction { get; set; }
        public ObservableCollection<Tuple<string, int>> AvailableClasses { get; set; }
        private PredictionQueue cq { get; set; }
        

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

        private void AddPrecomputedPrediction(ModelPrediction mp)
        {
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

        private void AddPrediction(PredictionResult pr)
        {
            dispatcher.BeginInvoke(new Action(() =>
            {
                ObservableModelPrediction.Add(new ModelPrediction(pr));
                var buf = AvailableClasses.Where(x => x.Item1 == pr.ClassName).FirstOrDefault();

                if (buf != null)
                {
                    AvailableClasses[AvailableClasses.IndexOf(buf)] = new Tuple<string, int>(buf.Item1, buf.Item2 + 1);
                }
                else
                {
                    AvailableClasses.Add(new Tuple<string, int>(pr.ClassName, 1));
                }
            }));
        }

        private async void ExecuteOpen(object param)
        {
            ObservableModelPrediction.Clear();
            AvailableClasses.Clear();
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    IsRunning = true;
                    (var OldImages, var NewImages) = await client.PostOld(fbd.SelectedPath, cts);
                    OldImages.ForEach(delegate (PredictionResponse prs) { AddPrecomputedPrediction(new ModelPrediction(prs.ClassName,prs.Proba,prs.FilePath,Convert.FromBase64String(prs.Image))); });
                    var NewImagesResults = await client.GetNew(NewImages, cts);
                    NewImagesResults.ForEach(delegate (PredictionResult pr) { AddPrediction(pr); });
                    IsRunning = false;
                }
                catch(TaskCanceledException tce)
                {
                    MessageBox.Show("Tasks were cancelled");
                }
                catch (Exception e)
                {
                    MessageBox.Show("Prediction failed!");
                }
            }
        }

        private bool CanExecuteOpen(object param)
        {
            return !IsRunning;
        }

        private async void ExecuteStop(object param)
        {
            cts.Cancel(false);
            cts.Dispose();
            cts = new CancellationTokenSource();
            IsRunning = false;
        }

        private bool CanExecuteStop(object param)
        {
            return IsRunning;
        }

        private void ExecuteClear(object param)
        {
            ObservableModelPrediction.Clear();
            AvailableClasses.Clear();
            try
            {
                client.Delete();
            }
            catch (Exception e)
            {
                MessageBox.Show("Clearing DataBase failed!");
            }
        }

        private bool CanExecuteClear(object param)
        {
            return !IsRunning;
        }

        private async void ExecuteGetStats(object param)
        {
            try
            {
                var stats = await client.GetStats();
                MessageBox.Show(string.Join(Environment.NewLine, stats));
            }
            catch (Exception e)
            {
                MessageBox.Show("Gettings stats failed!");
            }
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

            this.OpenCommand = new DelegateCommand(ExecuteOpen, CanExecuteOpen);
            this.StopCommand = new DelegateCommand(ExecuteStop, CanExecuteStop);
            this.ClearDataBaseCommand = new DelegateCommand(ExecuteClear, CanExecuteClear);
            this.GetStatsCommand = new DelegateCommand(ExecuteGetStats);
        }
    }
}
