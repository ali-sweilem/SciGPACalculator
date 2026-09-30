using System.ComponentModel;
using System.Windows.Input;
using GpaCalculator.Core.Models;
using GpaCalculator.WPF.MvvmBase;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// يلفّ Course واحد للعرض والتعديل في الـ DataGrid. أي تغيير هنا بيتفعّل عليه
    /// حدث Changed، اللي الـ SemesterViewModel بيسمعه ويمرره لفوق لحد ما يوصل
    /// لـ DataEntryViewModel.DataChanged، واللي بدوره بيخلي صفحة الحساب تعمل Refresh.
    /// </summary>
    public class CourseViewModel : ViewModelBase, IDataErrorInfo
    {
        public Course Model { get; }

        public event EventHandler? Changed;

        public ICommand DismissWarningCommand { get; }

        public CourseViewModel(Course model)
        {
            Model = model;
            DismissWarningCommand = new RelayCommand(() => ShowRetakeWarning = false);
        }

        private bool _showRetakeWarning;

        /// <summary>
        /// بترفع true لو المستخدم غيّر نسبة راسبة لنسبة ناجحة في نفس الصف بالظبط -
        /// ده بالضبط السيناريو اللي بيضيع فيه تاريخ الرسوب من غير أي تحذير، لأن الكود
        /// بيحسب الرسوب عن طريق مقارنة سجلات منفصلة، مش عن طريق تتبّع تعديلات الصف نفسه.
        /// </summary>
        public bool ShowRetakeWarning
        {
            get => _showRetakeWarning;
            private set => SetProperty(ref _showRetakeWarning, value);
        }

        public string CourseName
        {
            get => Model.CourseName;
            set
            {
                if (Model.CourseName == value) return;
                Model.CourseName = value;

                // الاسم هو المعرّف اللي بيربط محاولات نفس المادة ببعض في RetakePolicyResolver،
                // فبنخليه دايمًا متطابق مع CourseCode تلقائيًا - المستخدم مش محتاج يدخل كود منفصل.
                Model.CourseCode = value;

                OnPropertyChanged();
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public decimal CreditHours
        {
            get => Model.CreditHours;
            set
            {
                if (Model.CreditHours == value) return;
                Model.CreditHours = value;
                OnPropertyChanged();
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        public decimal? Percentage
        {
            get => Model.Percentage;
            set
            {
                if (Model.Percentage == value) return;

                var oldValue = Model.Percentage;
                Model.Percentage = value;

                // لو كانت راسبة قبل التعديل وبقت ناجحة بعده - في نفس الصف بالظبط -
                // ده بالضبط التعديل اللي بيضيع معلومة "الرسوب حصل قبل كده" من غير أي أثر
                if (oldValue.HasValue && !GradeScale.IsPassing(oldValue.Value) &&
                    value.HasValue && GradeScale.IsPassing(value.Value))
                {
                    ShowRetakeWarning = true;
                }

                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFailing));
                Changed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// مجرد مؤشر عرض بصري (لتلوين الصف بالأحمر في الواجهة) - مش بيتخزن ولا بيتحسب عليه حاجة،
        /// بيتحسب حيًا من النسبة الحالية.
        /// </summary>
        public bool IsFailing => Model.Percentage.HasValue && !GradeScale.IsPassing(Model.Percentage.Value);

        // --- IDataErrorInfo: لو رجّعنا رسالة هنا، الـ DataGrid يرفض يقبل القيمة أصلًا ---
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

                    case nameof(Percentage):
                        if (Percentage.HasValue && (Percentage < 0 || Percentage > 100))
                            return "النسبة لازم تكون بين 0 و100";
                        break;
                }

                return string.Empty;
            }
        }
    }
}
