using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    /// <summary>
    /// مقرر افتراضي هيدخله المستخدم في أداة الـ What-If (مادة جديدة، أو مادة سبق ورسب فيها
    /// ودلوقتي عايز يعرف توقع النجاح فيها بدرجة معينة).
    /// </summary>
    public class WhatIfCourseInput
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public decimal CreditHours { get; set; }
        public decimal HypotheticalPercentage { get; set; }
    }

    public class WhatIfResult
    {
        /// <summary>
        /// الـ CGPA الحالي الحقيقي، قبل أي محاكاة (للمقارنة في الواجهة).
        /// </summary>
        public decimal? CurrentCgpa { get; set; }

        /// <summary>
        /// الـ CGPA المتوقع لو اتحققت السيناريوهات الافتراضية.
        /// </summary>
        public decimal? PredictedCgpa { get; set; }

        /// <summary>
        /// تحذيرات (زي محاولة محاكاة مادة ناجح فيها الطالب بالفعل).
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    public static class WhatIfService
    {
        /// <summary>
        /// بيحسب الـ CGPA المتوقع لو الطالب جاب الدرجات الافتراضية دي في المقررات المحددة،
        /// من غير ما يعدّل على البيانات الحقيقية المحفوظة إطلاقًا (Simulation بحتة).
        /// القايمة ممكن تكون مقرر واحد بس أو أكتر (اختياري حسب طلبك).
        /// </summary>
        public static WhatIfResult Simulate(
            IEnumerable<Semester> actualSemesters,
            IEnumerable<WhatIfCourseInput> hypotheticalCourses)
        {
            var actualCourses = actualSemesters.SelectMany(s => s.Courses).ToList();
            var currentCgpa = GpaCalculatorService.CalculateCumulativeGpa(actualSemesters);

            var warnings = new List<string>();
            var simulatedAdditions = new List<Course>();

            foreach (var input in hypotheticalCourses)
            {
                var existingRecordsForCode = actualCourses
                    .Where(c => c.CourseCode == input.CourseCode && c.Percentage.HasValue)
                    .ToList();

                var alreadyPassed = existingRecordsForCode
                    .Any(c => GradeScale.IsPassing(c.Percentage!.Value));

                if (alreadyPassed)
                {
                    // النظام لا يدعم إعادة مادة ناجح فيها الطالب أصلاً بالفعل (لا يوجد Improvement Retake).
                    // بنتجاهل المحاولة الافتراضية دي بدل ما نسيب الـ Resolver يتعامل معاها
                    // كأنها محاولة تحسين، وهيبلّغ المستخدم ليه.
                    warnings.Add(
                        $"تحذير: المادة '{input.CourseCode}' مسجلة بالفعل كناجحة، " +
                        "والنظام لا يدعم إعادة مادة لتحسين الدرجة - تم تجاهل هذا السيناريو.");
                    continue;
                }

                // بيتضاف كـ Course عادي، مربوط بنفس الـ CourseCode. لو فيه محاولة فاشلة حقيقية
                // بنفس الكود، الـ Resolver هيتعرف عليها تلقائيًا ويطبّق قاعدة الـ 2H.
                simulatedAdditions.Add(new Course
                {
                    CourseCode = input.CourseCode,
                    CourseName = input.CourseName,
                    CreditHours = input.CreditHours,
                    Percentage = input.HypotheticalPercentage,
                    IsRetake = existingRecordsForCode.Any(),
                    SemesterId = null // مقرر افتراضي مش مربوط بترم حقيقي
                });
            }

            var combined = actualCourses.Concat(simulatedAdditions);
            var contributions = RetakePolicyResolver.Resolve(combined);

            decimal? predicted = null;
            if (contributions.Any())
            {
                var totalPoints = contributions.Sum(c => c.Points);
                var totalHours = contributions.Sum(c => c.Hours);
                predicted = totalHours == 0 ? null : totalPoints / totalHours;
            }

            return new WhatIfResult
            {
                CurrentCgpa = currentCgpa,
                PredictedCgpa = predicted,
                Warnings = warnings
            };
        }
    }
}
