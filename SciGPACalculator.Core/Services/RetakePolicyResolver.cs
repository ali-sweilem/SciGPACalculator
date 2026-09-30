using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    /// <summary>
    /// نتيجة تطبيق قاعدة الـ Retake على مادة واحدة (بعد تجميع كل محاولاتها).
    /// دي "المساهمة النهائية" للمادة في أي حساب تراكمي (CGPA)، مش السجلات الخام.
    /// </summary>
    public class CourseContribution
    {
        public string CourseCode { get; set; } = string.Empty;
        public decimal Points { get; set; }   // = Points-per-hour × Hours (مش النسبة، القيمة النهائية المضروبة)
        public decimal Hours { get; set; }    // = الـ Denominator المُعدّل (H أو 2H)
        public bool HasEverFailed { get; set; }
        public bool CurrentlyPassed { get; set; }
    }

    public static class RetakePolicyResolver
    {
        /// <summary>
        /// الحد الأقصى لمضاعف الساعات لأي مادة، مهما تكرر رسوبها.
        /// القيمة دي جاية حرفيًا من قاعدتك: "الرقم دا مش بيزيد عن الضعف مهما كان عدد مرات الرسوب".
        /// </summary>
        private const decimal MaxHourMultiplier = 2m;

        /// <summary>
        /// بياخد كل مقررات الطالب (مجمّعة من كل الفصول)، ويرجّع مساهمة كل مادة (Group by CourseCode)
        /// بعد تطبيق قاعدة الـ Retake. النتيجة دي هي اللي المفروض تتغذى بيها حسابات الـ CGPA،
        /// مش الـ Course records الخام مباشرة.
        /// </summary>
        public static List<CourseContribution> Resolve(IEnumerable<Course> allCourses)
        {
            var results = new List<CourseContribution>();

            var groups = allCourses
                .Where(c => c.Percentage.HasValue)   // تجاهل أي مقرر لسه معندوش درجة خالص
                .GroupBy(c => c.CourseCode);

            foreach (var group in groups)
            {
                var attempts = group.ToList();
                var passingAttempts = attempts.Where(c => GradeScale.IsPassing(c.Percentage!.Value)).ToList();
                var failingAttemptsCount = attempts.Count(c => !GradeScale.IsPassing(c.Percentage!.Value));

                if (passingAttempts.Any())
                {
                    // اتفرض إن الطالب بيوقف عن إعادة المادة بمجرد ما ينجح فيها،
                    // فمن المفروض يكون فيه محاولة ناجحة واحدة بس. لو حصل غلط إدخال
                    // (أكتر من محاولة ناجحة لنفس المادة)، ناخد آخر واحدة كـ "النتيجة الرسمية".
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
                    // لسه فاشل في المادة دي، مفيش نجاح لحد دلوقتي.
                    // الـ Points = 0 دايمًا هنا (لأن مفيش محاولة ناجحة أصلاً).
                    // الـ Hours بتتراكم عادي لحد ما توصل للـ Cap (2H)، وبعدها بتثبت.
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
