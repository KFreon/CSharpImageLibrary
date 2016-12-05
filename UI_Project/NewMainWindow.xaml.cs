using CSharpImageLibrary;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using UsefulThings;

namespace UI_Project
{
    /// <summary>
    /// Interaction logic for NewMainWindow.xaml
    /// </summary>
    public partial class NewMainWindow : Window
    {
        public NewViewModel vm { get; private set; }

        UsefulThings.WPF.DragDropHandler<NewViewModel> DragDropHandler = null;

        public NewMainWindow()
        {
            vm = new NewViewModel();

            DragDropHandler = new UsefulThings.WPF.DragDropHandler<NewViewModel>(this);
            DragDropHandler.DropValidator = new Predicate<string[]>(files =>
            {
                // Check only one file - can only load one, so restrict to one.
                if (files.Length != 1)
                    return false;

                // Check file extension
                if (!ImageFormats.GetSupportedExtensions().Contains(Path.GetExtension(files[0]).Replace(".", ""), StringComparison.OrdinalIgnoreCase))  // Checks extension, ignoring '.'
                    return false;

                return true;
            });

            DragDropHandler.DropAction = new Action<NewViewModel, string[]>((viewModel, filePath) =>
            {
                if (filePath?.Length < 1)
                    return;

                Load(filePath[0]);
            });

            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(vm.IsImageLoaded) && !vm.IsImageLoaded)
                    CloseSavePanel();
            };

            InitializeComponent();
            DataContext = vm;

            CloseSavePanel();
            ClosePanelButton.Visibility = Visibility.Collapsed;
        }

        void CloseSavePanel()
        {
            // Only animate if not already closed.
            if (SecondColumn.Width == new GridLength(0, GridUnitType.Star))
                return;

            Storyboard closer = (Storyboard)SecondColumn.FindResource("SecondColumnCloser");
            closer.Begin(SecondColumn);
        }

        private void WindowMinMaxButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;
        }

        private void WindowMinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void WindowCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UsefulThings.WPF.WindowBlur.EnableBlur(this);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = ImageFormats.GetSupportedExtensionsForDialogBoxAsString();
            ofd.Title = "Select image to load";
            if (ofd.ShowDialog() == true)
                Load(ofd.FileName);
        }

        void Load(string filename)
        {
            ConvertButton.Visibility = Visibility.Visible;
            ClosePanelButton.Visibility = Visibility.Collapsed;

            vm.LoadImage(filename);
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            ConvertButton.Visibility = Visibility.Collapsed;
            ClosePanelButton.Visibility = Visibility.Visible;

            vm.WindowTitle = "ImageEngine - View and Convert";
            vm.UpdateSavePreview();
        }

        private void SavePathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.InitialDirectory = Path.GetDirectoryName(vm.SavePath);
            sfd.FileName = Path.GetFileName(vm.SavePath);
            sfd.Title = "Select save destination - Don't worry about File Extension. It'll get updated.";  // TODO: Have format option here perhaps? Maybe just allow selection of format here?

            if (sfd.ShowDialog() == true)
                vm.SavePath = sfd.FileName;
        }

        private void TOPWINDOW_DragOver(object sender, DragEventArgs e)
        {
            DragDropHandler.DragOver(e);
        }

        private void TOPWINDOW_Drop(object sender, DragEventArgs e)
        {
            DragDropHandler.Drop(sender, e);
        }

        private void ClosePanelButton_Click(object sender, RoutedEventArgs e)
        {
            ConvertButton.Visibility = Visibility.Visible;
            ClosePanelButton.Visibility = Visibility.Collapsed;

            vm.WindowTitle = "ImageEngine - View"; 
        }

        private void BulkConvertListBox_DragOver(object sender, DragEventArgs e)
        {

        }

        private void BulkConvertListBox_Drop(object sender, DragEventArgs e)
        {

        }

        private void BulkBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = ImageFormats.GetSupportedExtensionsForDialogBoxAsString();
            ofd.Title = "Select files to be converted. Needn't be the same format.";

            if (ofd.ShowDialog() == true)
                vm.BulkConvertFiles.AddRange(ofd.FileNames);
        }

        private void BulkConvertButton_Click(object sender, RoutedEventArgs e)
        {
            vm.DoBulkConvert();
        }

        private void BulkCloseButton_Click(object sender, RoutedEventArgs e)
        {
            vm.BulkConvertOpen = false;
        }

        private void BulkConvertOpenButton_Click(object sender, RoutedEventArgs e)
        {
            vm.BulkConvertOpen = true;
        }

        private void BulkSaveBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog folderBrowser = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Title = "Select folder to save converted files to."
            };

            if (folderBrowser.ShowDialog() == CommonFileDialogResult.Ok)
                vm.BulkSaveFolder = folderBrowser.FileName;
        }
    }
}
