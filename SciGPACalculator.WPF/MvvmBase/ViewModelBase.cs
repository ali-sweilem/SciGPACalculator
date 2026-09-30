using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GpaCalculator.WPF.MvvmBase
{
    /// <summary>
    /// كلاس أساسي لأي ViewModel. بيوفر SetProperty اللي بتقلل التكرار:
    /// بدل ما تكتب "لو القيمة اتغيرت، حدّث الحقل، وبلّغ الواجهة" يدويًا في كل Property،
    /// بتستدعي السطر ده وخلاص.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
