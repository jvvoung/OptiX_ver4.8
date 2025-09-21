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
            set => SetProperty(ref category, value);
        }

        public string X
        {
            get => x;
            set => SetProperty(ref x, value);
        }

        public string Y
        {
            get => y;
            set => SetProperty(ref y, value);
        }

        public string L
        {
            get => l;
            set => SetProperty(ref l, value);
        }

        public string Current
        {
            get => current;
            set => SetProperty(ref current, value);
        }

        public string Efficiency
        {
            get => efficiency;
            set => SetProperty(ref efficiency, value);
        }

        public string Zone
        {
            get => zone;
            set => SetProperty(ref zone, value);
        }

        public string InnerId
        {
            get => innerId;
            set => SetProperty(ref innerId, value);
        }

        public string CellId
        {
            get => cellId;
            set => SetProperty(ref cellId, value);
        }

        public string ErrorName
        {
            get => errorName;
            set => SetProperty(ref errorName, value);
        }

        public string Tact
        {
            get => tact;
            set => SetProperty(ref tact, value);
        }

        public string Judgment
        {
            get => judgment;
            set => SetProperty(ref judgment, value);
        }

        public bool IsFirstInGroup { get; set; }
        public int GroupSize { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

