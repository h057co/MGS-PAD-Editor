using System.Windows;

namespace MgsPadEditor.Views
{
    public partial class AddProfileDialog : Window
    {
        public string ProfileName => TxtName.Text;
        public string GuidInput
        {
            get => TxtGuid.Text;
            set => TxtGuid.Text = value;
        }

        public AddProfileDialog()
        {
            InitializeComponent();
            TxtName.Focus();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtName.Text))
            {
                MessageBox.Show("Debes ingresar un nombre para el perfil.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
