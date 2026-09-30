using System.ComponentModel;
using GpaCalculator.WPF.MvvmBase;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// صف واحد في جدول "المقررات المتبقية" لأداة Target GPA. مفيش حقل درجة هنا -
    /// الدرجة المطلوبة هي بالظبط اللي الأداة بتحسبها.
    /// </summary>
    public class RemainingCourseRowViewModel : ViewModelBase, IDataErrorInfo
    {
        private string _courseName = string.Empty;
        public string CourseName
        {
            get => _courseName;
            set => SetProperty(ref _courseName, value);
        }

        private decimal _creditHours = 3;
        public decimal CreditHours
        {
            get => _creditHours;
            set => SetProperty(ref _creditHours, value);
        }

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(CreditHours) && CreditHours <= 0)
                    return "الساعات المعتمدة لازم تكون أكبر من صفر";

                return string.Empty;
            }
        }
    }
}
