using System.Windows.Input;

namespace GpaCalculator.WPF.MvvmBase
{
    /// <summary>
    /// تنفيذ قياسي لـ ICommand، يُستخدم لربط الأزرار في الـ XAML بأفعال في الـ ViewModel
    /// من غير ما نكتب Event Handlers في الـ Code-Behind (ده بيكسر مبدأ MVVM).
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Overload مريح للحالة الأشيع: أمر من غير Parameter.
        /// </summary>
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(), canExecute == null ? null : _ => canExecute())
        {
        }

        /// <summary>
        /// الربط بـ CommandManager.RequerySuggested بيخلي WPF يعيد تقييم CanExecute تلقائيًا
        /// مع أي تفاعل على الواجهة (كليك، تنقل بالـ Tab...)، من غير ما نستدعي
        /// RaiseCanExecuteChanged يدويًا في كل مكان بنغيّر فيه حالة تسمح/تمنع تنفيذ الأمر.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}
