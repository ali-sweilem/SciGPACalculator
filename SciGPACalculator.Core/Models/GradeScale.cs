namespace GpaCalculator.Core.Models
{
    /// <summary>
    /// Defines the conversion from percentage grades to Grade Points,
    /// based on the official college grading scale.
    ///
    /// The grading scale is a continuous linear formula from 60% to 100%,
    /// where each additional 1% adds 0.1 Grade Points.
    /// The letter grades (A, B, C, D) are labels applied to ranges within
    /// the same continuous formula.
    ///
    /// For example:
    /// 60% = 1.0, 65% = 1.5, 75% = 2.5, 85% = 3.5, and 100% = 5.0.
    /// This means there are no gaps between grade ranges.
    /// </summary>
    public static class GradeScale
    {
        public const decimal PassingPercentage = 60m;

        /// <summary>
        /// The Grade Points at the minimum passing percentage (60%).
        /// This is the starting point of the linear formula.
        /// </summary>
        private const decimal PointsAtPassingThreshold = 1.0m;

        /// <summary>
        /// The Grade Point increase for each additional percentage point.
        /// Each 1% above the passing threshold adds 0.1 points.
        /// </summary>
        private const decimal PointsPerPercentagePoint = 0.1m;

        /// <summary>
        /// The maximum possible Grade Points, reached at 100%.
        /// </summary>
        public static decimal MaxPossiblePoints => ToPoints(100m);

        public static bool IsPassing(decimal percentage) => percentage >= PassingPercentage;

        /// <summary>
        /// Converts a percentage grade to Grade Points using the official linear formula.
        /// A failing grade below 60% always results in 0 points.
        /// </summary>
        public static decimal ToPoints(decimal percentage)
        {
            if (!IsPassing(percentage)) return 0m;

            return PointsAtPassingThreshold + (percentage - PassingPercentage) * PointsPerPercentagePoint;
        }

        /// <summary>
        /// Returns the letter grade (A/B/C/D/F) for display purposes only.
        /// The letter grade is not used in any calculations.
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
        /// Calculates the minimum percentage required to achieve a specific number
        /// of Grade Points. This is used by the Target GPA tool to suggest
        /// the approximate percentage needed.
        /// Returns null if the required points are higher than the maximum possible points.
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