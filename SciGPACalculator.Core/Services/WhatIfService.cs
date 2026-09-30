using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    /// <summary>
    /// A hypothetical course entered by the user for the What-If tool.
    /// It can be a new course or a course the student previously failed
    /// and wants to simulate passing with a specific grade.
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
        /// The actual current CGPA before any simulation, used for comparison in the UI.
        /// </summary>
        public decimal? CurrentCgpa { get; set; }

        /// <summary>
        /// The predicted CGPA if the hypothetical scenarios are applied.
        /// </summary>
        public decimal? PredictedCgpa { get; set; }

        /// <summary>
        /// Warnings, such as trying to simulate a course the student has already passed.
        /// </summary>
        public List<string> Warnings { get; set; } = new();
    }

    public static class WhatIfService
    {
        /// <summary>
        /// Calculates the predicted CGPA if the student gets the specified hypothetical grades
        /// in the selected courses, without modifying the saved data at all.
        /// This is a pure simulation.
        /// The list can contain one or more courses, depending on the user's request.
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
                    // The system does not support retaking a course the student has already passed
                    // for grade improvement. We ignore this hypothetical attempt instead of letting
                    // the Resolver treat it as an improvement retake, and inform the user why.
                    warnings.Add(
                        $"تحذير: المادة '{input.CourseCode}' مسجلة بالفعل كناجحة، " +
                        "والنظام لا يدعم إعادة مادة لتحسين الدرجة - تم تجاهل هذا السيناريو.");
                    continue;
                }

                // Add it as a normal Course with the same CourseCode. If there is an actual
                // failed attempt with the same code, the Resolver will detect it automatically
                // and apply the 2H rule.
                simulatedAdditions.Add(new Course
                {
                    CourseCode = input.CourseCode,
                    CourseName = input.CourseName,
                    CreditHours = input.CreditHours,
                    Percentage = input.HypotheticalPercentage,
                    IsRetake = existingRecordsForCode.Any(),
                    SemesterId = null // Hypothetical course is not linked to a real semester.
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