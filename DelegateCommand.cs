using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Windows.Input;

namespace ApiDemo
{
    public class DelegateCommand : ICommand
    {
        readonly Action<object?>? m_execute;
        readonly Predicate<object?>? m_canExecute;

        public DelegateCommand(Action<object?> execute)
            : this(execute, null)
        {

        }

        public DelegateCommand(Action<object?>? execute, Predicate<object?>? canExecute)
        {
            Contract.Requires(execute != null);
            m_execute = execute;
            m_canExecute = canExecute;           
        }

        public bool CanExecute(object? parameter)
        {
            return m_canExecute == null ? true : m_canExecute(parameter);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object? parameter)
        {
            m_execute?.Invoke(parameter);
        }
    }
}
