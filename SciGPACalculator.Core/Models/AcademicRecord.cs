namespace GpaCalculator.Core.Models
{
    /// <summary>
    /// Represents the student's complete academic data,
    /// including all semesters and their courses.
    /// This is the main structure used to save and load academic data,
    /// currently stored in a JSON file.
    /// </summary>
    public class AcademicRecord
    {
        public List<Semester> Semesters { get; set; } = new();
    }
}