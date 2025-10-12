using System.ComponentModel;

namespace OptiX.Common
{
    public class DataTableItem : INotifyPropertyChanged
    {
        private string category;
        private string point;
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

        public string Point
        {
            get => point;
            set => SetProperty(ref point, value, "Point");
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
            {
                // UI 스레드 확인 후 PropertyChanged 이벤트 발생
                if (System.Windows.Application.Current != null)
                {
                    var dispatcher = System.Windows.Application.Current.Dispatcher;
                    
                    // 이미 UI 스레드라면 직접 호출
                    if (dispatcher.CheckAccess())
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                    }
                    else
                    {
                        // UI 스레드가 아니라면 Invoke로 동기 호출
                        dispatcher.Invoke(() =>
                        {
                            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                        });
                    }
                }
                else
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }
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

