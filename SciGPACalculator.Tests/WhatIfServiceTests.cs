using GpaCalculator.Core.Models;
using GpaCalculator.Core.Services;
using Xunit;

namespace GpaCalculator.Tests
{
    public class WhatIfServiceTests
    {
        [Fact]
        public void Simulate_BrandNewCourse_AddsNormalWeightOnTopOfCurrent()
        {
            var semesters = new List<Semester>
            {
                new Semester
                {
                    SemesterId = 1,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "ENG101", CreditHours = 3, Percentage = 90 } // 4.0 pts
                    }
                }
            };

            var hypothetical = new List<WhatIfCourseInput>
            {
                new WhatIfCourseInput { CourseCode = "CS201", CourseName = "Data Structures", CreditHours = 3, HypotheticalPercentage = 85 } // 3.5 pts
            };

            var result = WhatIfService.Simulate(semesters, hypothetical);

            // الحالي: 4.0 (بدون تغيير)
            Assert.Equal(4.0m, result.CurrentCgpa);

            // المتوقع: (4.0×3 + 3.5×3) / (3+3) = 22.5 / 6 = 3.75
            Assert.Equal(3.75m, result.PredictedCgpa);
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Simulate_PreviouslyFailedCourse_MatchesUserScenario_AppliesRetakeCapAutomatically()
        {
            // بالظبط السيناريو اللي وصفته: رسبت في مقرر الترم الأول (Percentage < 60)،
            // ولسه معدتهوش، وعايز أعرف الـ CGPA المتوقع لو نجحت فيه بدرجة معينة مستقبلاً.
            var semesters = new List<Semester>
            {
                new Semester
                {
                    SemesterId = 1,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "PHYS101", CreditHours = 3, Percentage = 45 } // راسب
                    }
                }
            };

            var hypothetical = new List<WhatIfCourseInput>
            {
                new WhatIfCourseInput { CourseCode = "PHYS101", CourseName = "Physics", CreditHours = 3, HypotheticalPercentage = 75 } // نجاح افتراضي: 2.5 pts
            };

            var result = WhatIfService.Simulate(semesters, hypothetical);

            // الحالي: مادة راسب فيها لسه، مانجحش => Points=0, Hours=3 => CGPA = 0
            Assert.Equal(0m, result.CurrentCgpa);

            // المتوقع: لازم الرسوب القديم يتشال تمامًا من الحساب، والمادة تتحسب بـ 2H بس
            // (2.5×3) / (2×3) = 7.5 / 6 = 1.25
            // فخلينا نتأكد إن القيمة مش 7.5/9 (يعني إن الرسوب القديم لسه بيحسب)
            Assert.Equal(7.5m / 6m, result.PredictedCgpa);
            Assert.NotEqual(7.5m / 9m, result.PredictedCgpa); // ده كان هيحصل لو الرسوب القديم اتعد غلط
            Assert.Empty(result.Warnings);
        }

        [Fact]
        public void Simulate_AlreadyPassedCourse_ReturnsWarningAndIgnoresScenario()
        {
            var semesters = new List<Semester>
            {
                new Semester
                {
                    SemesterId = 1,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "ENG101", CreditHours = 3, Percentage = 90 }
                    }
                }
            };

            var hypothetical = new List<WhatIfCourseInput>
            {
                new WhatIfCourseInput { CourseCode = "ENG101", CourseName = "English", CreditHours = 3, HypotheticalPercentage = 95 }
            };

            var result = WhatIfService.Simulate(semesters, hypothetical);

            Assert.Single(result.Warnings);
            // المتوقع لازم يفضل زي الحالي بالظبط، لأن السيناريو اتجاهل تمامًا
            Assert.Equal(result.CurrentCgpa, result.PredictedCgpa);
        }

        [Fact]
        public void Simulate_SingleHypotheticalCourse_WorksWithoutRequiringMultiple()
        {
            // تأكيد إن الأداة اختيارية العدد - مقرر واحد بس كافي، مفيش حد أدنى
            var semesters = new List<Semester>(); // مفيش بيانات حقيقية خالص

            var hypothetical = new List<WhatIfCourseInput>
            {
                new WhatIfCourseInput { CourseCode = "CS101", CourseName = "Intro to CS", CreditHours = 3, HypotheticalPercentage = 80 } // 3.0 pts
            };

            var result = WhatIfService.Simulate(semesters, hypothetical);

            Assert.Null(result.CurrentCgpa); // مفيش بيانات حقيقية
            Assert.Equal(3.0m, result.PredictedCgpa);
        }
    }
}
