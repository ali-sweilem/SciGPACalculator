namespace GpaCalculator.WPF.ViewModels
{
    /// <summary>
    /// عنصر عرض بس (مش قابل للتعديل) - بيتبني من جديد كل مرة Refresh بتتنادى.
    /// </summary>
    public class SemesterSummaryViewModel
    {
        public string Name { get; }
        public string GpaDisplay { get; }

        public SemesterSummaryViewModel(string name, decimal? gpa)
        {
            Name = name;
            GpaDisplay = gpa.HasValue ? gpa.Value.ToString("F2") : "لا توجد درجات بعد";
        }
    }
}
