using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    /// <summary>
    /// Represents the final contribution of one course after applying the Retake rule
    /// and grouping all of its attempts.
    /// This represents the final contribution used in cumulative GPA (CGPA) calculations,
    /// not the raw course records.
    /// </summary>
    public class CourseContribution
    {
        public string CourseCode { get; set; } = string.Empty;
        public decimal Points { get; set; }   // Points-per-hour × Hours (the final weighted points value)
        public decimal Hours { get; set; }    // The adjusted denominator (H or 2H)
        public bool HasEverFailed { get; set; }
        public bool CurrentlyPassed { get; set; }
    }

    public static class RetakePolicyResolver
    {
        /// <summary>
        /// The maximum hour multiplier for any course, regardless of how many times it was failed.
        /// The value follows the rule that the hours cannot exceed twice the original course hours.
        /// </summary>
        private const decimal MaxHourMultiplier = 2m;

        /// <summary>
        /// Takes all courses across all semesters and returns the final contribution
        /// for each course after applying the Retake rule.
        /// Courses are grouped by CourseCode.
        /// The returned contributions are used for CGPA calculations instead of
        /// using the raw Course records directly.
        /// </summary>
        public static List<CourseContribution> Resolve(IEnumerable<Course> allCourses)
        {
            var results = new List<CourseContribution>();

            var groups = allCourses
                .Where(c => c.Percentage.HasValue)   // Ignore courses that do not have a grade yet.
                .GroupBy(c => c.CourseCode);

            foreach (var group in groups)
            {
                var attempts = group.ToList();
                var passingAttempts = attempts.Where(c => GradeScale.IsPassing(c.Percentage!.Value)).ToList();
                var failingAttemptsCount = attempts.Count(c => !GradeScale.IsPassing(c.Percentage!.Value));

                if (passingAttempts.Any())
                {
                    // It is assumed that the student stops retaking the course after passing it.
                    // Therefore, there should normally be only one passing attempt.
                    // If multiple passing attempts exist due to an input error,
                    // the last one is treated as the official result.
                    var finalPass = passingAttempts.Last();
                    var hours = finalPass.CreditHours;
                    var hadPriorFailure = failingAttemptsCount > 0;

                    results.Add(new CourseContribution
                    {
                        CourseCode = group.Key,
                        Points = GradeScale.ToPoints(finalPass.Percentage!.Value) * hours,
                        Hours = hadPriorFailure ? hours * MaxHourMultiplier : hours,
                        HasEverFailed = hadPriorFailure,
                        CurrentlyPassed = true
                    });
                }
                else
                {
                    // The course is still failed because there is no passing attempt yet.
                    // Points are always 0 because there is no successful attempt.
                    // The hours increase with each failed attempt until reaching the 2H cap.
                    var hours = attempts.First().CreditHours;
                    var multiplier = Math.Min(failingAttemptsCount, (int)MaxHourMultiplier);

                    results.Add(new CourseContribution
                    {
                        CourseCode = group.Key,
                        Points = 0m,
                        Hours = hours * multiplier,
                        HasEverFailed = true,
                        CurrentlyPassed = false
                    });
                }
            }

            return results;
        }
    }
}