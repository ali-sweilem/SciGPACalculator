using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    /// <summary>
    /// A course remaining for graduation that will be taken in the future.
    /// It can be a completely new course or a course the student previously failed
    /// and has not passed yet. This case is detected automatically from the actual data,
    /// not entered manually.
    /// </summary>
    public class RemainingCourseInput
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public decimal CreditHours { get; set; }
    }

    public class TargetGpaResult
    {
        public decimal? CurrentCgpa { get; set; }
        public decimal TargetCgpa { get; set; }
        public decimal TotalWeightedRemainingHours { get; set; }
        public decimal RequiredTotalPoints { get; set; }
        public decimal? RequiredAveragePoints { get; set; }
        public bool IsAchievable { get; set; }
        public bool AlreadyGuaranteed { get; set; }

        /// <summary>
        /// The closest practical grade (such as "B+ / 85%") that meets the requirement,
        /// assuming the student gets approximately the same grade in all remaining courses.
        /// This is an estimate, not an exact result for each individual course.
        /// </summary>
        public string? SuggestedMinimumGrade { get; set; }

        public List<string> Messages { get; set; } = new();
    }

    public static class TargetGpaService
    {
        public static TargetGpaResult CalculateRequiredAverage(
            IEnumerable<Semester> actualSemesters,
            decimal targetCgpa,
            IEnumerable<RemainingCourseInput> remainingCourses)
        {
            var actualCourses = actualSemesters.SelectMany(s => s.Courses).ToList();
            var remainingList = remainingCourses.ToList();
            var remainingCodes = remainingList.Select(r => r.CourseCode).ToHashSet();

            // The current CGPA displayed to the user uses all actual data as it is, without exclusions.
            var displayedCurrentCgpa = GpaCalculatorService.CalculateCumulativeGpa(actualSemesters);

            // The baseline used in the calculation excludes any course listed as remaining
            // because it will be fully handled by the Weighted Remaining Hours below.
            // This prevents its hours from being counted twice: once as a current failure
            // and once as a future remaining course.
            var settledCourses = actualCourses.Where(c => !remainingCodes.Contains(c.CourseCode));
            var settledContributions = RetakePolicyResolver.Resolve(settledCourses);

            var baselinePoints = settledContributions.Sum(c => c.Points);
            var baselineHours = settledContributions.Sum(c => c.Hours);

            decimal weightedRemainingHours = 0;
            foreach (var course in remainingList)
            {
                var hasPriorFailure = actualCourses.Any(c =>
                    c.CourseCode == course.CourseCode &&
                    c.Percentage.HasValue &&
                    !GradeScale.IsPassing(c.Percentage.Value));

                var multiplier = hasPriorFailure ? 2m : 1m;
                weightedRemainingHours += course.CreditHours * multiplier;
            }

            var result = new TargetGpaResult
            {
                CurrentCgpa = displayedCurrentCgpa,
                TargetCgpa = targetCgpa,
                TotalWeightedRemainingHours = weightedRemainingHours
            };

            if (weightedRemainingHours == 0)
            {
                result.Messages.Add("لا يوجد مقررات متبقية مُدخلة - أضف مادة واحدة على الأقل لحساب المطلوب.");
                return result;
            }

            var totalDenominator = baselineHours + weightedRemainingHours;
            var requiredTotalPoints = targetCgpa * totalDenominator - baselinePoints;
            var requiredAverage = requiredTotalPoints / weightedRemainingHours;

            result.RequiredTotalPoints = requiredTotalPoints;
            result.RequiredAveragePoints = requiredAverage;

            if (requiredAverage <= 0)
            {
                result.AlreadyGuaranteed = true;
                result.IsAchievable = true;
                result.Messages.Add("الهدف مضمون فعليًا حتى لو حصلت على صفر في كل المواد الباقية.");
            }
            else if (requiredAverage > GradeScale.MaxPossiblePoints)
            {
                result.IsAchievable = false;
                result.Messages.Add(
                    $"الهدف غير قابل للتحقيق رياضيًا: محتاج متوسط {requiredAverage:F2} نقطة، " +
                    $"وأقصى تقدير ممكن هو {GradeScale.MaxPossiblePoints:F1} نقطة فقط.");
            }
            else
            {
                result.IsAchievable = true;
                var minPercentage = GradeScale.MinimumPercentageForPoints(requiredAverage);
                if (minPercentage.HasValue)
                {
                    var letter = GradeScale.ToLetter(minPercentage.Value);
                    result.SuggestedMinimumGrade =
                        $"تقريبًا {minPercentage.Value:F1}% ({letter}) في كل مادة متبقية";
                }
            }

            return result;
        }
    }
}