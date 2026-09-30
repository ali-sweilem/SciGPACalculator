using GpaCalculator.Core.Models;
using GpaCalculator.Core.Services;
using Xunit;

namespace GpaCalculator.Tests
{
    public class TargetGpaServiceTests
    {
        [Fact]
        public void CalculateRequiredAverage_NoCurrentData_OnlyNewCourses_SimpleCase()
        {
            var semesters = new List<Semester>(); // طالب لسه مبدأش، مفيش بيانات حالية

            var remaining = new List<RemainingCourseInput>
            {
                new RemainingCourseInput { CourseCode = "A", CreditHours = 3 },
                new RemainingCourseInput { CourseCode = "B", CreditHours = 3 }
            };

            // عايز CGPA = 3.0 من غير أي بيانات سابقة، ومفيش أي مادة Retake هنا
            var result = TargetGpaService.CalculateRequiredAverage(semesters, targetCgpa: 3.0m, remaining);

            Assert.Equal(6m, result.TotalWeightedRemainingHours);      // 3+3 عادي، مفيش Retake
            Assert.Equal(18m, result.RequiredTotalPoints);              // 3.0 × 6 - 0
            Assert.Equal(3.0m, result.RequiredAveragePoints);           // نفس الهدف بالظبط، منطقي مفيش بيانات سابقة
            Assert.True(result.IsAchievable);
        }

        [Fact]
        public void CalculateRequiredAverage_MixOfNormalAndRetakeCourse_UsesWeightedDenominator()
        {
            // هنا بنكرر المثال اللي شرحته قبل كده: مادة A عادية (3H) + مادة B Retake (3H فعليًا،
            // بس هتتحسب 2×3=6 في المقام). لازم يبقى فيه سجل فاشل فعلي لمادة B علشان تتكتشف
            // تلقائيًا كـ Retake.
            var semesters = new List<Semester>
            {
                new Semester
                {
                    SemesterId = 1,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "B", CreditHours = 3, Percentage = 45 } // رسوب سابق في B
                    }
                }
            };

            var remaining = new List<RemainingCourseInput>
            {
                new RemainingCourseInput { CourseCode = "A", CreditHours = 3 }, // مادة جديدة عادية
                new RemainingCourseInput { CourseCode = "B", CreditHours = 3 }  // نفس مادة الرسوب، هتتسجل تاني
            };

            var result = TargetGpaService.CalculateRequiredAverage(semesters, targetCgpa: 3.0m, remaining);

            // الـ Baseline: مادة B اتستبعدت بالكامل من الحساب الحالي (مش هتساهم بـ H لوحدها هنا)
            // الـ Weighted Remaining Hours: A=3 (عادي) + B=6 (2×3 لأنها Retake) = 9
            Assert.Equal(9m, result.TotalWeightedRemainingHours);

            // Required Total Points = 3.0 × (0 + 9) - 0 = 27
            Assert.Equal(27m, result.RequiredTotalPoints);

            // Required Average = 27 / 9 = 3.0 (نفس الهدف، لأن الـ Baseline صفر هنا)
            Assert.Equal(3.0m, result.RequiredAveragePoints);

