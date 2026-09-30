using GpaCalculator.Core.Services;
using Xunit;

namespace GpaCalculator.Tests
{
    public class CsvImportServiceTests
    {
        [Fact]
        public void Import_ValidRows_GroupsIntoSemestersCorrectly()
        {
            var csv =
                "الفصل,اسم المقرر,الساعات المعتمدة,النسبة\n" +
                "الفصل الأول,الجبر,4,85\n" +
                "الفصل الأول,فيزياء,3,90\n" +
                "الفصل الثاني,كيمياء,3,70";

            var result = CsvImportService.Import(csv);

            Assert.Equal(3, result.RowsImported);
            Assert.Empty(result.Errors);
            Assert.Equal(2, result.ImportedSemesters.Count);

            var firstSemester = result.ImportedSemesters.Single(s => s.Name == "الفصل الأول");
            Assert.Equal(2, firstSemester.Courses.Count);

            var secondSemester = result.ImportedSemesters.Single(s => s.Name == "الفصل الثاني");
            Assert.Single(secondSemester.Courses);
        }

        [Fact]
        public void Import_MissingRequiredColumn_ReturnsErrorAndNoData()
        {
            // مفيش عمود "الساعات المعتمدة" خالص
            var csv = "اسم المقرر,النسبة\nالجبر,85";

            var result = CsvImportService.Import(csv);

            Assert.Empty(result.ImportedSemesters);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Import_BlankCourseName_SkipsRowButContinuesWithRest()
        {
            var csv =
                "اسم المقرر,الساعات المعتمدة,النسبة\n" +
                ",3,85\n" +           // اسم فاضي - لازم يتجاهل
                "فيزياء,3,90";         // ده صحيح ولازم يستورد

            var result = CsvImportService.Import(csv);

            Assert.Equal(1, result.RowsImported);
            Assert.Single(result.Errors);
        }

        [Fact]
        public void Import_InvalidCreditHours_SkipsRow()
        {
            var csv =
                "اسم المقرر,الساعات المعتمدة,النسبة\n" +
                "الجبر,غير_رقم,85\n" +
                "الجبر,0,85";           // صفر مرفوض كمان (لازم يكون أكبر من صفر)

            var result = CsvImportService.Import(csv);

            Assert.Equal(0, result.RowsImported);
            Assert.Equal(2, result.Errors.Count);
        }

        [Fact]
        public void Import_InvalidPercentage_SkipsRow()
        {
            var csv = "اسم المقرر,الساعات المعتمدة,النسبة\nالجبر,4,150"; // خارج المدى المنطقي

            var result = CsvImportService.Import(csv);

            Assert.Equal(0, result.RowsImported);
            Assert.Single(result.Errors);
        }

        [Fact]
        public void Import_BlankPercentage_TreatedAsNullNotAsError()
        {
            // مقرر لسه معندوش درجة - ده سيناريو مقبول (زي مادة مسجلة في ترم جاي)
            var csv = "اسم المقرر,الساعات المعتمدة,النسبة\nمقرر مستقبلي,3,";

            var result = CsvImportService.Import(csv);

            Assert.Equal(1, result.RowsImported);
            Assert.Empty(result.Errors);
            Assert.Null(result.ImportedSemesters.Single().Courses.Single().Percentage);
        }

        [Fact]
        public void Import_QuotedFieldWithEmbeddedComma_ParsedAsSingleField()
        {
            // اسم المقرر محاط بتنصيص وفيه فاصلة جواه - لازم يتقرا كحقل واحد، مش ينقسم غلط
            var csv = "اسم المقرر,الساعات المعتمدة,النسبة\n\"مقرر تمهيدي, جزء أول\",3,80";

            var result = CsvImportService.Import(csv);

            Assert.Equal(1, result.RowsImported);
            Assert.Equal("مقرر تمهيدي, جزء أول", result.ImportedSemesters.Single().Courses.Single().CourseName);
        }

        [Fact]
        public void Import_NoSemesterColumn_DefaultsAllRowsToSingleSemester()
        {
            var csv =
                "اسم المقرر,الساعات المعتمدة,النسبة\n" +
                "الجبر,4,85\n" +
                "فيزياء,3,90";

            var result = CsvImportService.Import(csv);

            Assert.Single(result.ImportedSemesters);
            Assert.Equal("مستورد", result.ImportedSemesters.Single().Name);
            Assert.Equal(2, result.ImportedSemesters.Single().Courses.Count);
        }

        [Fact]
        public void Import_EnglishHeaderAliases_AlsoRecognized()
        {
            var csv = "CourseName,CreditHours,Percentage\nAlgebra,4,85";

            var result = CsvImportService.Import(csv);

            Assert.Equal(1, result.RowsImported);
            Assert.Empty(result.Errors);
        }
    }
}
