namespace GpaCalculator.Core.Models
{
    /// <summary>
    /// Represents a single course record in the student's academic data.
    /// The same course can have multiple Course records if it was retaken
    /// (for example, failed first and passed later).
    /// These records are linked using the CourseCode.
    /// </summary>
    public class Course
    {
        /// <summary>
        /// A unique code used to identify the course, such as "MATH101".
        /// If there is no official course code, the course name can be used instead.
        /// This value is used to link all attempts of the same course when calculating retakes.
        /// It should use the same format for every attempt.
        /// Later, this can be handled using a dropdown in the UI instead of manual input
        /// to prevent typing errors.
        /// </summary>
        public string CourseCode { get; set; } = string.Empty;

        /// <summary>
        /// The display name of the course.
        /// It can be the same as CourseCode if there is no official course code system.
        /// </summary>
        public string CourseName { get; set; } = string.Empty;

        public decimal CreditHours { get; set; }

        /// <summary>
        /// The course grade as a percentage (0-100).
        /// Nullable because a course may not have a grade yet.
        /// This is useful for What-If and Target GPA calculations,
        /// where a temporary grade can be entered.
        /// </summary>
        public decimal? Percentage { get; set; }

        /// <summary>
        /// True if the student failed this course in a previous attempt
        /// with the same CourseCode.
        /// This value will be calculated automatically by the application
        /// instead of being entered manually by the user.
        /// The calculation will be implemented later in the service layer.
        /// </summary>
        public bool IsRetake { get; set; }

        /// <summary>
        /// The semester in which the course was taken.
        /// Nullable because a remaining course, such as a previously failed course
        /// that has not been retaken yet, may be included in Target GPA calculations
        /// without being assigned to a specific semester.
        /// </summary>
        public int? SemesterId { get; set; }
    }
}