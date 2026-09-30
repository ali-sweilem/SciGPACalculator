using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    public static class GpaCalculatorService
    {
        /// <summary>
        /// GPA الفصل الواحد. بيحسب من مقررات الفصل ده بس، من غير أي تأثير من قاعدة الـ Retake
        /// (كل فصل بيعكس مجهود الطالب فيه هو بالظبط، والـ Retake Cap بيتطبق بس على مستوى
        /// الـ CGPA التراكمي الكلي — ده الـ Alternative A اللي اتفقنا عليه).
        /// بيرجع null لو مفيش أي مقرر متدرّج في الفصل، بدل ما يرجع 0 وهمي.
        /// </summary>
        public static decimal? CalculateSemesterGpa(Semester semester)
        {
            var graded = semester.Courses.Where(c => c.Percentage.HasValue).ToList();
            if (!graded.Any()) return null;

            var totalPoints = graded.Sum(c => GradeScale.ToPoints(c.Percentage!.Value) * c.CreditHours);
            var totalHours = graded.Sum(c => c.CreditHours);

            return totalHours == 0 ? null : totalPoints / totalHours;
        }

        /// <summary>
        /// الـ CGPA التراكمي الحالي، عبر كل الفصول، بعد تطبيق قاعدة الـ Retake
        /// (مادة اتفشلت وبعدين نجحت بتتحسب بـ 2H مش H، ومادة لسه فاشلة بتتحسب حسب نفس المنطق).
        /// </summary>
        public static decimal? CalculateCumulativeGpa(IEnumerable<Semester> semesters)
        {
            var allCourses = semesters.SelectMany(s => s.Courses);
            var contributions = RetakePolicyResolver.Resolve(allCourses);

            if (!contributions.Any()) return null;

            var totalPoints = contributions.Sum(c => c.Points);
            var totalHours = contributions.Sum(c => c.Hours);

            return totalHours == 0 ? null : totalPoints / totalHours;
        }
    }
}
