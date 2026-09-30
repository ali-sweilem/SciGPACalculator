using GpaCalculator.Core.Models;
using GpaCalculator.Core.Services;
using Xunit;

namespace GpaCalculator.Tests
{
    public class RetakePolicyResolverTests
    {
        [Fact]
        public void Course_NeverFailed_ContributesNormalWeight()
        {
            // مقرر عادي، H=3، نجح من أول مرة بـ 90% => Points = 1.0 + (90-60)×0.1 = 4.0
            var courses = new List<Course>
            {
                new Course { CourseCode = "CS101", CreditHours = 3, Percentage = 90 }
            };

            var result = RetakePolicyResolver.Resolve(courses).Single();

            Assert.False(result.HasEverFailed);
            Assert.Equal(3m, result.Hours);                 // H، مش 2H
            Assert.Equal(4.0m * 3, result.Points);
        }

        [Fact]
        public void Course_FailedOnceThenPassed_MatchesAlgebraExample()
        {
            // ده بالظبط مثالك: الجبر H=4، رسب مرة واحدة، نجح بنسبة 85%
            // Points = 1.0 + (85-60)×0.1 = 3.5 (بالصدفة نفس حد بداية تقدير A في اللائحة)
            var courses = new List<Course>
            {
                new Course { CourseCode = "MATH101", CreditHours = 4, Percentage = 40 },  // رسوب (أقل من 60)
                new Course { CourseCode = "MATH101", CreditHours = 4, Percentage = 85 }   // نجاح لاحق => 3.5 Points
            };

            var result = RetakePolicyResolver.Resolve(courses).Single();

            Assert.True(result.HasEverFailed);
            Assert.True(result.CurrentlyPassed);
            Assert.Equal(8m, result.Hours);                  // 2H بالظبط، زي ما اتفقنا
            Assert.Equal(14.0m, result.Points);               // 3.5 × 4 = 14.0 (البسط ماتأثرش بالرسوب)
        }

        [Fact]
        public void Course_FailedTwiceThenPassed_HoursStillCappedAtDouble()
        {
            // رسب مرتين، نجح في المرة التالتة بنسبة 75% => Points = 1.0+(75-60)×0.1 = 2.5
            // الـ Hours المفروض تفضل 2H، مش 3H.
            var courses = new List<Course>
            {
                new Course { CourseCode = "PHYS201", CreditHours = 3, Percentage = 45 },
                new Course { CourseCode = "PHYS201", CreditHours = 3, Percentage = 50 },
                new Course { CourseCode = "PHYS201", CreditHours = 3, Percentage = 75 } // نجاح => 2.5 Points
            };

            var result = RetakePolicyResolver.Resolve(courses).Single();

            Assert.Equal(6m, result.Hours);                   // 2×3 بالظبط، مش 3×3
            Assert.Equal(2.5m * 3, result.Points);
        }

        [Fact]
        public void Course_StillFailingOnce_NotYetPassed_ContributesFullHourOnly()
        {
            // رسب مرة واحدة ولسه معداهاش. المفروض Denominator = H بس (مش Capped لسه)
            var courses = new List<Course>
            {
                new Course { CourseCode = "CHEM101", CreditHours = 4, Percentage = 50 }
            };

            var result = RetakePolicyResolver.Resolve(courses).Single();

            Assert.False(result.CurrentlyPassed);
            Assert.Equal(4m, result.Hours);                   // H
            Assert.Equal(0m, result.Points);
        }

        [Fact]
        public void Course_StillFailingTwiceOrMore_HoursCappedAtDoubleEvenWithoutPassing()
        {
            // ده الافتراض اللي شرحته: رسب 3 مرات ولسه معداهاش،
            // الـ Cap بيتفعّل من الرسوبة التانية وبيثبت عند 2H
            var courses = new List<Course>
            {
                new Course { CourseCode = "CHEM101", CreditHours = 4, Percentage = 50 },
                new Course { CourseCode = "CHEM101", CreditHours = 4, Percentage = 55 },
                new Course { CourseCode = "CHEM101", CreditHours = 4, Percentage = 40 }
            };

            var result = RetakePolicyResolver.Resolve(courses).Single();

            Assert.Equal(8m, result.Hours);                   // 2×4، مش 3×4
            Assert.Equal(0m, result.Points);
        }

        [Fact]
        public void UngradedCourse_IsIgnoredCompletely()
        {
            // مقرر لسه معندوش درجة (Percentage = null) لازم يتجاهل تمامًا من الحساب
            var courses = new List<Course>
            {
                new Course { CourseCode = "FUT101", CreditHours = 3, Percentage = null }
            };

            var result = RetakePolicyResolver.Resolve(courses);

            Assert.Empty(result);
        }
    }

    public class GpaCalculatorServiceTests
    {
        [Fact]
        public void SemesterGpa_IgnoresRetakeRule_CountsCourseAtFaceValue()
        {
            // فصل فيه مادة رسب فيها الطالب. الـ Semester GPA بتاع الفصل ده لازم يتحسب
            // عادي بالـ H الحقيقي، من غير أي تعديل — القاعدة دي بتتطبق بس على الـ CGPA
            var semester = new Semester
            {
                SemesterId = 1,
                Courses = new List<Course>
                {
                    new Course { CourseCode = "MATH101", CreditHours = 4, Percentage = 40 }, // راسب
                    new Course { CourseCode = "ENG101",  CreditHours = 3, Percentage = 90 }  // ناجح
                }
            };

            var gpa = GpaCalculatorService.CalculateSemesterGpa(semester);

            // (0×4 + 4.0×3) / (4+3) = 12.0 / 7
            Assert.Equal(12.0m / 7m, gpa);
        }

        [Fact]
        public void SemesterGpa_NoGradedCourses_ReturnsNull()
        {
            var semester = new Semester
            {
                Courses = new List<Course> { new Course { CourseCode = "X", CreditHours = 3, Percentage = null } }
            };

            Assert.Null(GpaCalculatorService.CalculateSemesterGpa(semester));
        }

        [Fact]
        public void CumulativeGpa_AppliesRetakeRuleAcrossSemesters()
        {
            // نفس مثال الجبر، بس موزّع على فصلين مختلفين — بيتأكد إن الـ Resolver
            // بيلم المحاولات صح حتى لو كل محاولة في فصل مختلف
            var semesters = new List<Semester>
            {
                new Semester
                {
                    SemesterId = 1,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "MATH101", CreditHours = 4, Percentage = 40 } // رسوب
                    }
                },
                new Semester
                {
                    SemesterId = 2,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "MATH101", CreditHours = 4, Percentage = 85 }, // نجاح لاحق
                        new Course { CourseCode = "ENG101",  CreditHours = 3, Percentage = 90 }  // مادة عادية
                    }
                }
            };

            var cgpa = GpaCalculatorService.CalculateCumulativeGpa(semesters);

            // MATH101: 85% => 3.5 pts × 4H = 14.0, Hours=8  |  ENG101: 90% => 4.0 pts × 3H = 12.0, Hours=3
            // => (14.0 + 12.0) / (8 + 3) = 26.0 / 11
            var expected = 26.0m / 11m;
            Assert.Equal(expected, cgpa);
        }
    }
}
