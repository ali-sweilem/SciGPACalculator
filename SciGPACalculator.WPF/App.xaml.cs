using System.Windows;
using GpaCalculator.Core.Persistence;
using GpaCalculator.WPF.ViewModels;

namespace GpaCalculator.WPF
{
    public partial class App : Application
    {
        /// <summary>
        /// هنا مكان "تجميع" المشروع كله: بنعمل الـ Repository، وبنمرره لـ MainViewModel،
        /// وبنمرر الـ ViewModel ده لـ MainWindow. من غير Container، لأن المشروع صغير
        /// وده أوضح للفهم من إضافة مكتبة DI مالهاش داعي دلوقتي.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var repository = new JsonAcademicRecordRepository();
            var mainViewModel = new MainViewModel(repository);

            var mainWindow = new MainWindow(mainViewModel);
            mainWindow.Show();
        }
    }
}
