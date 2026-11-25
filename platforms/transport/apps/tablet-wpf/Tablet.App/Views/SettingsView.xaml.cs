using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Tablet.App.ViewModels;

namespace Tablet.App.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    private void SelectCertificateFolder(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
            return;

        var dialog = new OpenFolderDialog { Title = "MobED 인증서 폴더 선택" };
        var currentFolder = CertificateFolderInput.Text.Trim();
        if (Directory.Exists(currentFolder))
            dialog.InitialDirectory = currentFolder;

        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
            viewModel.SetCertificateFolder(dialog.FolderName);
    }

    private async void SaveAndTest(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SettingsViewModel viewModel)
            return;

        CertificateFolderInput.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        if (await viewModel.SaveAndTestAsync(ApiKeyInput.Password))
            ApiKeyInput.Clear();
    }
}
