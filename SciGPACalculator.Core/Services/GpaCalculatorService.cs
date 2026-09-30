using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    public static class GpaCalculatorService
    {
        /// <summary>
        /// Calculates the GPA for a single semester.
        /// It uses only the courses in that semester without applying the Retake rule.
        /// Each semester reflects the student's actual performance during that semester.
        /// The Retake Cap is applied only when calculating the overall cumulative GPA (CGPA).
        ///
        /// Returns null if the semester has no graded courses instead of returning
        /// a misleading value of 0.
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
        /// Calculates the current cumulative GPA (CGPA) across all semesters
        /// after applying the Retake rule.
        /// A course that was failed and then passed is calculated using 2H instead of H,
        /// and a course that is still failed follows the same policy.
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