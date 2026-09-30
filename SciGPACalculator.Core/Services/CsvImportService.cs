using System.Text;
using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Services
{
    public class CsvImportResult
    {
        public List<Semester> ImportedSemesters { get; set; } = new();
        public int RowsImported { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Imports courses from a CSV file instead of entering them manually.
    /// It intentionally uses no external libraries to keep the project
    /// simple and minimize external dependencies.
    /// </summary>
    public static class CsvImportService
    {
        // Each field can use any of these alternative names in the header row (case-insensitive).
        private static readonly Dictionary<string, string[]> ColumnAliases = new()
        {
            ["Semester"] = new[] { "الفصل", "الترم", "semester", "term" },
            ["CourseName"] = new[] { "اسم المقرر", "المقرر", "course", "coursename" },
            ["CreditHours"] = new[] { "الساعات المعتمدة", "الساعات", "credithours", "hours" },
            ["Percentage"] = new[] { "النسبة", "النسبة %", "percentage", "grade" }
        };

        public static CsvImportResult Import(string csvContent)
        {
            var result = new CsvImportResult();

            var lines = csvContent
                .Split('\n')
                .Select(l => l.TrimEnd('\r'))
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToList();

            if (lines.Count < 2)
            {
                result.Errors.Add("الملف فاضي أو ناقص - لازم يحتوي على صف عناوين وصف بيانات على الأقل.");
                return result;
            }

            var headers = ParseCsvLine(lines[0]).Select(h => h.Trim()).ToList();
            var columnIndex = new Dictionary<string, int>();

            foreach (var (key, aliases) in ColumnAliases)
            {
                var index = headers.FindIndex(h =>
                    aliases.Any(a => string.Equals(a, h, StringComparison.OrdinalIgnoreCase)));
                if (index >= 0) columnIndex[key] = index;
            }

            if (!columnIndex.ContainsKey("CourseName") || !columnIndex.ContainsKey("CreditHours"))
            {
                result.Errors.Add("الملف لازم يحتوي على الأقل على عمود 'اسم المقرر' وعمود 'الساعات المعتمدة'.");
                return result;
            }

            var semesterLookup = new Dictionary<string, Semester>();
            var nextSemesterId = 1;

            for (var lineNumber = 1; lineNumber < lines.Count; lineNumber++)
            {
                var fields = ParseCsvLine(lines[lineNumber]);
                var displayLineNumber = lineNumber + 1; // +1 because the file line numbers start at 1, including the header row.

                string GetField(string key) =>
                    columnIndex.TryGetValue(key, out var idx) && idx < fields.Count
                        ? fields[idx].Trim()
                        : string.Empty;

                var courseName = GetField("CourseName");
                if (string.IsNullOrWhiteSpace(courseName))
                {
                    result.Errors.Add($"سطر {displayLineNumber}: اسم المقرر فاضي - تم تجاهل السطر.");
                    continue;
                }

                if (!decimal.TryParse(GetField("CreditHours"), out var creditHours) || creditHours <= 0)
                {
                    result.Errors.Add($"سطر {displayLineNumber}: الساعات المعتمدة غير صالحة - تم تجاهل السطر.");
                    continue;
                }

                decimal? percentage = null;
                var percentageRaw = GetField("Percentage");
                if (!string.IsNullOrWhiteSpace(percentageRaw))
                {
                    if (!decimal.TryParse(percentageRaw, out var parsedPercentage) ||
                        parsedPercentage < 0 || parsedPercentage > 100)
                    {
                        result.Errors.Add($"سطر {displayLineNumber}: النسبة غير صالحة (لازم تكون بين 0 و100) - تم تجاهل السطر.");
                        continue;
                    }
                    percentage = parsedPercentage;
                }

                // An empty percentage means the course does not have a grade yet.
                // This is allowed and is not considered an error.

                var semesterName = columnIndex.ContainsKey("Semester") ? GetField("Semester") : string.Empty;
                if (string.IsNullOrWhiteSpace(semesterName)) semesterName = "مستورد";

                if (!semesterLookup.TryGetValue(semesterName, out var semester))
                {
                    semester = new Semester
                    {
                        SemesterId = nextSemesterId,
                        Name = semesterName,
                        Order = nextSemesterId
                    };
                    nextSemesterId++;
                    semesterLookup[semesterName] = semester;
                    result.ImportedSemesters.Add(semester);
                }

                semester.Courses.Add(new Course
                {
                    CourseCode = courseName,
                    CourseName = courseName,
                    CreditHours = creditHours,
                    Percentage = percentage,
                    SemesterId = semester.SemesterId
                });

                result.RowsImported++;
            }

            return result;
        }

        /// <summary>
        /// A small parser for a single CSV line that correctly handles fields
        /// enclosed in quotation marks.
        /// This allows fields to contain commas, as Excel may generate them,
        /// unlike a simple Split(',') which would fail in this case.
        /// </summary>
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"'); // Escaped quotation mark inside a quoted field.
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields;
        }
    }
}