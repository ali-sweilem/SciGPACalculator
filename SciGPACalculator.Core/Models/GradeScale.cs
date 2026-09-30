namespace GpaCalculator.Core.Models
{
    /// <summary>
    /// جدول تحويل النسبة المئوية إلى Grade Points، مبني على اللائحة الرسمية للكلية.
    ///
    /// اللائحة مش تقديرات منفصلة فعليًا - هي معادلة خطية متصلة من 60% لـ 100%
    /// (0.1 نقطة لكل 1%)، والحروف (A, B, C, D) مجرد تسميات فوق نفس المعادلة المستمرة.
    /// اتأكد من كده بمقارنة حدود كل تقدير: عند 60% (بداية D) = 1.0، عند 65% (بداية C) = 1.5،
    /// عند 75% (بداية B) = 2.5، عند 85% (بداية A) = 3.5، عند 100% = 5.0 - كلهم مطابقين
    /// للائحة الرسمية بالظبط، فمفيش قفزات بين التقديرات، الانتقال ناعم.
    /// </summary>
    public static class GradeScale
    {
        public const decimal PassingPercentage = 60m;

        /// <summary>النقط عند أقل نسبة نجاح (60%) بالظبط - نقطة بداية المعادلة الخطية.</summary>
        private const decimal PointsAtPassingThreshold = 1.0m;

        /// <summary>معدل الزيادة: كل 1% فوق حد النجاح بيضيف 0.1 نقطة، ثابت عبر اللائحة كلها.</summary>
        private const decimal PointsPerPercentagePoint = 0.1m;

        /// <summary>أعلى Points ممكن الوصول له فعليًا (عند 100%) = 5.0.</summary>
        public static decimal MaxPossiblePoints => ToPoints(100m);

        public static bool IsPassing(decimal percentage) => percentage >= PassingPercentage;

        /// <summary>
        /// بيحوّل النسبة المئوية لـ Points حسب المعادلة الخطية الرسمية.
        /// راسب (أقل من 60%) = صفر دايمًا، بغض النظر عن قيمة النسبة بالظبط.
        /// </summary>
        public static decimal ToPoints(decimal percentage)
        {
            if (!IsPassing(percentage)) return 0m;

            return PointsAtPassingThreshold + (percentage - PassingPercentage) * PointsPerPercentagePoint;
        }

        /// <summary>
        /// التسمية الحرفية (A/B/C/D/F) - للعرض بس، مش بتُستخدم في أي حساب فعلي.
        /// </summary>
        public static string ToLetter(decimal percentage)
        {
            if (percentage >= 85m) return "A";
            if (percentage >= 75m) return "B";
            if (percentage >= 65m) return "C";
            if (percentage >= 60m) return "D";
            return "F";
        }

        /// <summary>
        /// عكس المعادلة: أقل نسبة مئوية بتحقق عدد Points معين. بيتستخدم في اقتراح
        /// "محتاج تقريبًا كام%" في أداة الـ Target GPA.
        /// بيرجع null لو الـ Points المطلوبة مستحيلة (أعلى من MaxPossiblePoints).
        /// </summary>
        public static decimal? MinimumPercentageForPoints(decimal requiredPoints)
        {
            if (requiredPoints > MaxPossiblePoints) return null;
            if (requiredPoints <= PointsAtPassingThreshold) return PassingPercentage;

            var percentage = PassingPercentage + (requiredPoints - PointsAtPassingThreshold) / PointsPerPercentagePoint;
            return percentage > 100m ? 100m : percentage;
        }
    }
}
