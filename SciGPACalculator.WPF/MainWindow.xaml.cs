using System.ComponentModel;
using System.Windows;
using GpaCalculator.WPF.ViewModels;

namespace GpaCalculator.WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        /// <summary>
        /// حفظ تلقائي وقت قفل البرنامج، بدل ما نطلب من المستخدم يدوس زرار Save بنفسه
        /// كل مرة. لو حبينا زرار Save يدوي زيادة في الاطمئنان، ممكن نضيفه بعدين بسهولة.
        /// </summary>
        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SaveAll();
            }

            base.OnClosing(e);
        }
    }
}
