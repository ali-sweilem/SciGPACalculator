using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using GpaCalculator.Core.Models;
using GpaCalculator.Core.Services;
using GpaCalculator.WPF.MvvmBase;
using Microsoft.Win32;

namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// ViewModel الخاص بصفحة إدخال البيانات (الصفحة 2).
    /// بيدير قايمة الفصول (إضافة/حذف)، وبيسمع لأي تغيير جوه أي فصل أو مقرر تابع له،
    /// وبيبلّغ فوق (لـ MainViewModel) عن طريق DataChanged - اللي بيخلي صفحة الحساب تتحدث.
    /// </summary>
    public class DataEntryViewModel : ViewModelBase
    {
        private readonly AcademicRecord _record;

        public ObservableCollection<SemesterViewModel> Semesters { get; } = new();

        public event EventHandler? DataChanged;

        public ICommand AddSemesterCommand { get; }
        public ICommand RemoveSemesterCommand { get; }
        public ICommand ImportCsvCommand { get; }

        public DataEntryViewModel(AcademicRecord record)
        {
            _record = record;

            foreach (var semester in _record.Semesters.OrderBy(s => s.Order))
            {
                AttachSemester(semester);
            }

            // بنسمع لأي إضافة/حذف في القايمة نفسها، علشان HasNoSemesters يفضل متزامن
            // مع عدد الفصول الفعلي من غير ما نحتاج ننادي عليه يدويًا في كل مكان
            Semesters.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasNoSemesters));

            AddSemesterCommand = new RelayCommand(AddSemester);
            RemoveSemesterCommand = new RelayCommand(param => RemoveSemester(param as SemesterViewModel));
            ImportCsvCommand = new RelayCommand(ImportFromCsv);
        }

        /// <summary>
        /// بتستخدم في الواجهة لإظهار رسالة توجيهية لما الطالب يفتح صفحة الإدخال لأول مرة.
        /// </summary>
        public bool HasNoSemesters => Semesters.Count == 0;

        private void AttachSemester(Semester semester)
        {
            var vm = new SemesterViewModel(semester);
            vm.Changed += (_, _) => RaiseDataChanged();
            Semesters.Add(vm);
        }

        private void AddSemester()
        {
            var nextId = GetNextSemesterId();

            var semester = new Semester
            {
                SemesterId = nextId,
                Name = $"الفصل {nextId}",
                Order = nextId
            };

            _record.Semesters.Add(semester);
            AttachSemester(semester);
            RaiseDataChanged();
        }

        /// <summary>
        /// ترقيم تلقائي بسيط: أعلى SemesterId موجود + 1، علشان نضمن عدم التكرار
        /// حتى لو المستخدم حذف فصول من النص وأعاد الإضافة. مشترك بين AddSemester
        /// والاستيراد من CSV، لأن الاتنين محتاجين نفس الضمان.
        /// </summary>
        private int GetNextSemesterId() =>
            _record.Semesters.Count == 0 ? 1 : _record.Semesters.Max(s => s.SemesterId) + 1;

        private void ImportFromCsv()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "ملفات CSV (*.csv)|*.csv",
                Title = "استيراد ملف الدرجات"
            };

            if (dialog.ShowDialog() != true) return;

            string content;
            try
            {
                content = File.ReadAllText(dialog.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذّر قراءة الملف:\n{ex.Message}", "خطأ في القراءة",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var importResult = CsvImportService.Import(content);

            foreach (var importedSemester in importResult.ImportedSemesters)
            {
                // لو فيه فصل بنفس الاسم بالفعل، ضيف المقررات له بدل ما نعمل فصل مكرر
                var existing = _record.Semesters.FirstOrDefault(s => s.Name == importedSemester.Name);

                if (existing != null)
                {
                    foreach (var course in importedSemester.Courses)
                    {
                        course.SemesterId = existing.SemesterId;
                        existing.Courses.Add(course);
                    }
                }
                else
                {
                    var newId = GetNextSemesterId();
                    importedSemester.SemesterId = newId;
                    importedSemester.Order = newId;
                    foreach (var course in importedSemester.Courses)
                        course.SemesterId = newId;

                    _record.Semesters.Add(importedSemester);
                }
            }

            RebuildFromRecord();
            RaiseDataChanged();

            var message = $"تم استيراد {importResult.RowsImported} مقرر بنجاح.";
            if (importResult.Errors.Any())
            {
                var shown = importResult.Errors.Take(10).ToList();
                message += $"\n\nتحذيرات ({importResult.Errors.Count}):\n" + string.Join("\n", shown);
                if (importResult.Errors.Count > shown.Count)
                    message += $"\n... و{importResult.Errors.Count - shown.Count} تحذير إضافي.";
            }

            MessageBox.Show(message, "نتيجة الاستيراد", MessageBoxButton.OK,
                importResult.Errors.Any() ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }

        /// <summary>
        /// بعد الاستيراد، أسهل وأضمن نعيد بناء كل ViewModels الفصول من الـ Model الحالي
        /// بالكامل، بدل ما نحاول نحدّث الـ ObservableCollection الموجودة جزئيًا بدقة.
        /// </summary>
        private void RebuildFromRecord()
        {
            Semesters.Clear();
            foreach (var semester in _record.Semesters.OrderBy(s => s.Order))
            {
                AttachSemester(semester);
            }
        }

        private void RemoveSemester(SemesterViewModel? vm)
        {
            if (vm == null) return;

            var coursesCount = vm.Courses.Count;
            var message = coursesCount > 0
                ? $"هيتم حذف \"{vm.Name}\" وكل المقررات اللي جواه ({coursesCount} مقرر) نهائيًا، ومينفعش ترجّعهم بعد كده.\n\nمتأكد؟"
                : $"هيتم حذف \"{vm.Name}\" نهائيًا. متأكد؟";

            var confirmed = MessageBox.Show(
                message,
                "تأكيد حذف الفصل",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No); // الافتراضي "لأ" عمدًا، علشان ضغطة Enter بالغلط ميحذفش حاجة

            if (confirmed != MessageBoxResult.Yes) return;

            _record.Semesters.Remove(vm.Model);
            Semesters.Remove(vm);
            RaiseDataChanged();
        }

        private void RaiseDataChanged() => DataChanged?.Invoke(this, EventArgs.Empty);
    }
}