            // لو كان الكود اتعمل غلط بمقام Naive (6 بدل 9)، كانت النتيجة هتبقى 4.5 (مستحيلة)
            Assert.NotEqual(4.5m, result.RequiredAveragePoints);
        }

        [Fact]
        public void CalculateRequiredAverage_FailedCourseNotYetRegistered_NoDoubleCountingOfHours()
        {
            // السيناريو اللي وصفته بالظبط: رسبت في مقرر الترم الأول، ولسه معدتهوش،
            // وعايز تعرف المطلوب في باقي مقررات التخرج شاملة المادة دي
            var semesters = new List<Semester>
            {
                new Semester
                {
                    SemesterId = 1,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "ENG101", CreditHours = 3, Percentage = 90 }, // مادة مستقرة ناجحة => 3.7×3=11.1
                        new Course { CourseCode = "PHYS101", CreditHours = 4, Percentage = 40 }  // راسب، لسه معدهاش
                    }
                }
            };

            var remaining = new List<RemainingCourseInput>
            {
                new RemainingCourseInput { CourseCode = "PHYS101", CreditHours = 4 } // نفس مادة الرسوب، هتتسجل مستقبلًا
            };

            var result = TargetGpaService.CalculateRequiredAverage(semesters, targetCgpa: 3.0m, remaining);

            // الـ CGPA الحالي المعروض للمستخدم لازم يفضل يعكس كل بياناته الحقيقية زي ما هي
            // (ENG101: 90% => 4.0 نقطة × 3 ساعات = 12.0) + (PHYS101 فاشلة: 0 نقطة / 4 ساعات) = 12.0/7
            Assert.Equal(12.0m / 7m, result.CurrentCgpa);

            // لكن الـ Baseline المستخدم في المعادلة استبعد PHYS101 تمامًا (مش هتساهم بـ 4 ساعات هنا)
            // وبقت هتتحسب بس عن طريق الـ Weighted Remaining Hours = 2×4 = 8
            Assert.Equal(8m, result.TotalWeightedRemainingHours);

            // Required Total Points = 3.0 × (3 + 8) - 12.0 = 33 - 12.0 = 21.0
            // (الـ 3 هنا هي baselineHours بتاعة ENG101 بس، مش 3+4)
            Assert.Equal(21.0m, result.RequiredTotalPoints);

            // Required Average = 21.0 / 8 = 2.625
            Assert.Equal(21.0m / 8m, result.RequiredAveragePoints);

            // لو كان فيه Double Counting (يعني الـ 4 ساعات بتاعة الرسوب القديم اتحسبت
            // كمان جوه الـ Baseline)، كان الناتج هيختلف تمامًا عن كده
        }

        [Fact]
        public void CalculateRequiredAverage_ImpossibleTarget_FlagsNotAchievable()
        {
            var semesters = new List<Semester>
            {
                new Semester
                {
                    SemesterId = 1,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "X", CreditHours = 30, Percentage = 40 } // نسبة كبيرة من الساعات رسوب دائم (مش هيتسجل تاني - مش في remaining)
                    }
                }
            };

            var remaining = new List<RemainingCourseInput>
            {
                new RemainingCourseInput { CourseCode = "Y", CreditHours = 3 }
            };

            // هدف عالي جدًا (3.9) مع باقي ساعات قليلة جدًا مقارنة برصيد سيء متراكم - مستحيل
            var result = TargetGpaService.CalculateRequiredAverage(semesters, targetCgpa: 3.9m, remaining);

            Assert.False(result.IsAchievable);
            Assert.Null(result.SuggestedMinimumGrade);
        }

        [Fact]
        public void CalculateRequiredAverage_AlreadyGuaranteed_EvenWithZeroInRemaining()
        {
            var semesters = new List<Semester>
            {
                new Semester
                {
                    SemesterId = 1,
                    Courses = new List<Course>
                    {
                        new Course { CourseCode = "X", CreditHours = 60, Percentage = 95 } // رصيد ممتاز متراكم كبير
                    }
                }
            };

            var remaining = new List<RemainingCourseInput>
            {
                new RemainingCourseInput { CourseCode = "Y", CreditHours = 3 }
            };

            // هدف منخفض جدًا مقارنة بالرصيد الحالي الضخم
            var result = TargetGpaService.CalculateRequiredAverage(semesters, targetCgpa: 2.0m, remaining);

            Assert.True(result.AlreadyGuaranteed);
            Assert.True(result.IsAchievable);
        }

        [Fact]
        public void CalculateRequiredAverage_NoRemainingCourses_ReturnsMessageOnly()
        {
            var semesters = new List<Semester>();
            var remaining = new List<RemainingCourseInput>();

            var result = TargetGpaService.CalculateRequiredAverage(semesters, targetCgpa: 3.0m, remaining);

            Assert.Equal(0m, result.TotalWeightedRemainingHours);
            Assert.Null(result.RequiredAveragePoints);
            Assert.NotEmpty(result.Messages);
        }
    }
}
