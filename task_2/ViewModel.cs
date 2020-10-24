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

namespace task_2
{
    public class ViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        public DelegateCommand OpenCommand { protected set; get; }
        public DelegateCommand StopCommand { protected set; get; }
        public ObservableCollection<ModelPrediction> ObservableModelPrediction { get; set; }
        public ICollectionView FilteredObservableModelPrediction { get; set; }
        public ObservableCollection<Tuple<string, int>> AvailableClasses { get; set; }
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
                this.IsRunning = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(param =>
                {
                    clf.PredictAll(cq, fbd.SelectedPath);
                    this.dispatcher.BeginInvoke(new Action(() => { this.IsRunning = false; }));
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
        }
    }
}
