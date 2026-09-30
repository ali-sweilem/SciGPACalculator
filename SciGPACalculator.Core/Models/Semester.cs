namespace GpaCalculator.Core.Models
{
    /// <summary>
    /// Represents a single academic semester.
    /// The semester GPA is calculated only from the courses in this semester.
    /// The Retake rule does not affect the semester GPA.
    /// The 2H Cap is applied only when calculating the cumulative GPA (CGPA).
    /// </summary>
    public class Semester
    {
        public int SemesterId { get; set; }

        /// <summary>
        /// The display name of the semester, such as "First Semester 2024"
        /// or "Fall 2024".
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The chronological order of the semester (1, 2, 3, ...).
        /// This is used to display GPA progress in the correct order on charts.
        /// </summary>
        public int Order { get; set; }

        public List<Course> Courses { get; set; } = new();
    }
}