using GpaCalculator.Core.Models;

namespace GpaCalculator.Core.Persistence
{
    /// <summary>
    /// Defines the interface used by the application to save and load academic data.
    /// The rest of the application, including future ViewModels, does not need to know
    /// how the data is stored.
    ///
    /// Currently, the data is stored as JSON.
    /// If the storage is changed to SQLite later, only the class implementing
    /// this interface needs to be updated.
    /// </summary>
    public interface IAcademicRecordRepository
    {
        AcademicRecord Load();
        void Save(AcademicRecord record);
    }
}