using System.Collections.ObjectModel;
using System.Windows.Input;
using GpaCalculator.Core.Models;
using GpaCalculator.Core.Services;
using GpaCalculator.WPF.MvvmBase;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// مسؤول عن قسم "توقع مستقبلي (What-If)" في صفحة الحساب.
    /// بيقرأ بيانات الطالب الحقيقية وقت الضغط على "احسب" بس (مش بيحتفظ بنسخة قديمة منها)،
    /// فمضمون دايمًا إنه بيحاكي على أحدث بيانات موجودة.
    /// </summary>
    public class WhatIfViewModel : ViewModelBase
    {
        private readonly AcademicRecord _record;

        public ObservableCollection<WhatIfCourseRowViewModel> Rows { get; } = new();
        public ObservableCollection<string> Warnings { get; } = new();

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }
        public ICommand SimulateCommand { get; }

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

        private decimal? _predictedCgpa;
        public decimal? PredictedCgpa
        {
            get => _predictedCgpa;
            private set
            {
                SetProperty(ref _predictedCgpa, value);
                OnPropertyChanged(nameof(PredictedCgpaDisplay));
            }
        }

        public string CurrentCgpaDisplay => CurrentCgpa.HasValue ? CurrentCgpa.Value.ToString("F2") : "-";
        public string PredictedCgpaDisplay => PredictedCgpa.HasValue ? PredictedCgpa.Value.ToString("F2") : "-";

        public WhatIfViewModel(AcademicRecord record)
        {
            _record = record;

            AddRowCommand = new RelayCommand(() => Rows.Add(new WhatIfCourseRowViewModel()));
            RemoveRowCommand = new RelayCommand(param =>
            {
                if (param is WhatIfCourseRowViewModel row) Rows.Remove(row);
            });
            SimulateCommand = new RelayCommand(Simulate);
        }

        private void Simulate()
        {
            var inputs = Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.CourseName))
                .Select(r => new WhatIfCourseInput
                {
                    // بنستخدم الاسم نفسه كـ CourseCode، بنفس منطق صفحة إدخال البيانات،
                    // علشان لو المادة دي نفسها موجودة كرسوب حقيقي، الـ Resolver يتعرف عليها
                    CourseCode = r.CourseName,
                    CourseName = r.CourseName,
                    CreditHours = r.CreditHours,
                    HypotheticalPercentage = r.HypotheticalPercentage
                });

            var result = WhatIfService.Simulate(_record.Semesters, inputs);

            CurrentCgpa = result.CurrentCgpa;
            PredictedCgpa = result.PredictedCgpa;

            Warnings.Clear();
            foreach (var warning in result.Warnings)
                Warnings.Add(warning);
        }
    }
}
