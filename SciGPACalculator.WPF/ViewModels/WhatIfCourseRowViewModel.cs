using System.ComponentModel;
using GpaCalculator.WPF.MvvmBase;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// صف واحد في جدول الـ What-If: مادة افتراضية بدرجة متوقعة، لسه معندهاش نتيجة حقيقية.
    /// </summary>
    public class WhatIfCourseRowViewModel : ViewModelBase, IDataErrorInfo
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

        private decimal _hypotheticalPercentage;
        public decimal HypotheticalPercentage
        {
            get => _hypotheticalPercentage;
            set => SetProperty(ref _hypotheticalPercentage, value);
        }

        public string Error => string.Empty;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(CreditHours):
                        if (CreditHours <= 0)
                            return "الساعات المعتمدة لازم تكون أكبر من صفر";
                        break;

                    case nameof(HypotheticalPercentage):
                        if (HypotheticalPercentage < 0 || HypotheticalPercentage > 100)
                            return "النسبة لازم تكون بين 0 و100";
                        break;
                }

                return string.Empty;
            }
        }
    }
}
