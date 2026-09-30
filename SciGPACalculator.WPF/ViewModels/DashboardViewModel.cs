using System.Collections.ObjectModel;
using GpaCalculator.Core.Models;
using GpaCalculator.Core.Services;
using GpaCalculator.WPF.MvvmBase;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// ViewModel الخاص بالصفحة الرئيسية (الصفحة 1). مش بيعمل حسابات بنفسه -
    /// بيستدعي GpaCalculatorService مباشرة للـ CGPA، وبيملك WhatIfViewModel
    /// و TargetGpaViewModel اللي كل واحد فيهم مسؤول عن قسمه لوحده.
    /// </summary>
    public class DashboardViewModel : ViewModelBase
    {
        private readonly AcademicRecord _record;

        public WhatIfViewModel WhatIf { get; }
        public TargetGpaViewModel TargetGpa { get; }

        public ObservableCollection<SemesterSummaryViewModel> SemesterSummaries { get; } = new();

        private decimal? _currentCgpa;
        public decimal? CurrentCgpa
        {
            get => _currentCgpa;
            private set
            {
                SetProperty(ref _currentCgpa, value);
                OnPropertyChanged(nameof(CurrentCgpaDisplay));
            }
        }

        public string CurrentCgpaDisplay => CurrentCgpa.HasValue ? CurrentCgpa.Value.ToString("F2") : "لا توجد بيانات بعد";

        public DashboardViewModel(AcademicRecord record)
        {
            _record = record;
            WhatIf = new WhatIfViewModel(record);
            TargetGpa = new TargetGpaViewModel(record);
        }

        /// <summary>
        /// بتتنادى من MainViewModel لما بيانات صفحة الإدخال تتغيّر، وكمان مرة واحدة
        /// وقت فتح البرنامج. بتعيد حساب الـ CGPA الحالي وملخص كل فصل من جديد بالكامل.
        /// </summary>
        public void Refresh()
        {
            CurrentCgpa = GpaCalculatorService.CalculateCumulativeGpa(_record.Semesters);

            SemesterSummaries.Clear();
            foreach (var semester in _record.Semesters.OrderBy(s => s.Order))
            {
                var gpa = GpaCalculatorService.CalculateSemesterGpa(semester);
                SemesterSummaries.Add(new SemesterSummaryViewModel(semester.Name, gpa));
            }
        }
    }
}
