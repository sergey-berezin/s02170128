using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using System.Runtime.CompilerServices;

namespace task_2
{
    public class ObservableModelPrediction : ObservableCollection<ModelPrediction>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        public ObservableModelPrediction() {
            base.CollectionChanged += ((sender, e) => { NotifyPropertyChanged("AvailableClasses"); });
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        private List<string> availableClasses;
        public List<string> AvailableClasses
        {
            get { return base.Items.Select(x => x.ClassName).Distinct().ToList(); }
            set { availableClasses = value; }
        }

    }
}
