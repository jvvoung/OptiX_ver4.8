using System.ComponentModel;

namespace OptiX.Models
{
    public class DataTableItem : INotifyPropertyChanged
    {
        private string category;
        private string x;
        private string y;
        private string l;
        private string current;
        private string efficiency;
        private string zone;
        private string innerId;
        private string cellId;
        private string errorName;
        private string tact;
        private string judgment;

        public string Category
        {
            get => category;
            set => SetProperty(ref category, value, "Category");
        }

        public string X
        {
            get => x;
            set => SetProperty(ref x, value, "X");
        }

        public string Y
        {
            get => y;
            set => SetProperty(ref y, value, "Y");
        }

        public string L
        {
            get => l;
            set => SetProperty(ref l, value, "L");
        }

        public string Current
        {
            get => current;
            set => SetProperty(ref current, value, "Current");
        }

        public string Efficiency
        {
            get => efficiency;
            set => SetProperty(ref efficiency, value, "Efficiency");
        }

        public string Zone
        {
            get => zone;
            set => SetProperty(ref zone, value, "Zone");
        }

        public string InnerId
        {
            get => innerId;
            set => SetProperty(ref innerId, value, "InnerId");
        }

        public string CellId
        {
            get => cellId;
            set => SetProperty(ref cellId, value, "CellId");
        }

        public string ErrorName
        {
            get => errorName;
            set => SetProperty(ref errorName, value, "ErrorName");
        }

        public string Tact
        {
            get => tact;
            set => SetProperty(ref tact, value, "Tact");
        }

        public string Judgment
        {
            get => judgment;
            set => SetProperty(ref judgment, value, "Judgment");
        }

        public bool IsFirstInGroup { get; set; }
        public int GroupSize { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

