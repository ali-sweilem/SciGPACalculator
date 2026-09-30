namespace GpaCalculator.Core.Models
{
    /// <summary>
    /// يمثل فصل دراسي واحد. الـ GPA بتاع الفصل ده بيتحسب من الـ Courses اللي جواه بس،
    /// من غير أي تأثير من قاعدة الـ Retake (اتفقنا إن الـ 2H Cap بيتطبق بس وقت حساب
    /// الـ CGPA التراكمي، مش على مستوى الفصل الواحد).
    /// </summary>
    public class Semester
    {
        public int SemesterId { get; set; }

        /// <summary>
        /// مثلاً "الفصل الأول 2024" أو "Fall 2024".
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// الترتيب الزمني للفصل (1، 2، 3...) — مهم لعرض تطور الـ GPA بترتيب صحيح في الـ Chart.
        /// </summary>
        public int Order { get; set; }

        public List<Course> Courses { get; set; } = new();
    }
}
