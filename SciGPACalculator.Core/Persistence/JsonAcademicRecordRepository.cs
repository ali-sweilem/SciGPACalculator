using System.Text.Json;
using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Persistence
{
    public class JsonAcademicRecordRepository : IAcademicRecordRepository
    {
        private readonly string _filePath;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true
        };

        /// <summary>
        /// If no file path is provided, a default path in %AppData% is used.
        /// The data is stored outside the project folder so it remains available
        /// even if the project is moved or rebuilt.
        ///
        /// Providing a file path explicitly is useful for unit tests,
        /// allowing a temporary file to be used instead of the actual data file.
        /// </summary>
        public JsonAcademicRecordRepository(string? filePath = null)
        {
            _filePath = filePath ?? GetDefaultFilePath();
        }

        private static string GetDefaultFilePath()
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GpaCalculator");

            Directory.CreateDirectory(folder);
            return Path.Combine(folder, "academic-record.json");
        }

        public AcademicRecord Load()
        {
            if (!File.Exists(_filePath))
                return new AcademicRecord();

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return new AcademicRecord();

            try
            {
                return JsonSerializer.Deserialize<AcademicRecord>(json, SerializerOptions)
                       ?? new AcademicRecord();
            }
            catch (JsonException ex)
            {
                // Do not let the user think their data was silently deleted.
                // Instead, clearly indicate that the data file is corrupted.
                throw new InvalidDataException(
                    $"ملف البيانات تالف أو بصيغة غير صالحة: {_filePath}", ex);
            }
        }

        public void Save(AcademicRecord record)
        {
            var json = JsonSerializer.Serialize(record, SerializerOptions);

            var tempFilePath = _filePath + ".tmp";
            File.WriteAllText(tempFilePath, json);

            // Atomic replacement: if the application crashes before this line,
            // the original file remains unchanged.
            File.Move(tempFilePath, _filePath, overwrite: true);
        }
    }
}