using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace OptiX.Common
{
    /// <summary>
    /// ViewModel에서 사용하는 공통 헬퍼 클래스 모음
    /// 
    /// 포함된 클래스:
    /// - RelayCommand: 파라미터 없는 Command 구현
    /// - RelayCommand<T>: 파라미터 있는 Command 구현
    /// - GraphDataPoint: 그래프 데이터 포인트
    /// - JudgmentStatusUpdateEventArgs: 판정 현황 업데이트 이벤트 인자
    /// - GraphDisplayUpdateEventArgs: 그래프 표시 업데이트 이벤트 인자
    /// </summary>

    #region RelayCommand Classes

    /// <summary>
    /// 파라미터 없는 ICommand 구현 (MVVM 패턴용)
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();
    }

    /// <summary>
    /// 파라미터 있는 ICommand 구현 (MVVM 패턴용)
    /// </summary>
    /// <typeparam name="T">파라미터 타입</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;
            if (parameter == null) return false;
            
            if (parameter is T typedParam)
                return _canExecute(typedParam);
            
            return false;
        }

        public void Execute(object parameter)
        {
            if (parameter is T typedParam)
                _execute(typedParam);
        }
    }

    #endregion

    #region Graph Data Classes

    /// <summary>
    /// 그래프 데이터 포인트 (OPTIC/IPVS 공통)
    /// </summary>
    public class GraphDataPoint
    {
        public int ZoneNumber { get; set; }
        public string Judgment { get; set; }
        public DateTime Timestamp { get; set; }
        public int GlobalIndex { get; set; }  // 전역 인덱스 (0부터 시작)
    }

    #endregion

    #region Event Arguments

    /// <summary>
    /// 판정 현황 업데이트 이벤트 인자
    /// </summary>
    public class JudgmentStatusUpdateEventArgs : EventArgs
    {
        public string RowName { get; set; }
        public string Quantity { get; set; }
        public string Rate { get; set; }

        public JudgmentStatusUpdateEventArgs()
        {
        }

        public JudgmentStatusUpdateEventArgs(string rowName, string quantity, string rate)
        {
            RowName = rowName;
            Quantity = quantity;
            Rate = rate;
        }
    }

    /// <summary>
    /// 그래프 표시 업데이트 이벤트 인자
    /// </summary>
    public class GraphDisplayUpdateEventArgs : EventArgs
    {
        public List<GraphManager.GraphDataPoint> DataPoints { get; set; }

        public GraphDisplayUpdateEventArgs()
        {
        }

        public GraphDisplayUpdateEventArgs(List<GraphManager.GraphDataPoint> dataPoints)
        {
            DataPoints = dataPoints;
        }
    }

    #endregion
}

