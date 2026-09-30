using GpaCalculator.Core.Models;
using GpaCalculator.Core.Persistence;
using Xunit;

namespace GpaCalculator.Tests
{
    public class JsonAcademicRecordRepositoryTests : IDisposable
    {
        private readonly string _tempFilePath;

        public JsonAcademicRecordRepositoryTests()
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"gpa-test-{Guid.NewGuid()}.json");
        }

        public void Dispose()
        {
            if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath);
            var tmp = _tempFilePath + ".tmp";
            if (File.Exists(tmp)) File.Delete(tmp);
        }

        [Fact]
        public void Load_FileDoesNotExist_ReturnsEmptyRecord()
        {
            var repository = new JsonAcademicRecordRepository(_tempFilePath);

            var record = repository.Load();

            Assert.NotNull(record);
            Assert.Empty(record.Semesters);
        }

        [Fact]
        public void SaveThenLoad_RoundTripsAllFieldsCorrectly()
        {
            var repository = new JsonAcademicRecordRepository(_tempFilePath);

            var original = new AcademicRecord
            {
                Semesters = new List<Semester>
                {
                    new Semester
                    {
                        SemesterId = 1,
                        Name = "الفصل الأول 2024",
                        Order = 1,
                        Courses = new List<Course>
                        {
                            new Course
                            {
                                CourseCode = "MATH101",
                                CourseName = "الجبر",
                                CreditHours = 4,
                                Percentage = 85.5m,     // بنتأكد إن الكسور العشرية بتتحفظ بدقة
                                IsRetake = true,
                                SemesterId = 1
                            },
                            new Course
                            {
                                CourseCode = "FUT101",
                                CourseName = "مادة مستقبلية",
                                CreditHours = 3,
                                Percentage = null,       // بنتأكد إن الـ null بيتحفظ صح
                                IsRetake = false,
                                SemesterId = null
                            }
                        }
                    }
                }
            };

            repository.Save(original);
            var loaded = repository.Load();

            Assert.Single(loaded.Semesters);
            Assert.Equal("الفصل الأول 2024", loaded.Semesters[0].Name);
            Assert.Equal(2, loaded.Semesters[0].Courses.Count);

            var math = loaded.Semesters[0].Courses.Single(c => c.CourseCode == "MATH101");
            Assert.Equal(85.5m, math.Percentage);
            Assert.True(math.IsRetake);

            var future = loaded.Semesters[0].Courses.Single(c => c.CourseCode == "FUT101");
            Assert.Null(future.Percentage);
            Assert.Null(future.SemesterId);
        }

        [Fact]
        public void Load_CorruptedJsonFile_ThrowsInvalidDataExceptionWithClearMessage()
        {
            File.WriteAllText(_tempFilePath, "{ هذا ليس JSON صالح على الإطلاق ][[[");
            var repository = new JsonAcademicRecordRepository(_tempFilePath);

            var ex = Assert.Throws<InvalidDataException>(() => repository.Load());
            Assert.Contains(_tempFilePath, ex.Message);
        }

        [Fact]
        public void Save_CalledTwice_SecondSaveFullyReplacesFirst()
        {
            var repository = new JsonAcademicRecordRepository(_tempFilePath);

            repository.Save(new AcademicRecord
            {
                Semesters = new List<Semester> { new Semester { SemesterId = 1, Name = "قديم" } }
            });

            repository.Save(new AcademicRecord
            {
                Semesters = new List<Semester> { new Semester { SemesterId = 2, Name = "جديد" } }
            });

            var loaded = repository.Load();

            Assert.Single(loaded.Semesters);
            Assert.Equal("جديد", loaded.Semesters[0].Name);
        }

        [Fact]
        public void Load_EmptyFile_ReturnsEmptyRecordInsteadOfThrowing()
        {
            File.WriteAllText(_tempFilePath, "");
            var repository = new JsonAcademicRecordRepository(_tempFilePath);

            var record = repository.Load();

            Assert.NotNull(record);
            Assert.Empty(record.Semesters);
        }
    }
}
