using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using GpaCalculator.Core.Models;
using GpaCalculator.Core.Services;
using GpaCalculator.WPF.MvvmBase;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// مسؤول عن قسم "المعدل المستهدف (Target GPA)" في صفحة الحساب.
    /// </summary>
    public class TargetGpaViewModel : ViewModelBase, IDataErrorInfo
    {
        private readonly AcademicRecord _record;

        public ObservableCollection<RemainingCourseRowViewModel> Rows { get; } = new();
        public ObservableCollection<string> Messages { get; } = new();

        public ICommand AddRowCommand { get; }
        public ICommand RemoveRowCommand { get; }
        public ICommand CalculateCommand { get; }

        private decimal _targetCgpa = 3.0m;
        public decimal TargetCgpa
        {
            get => _targetCgpa;
            set => SetProperty(ref _targetCgpa, value);
        }

        private decimal? _requiredAveragePoints;
        public decimal? RequiredAveragePoints
        {
            get => _requiredAveragePoints;
            private set
            {
                SetProperty(ref _requiredAveragePoints, value);
                OnPropertyChanged(nameof(RequiredAverageDisplay));
            }
        }

        public string RequiredAverageDisplay =>
            RequiredAveragePoints.HasValue ? RequiredAveragePoints.Value.ToString("F2") : "-";

        private string? _suggestedGrade;
        public string? SuggestedGrade
        {
            get => _suggestedGrade;
            private set => SetProperty(ref _suggestedGrade, value);
        }

        public TargetGpaViewModel(AcademicRecord record)
        {
            _record = record;

            AddRowCommand = new RelayCommand(() => Rows.Add(new RemainingCourseRowViewModel()));
            RemoveRowCommand = new RelayCommand(param =>
            {
                if (param is RemainingCourseRowViewModel row) Rows.Remove(row);
            });
            CalculateCommand = new RelayCommand(Calculate);
        }

        private void Calculate()
        {
            var inputs = Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.CourseName))
                .Select(r => new RemainingCourseInput
                {
                    CourseCode = r.CourseName,
                    CourseName = r.CourseName,
                    CreditHours = r.CreditHours
                });

            var result = TargetGpaService.CalculateRequiredAverage(_record.Semesters, TargetCgpa, inputs);

            RequiredAveragePoints = result.RequiredAveragePoints;
            SuggestedGrade = result.SuggestedMinimumGrade;

            Messages.Clear();
            foreach (var message in result.Messages)
                Messages.Add(message);
        }

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                if (columnName == nameof(TargetCgpa) &&
                    (TargetCgpa < 0 || TargetCgpa > GradeScale.MaxPossiblePoints))
                {
                    return $"الهدف لازم يكون بين 0 و{GradeScale.MaxPossiblePoints:F1}";
                }

                return string.Empty;
            }
        }
    }
}
