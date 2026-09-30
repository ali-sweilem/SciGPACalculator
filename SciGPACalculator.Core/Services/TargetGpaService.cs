using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    /// <summary>
    /// مقرر متبقي على التخرج، هيتسجل في المستقبل. ممكن يكون مادة جديدة تمامًا،
    /// أو مادة سبق ورسب فيها الطالب ولسه معدهاش (زي حالة "شرط التخرج" اللي ناقشناها) -
    /// الحالة دي بتتحدد تلقائيًا من البيانات الفعلية، مش بإدخال يدوي.
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
        /// أقرب تقدير عملي (زي "B+ / 85%") بيحقق المطلوب، بافتراض إنك هتجيب
        /// نفس التقدير تقريبًا في كل المواد الباقية. تقريبي، مش دقيق لكل مادة على حدة.
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

            // الـ CGPA الحالي المعروض للمستخدم - على كل بياناته الحقيقية زي ما هي، بدون استبعاد.
            var displayedCurrentCgpa = GpaCalculatorService.CalculateCumulativeGpa(actualSemesters);

            // الـ Baseline المستخدم فعليًا في المعادلة: بيستبعد أي مادة موجودة في قايمة
            // "المتبقي" لأنها هتتحل بالكامل عن طريق الـ Weighted Remaining Hours تحت،
            // ومنعًا لتكرار حساب ساعاتها مرتين (مرة كرسوب حالي، ومرة كمتبقي مستقبلي).
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
