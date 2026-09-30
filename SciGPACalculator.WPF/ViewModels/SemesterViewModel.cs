using System.Collections.ObjectModel;
using System.Windows.Input;
using GpaCalculator.Core.Models;
using GpaCalculator.WPF.MvvmBase;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// يلفّ Semester واحد. مسؤول عن إدارة قايمة مقرراته (إضافة/حذف)، وعن تمرير أي تغيير
    /// جوه أي مقرر فيه (أو إضافة/حذف مقرر) لفوق عن طريق حدث Changed.
    /// </summary>
    public class SemesterViewModel : ViewModelBase
    {
        public Semester Model { get; }

        public ObservableCollection<CourseViewModel> Courses { get; } = new();

        public event EventHandler? Changed;

        public ICommand AddCourseCommand { get; }
        public ICommand RemoveCourseCommand { get; }

        public SemesterViewModel(Semester model)
        {
            Model = model;

            foreach (var course in model.Courses)
            {
                AttachCourse(course);
            }

            AddCourseCommand = new RelayCommand(AddCourse);
            RemoveCourseCommand = new RelayCommand(param => RemoveCourse(param as CourseViewModel));
        }

        public string Name
        {
            get => Model.Name;
            set
            {
                if (Model.Name == value) return;
                Model.Name = value;
                OnPropertyChanged();
                RaiseChanged();
            }
        }

        private void AttachCourse(Course course)
        {
            var vm = new CourseViewModel(course);
            vm.Changed += (_, _) => RaiseChanged();
            Courses.Add(vm);
        }

        private void AddCourse()
        {
            var course = new Course
            {
                CreditHours = 3, // قيمة افتراضية شائعة، المستخدم يقدر يغيّرها فورًا
                SemesterId = Model.SemesterId
            };

            Model.Courses.Add(course);
            AttachCourse(course);
            RaiseChanged();
        }

        private void RemoveCourse(CourseViewModel? vm)
        {
            if (vm == null) return;

            Model.Courses.Remove(vm.Model);
            Courses.Remove(vm);
            RaiseChanged();
        }

        private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
    }
}
