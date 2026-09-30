using GpaCalculator.Core.Models;
using GpaCalculator.Core.Persistence;
using GpaCalculator.WPF.MvvmBase;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// الـ ViewModel الرئيسي اللي بيتحط كـ DataContext للنافذة كلها.
    /// مسؤول عن: تحميل البيانات وقت فتح البرنامج، حفظها وقت القفل،
    /// وتوصيل الصفحتين ببعض (لما تتغير بيانات صفحة الإدخال، صفحة الحساب تتحدث تلقائيًا).
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IAcademicRecordRepository _repository;

        public AcademicRecord Record { get; }

        public DataEntryViewModel DataEntry { get; }
        public DashboardViewModel Dashboard { get; }

        public MainViewModel(IAcademicRecordRepository repository)
        {
            _repository = repository;
            Record = _repository.Load();

            DataEntry = new DataEntryViewModel(Record);
            Dashboard = new DashboardViewModel(Record);

            DataEntry.DataChanged += (_, _) => Dashboard.Refresh();

            Dashboard.Refresh(); // حساب أولي لحظة فتح البرنامج، حتى لو مفيش تعديل حصل لسه
        }

        /// <summary>
        /// بتتنادى وقت قفل النافذة (شوف MainWindow.xaml.cs) - حفظ تلقائي بدون
        /// ما نحتاج زرار "Save" منفصل تشغله يدويًا.
        /// </summary>
        public void SaveAll()
        {
            _repository.Save(Record);
        }
    }
}
