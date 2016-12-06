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
        // TODO: Double click to maximise and minimise
        // TODO: Link zoom and pan on things

        public NewViewModel vm { get; private set; }

        UsefulThings.WPF.DragDropHandler<NewViewModel> DragDropHandler = null;
        UsefulThings.WPF.DragDropHandler<NewViewModel> BulkDropDragHandler = null;



        public NewMainWindow()
        {
            vm = new NewViewModel();

            DragDropHandler = new UsefulThings.WPF.DragDropHandler<NewViewModel>(this)
            {
                DropValidator = new Predicate<string[]>(files =>
                {
                // Check only one file - can only load one, so restrict to one.
                if (files.Length != 1)
                        return false;

                // Check file extension
                if (!ImageFormats.GetSupportedExtensions().Contains(Path.GetExtension(files[0]).Replace(".", ""), StringComparison.OrdinalIgnoreCase))  // Checks extension, ignoring '.'
                    return false;

                    return true;
                }),

                DropAction = new Action<NewViewModel, string[]>((viewModel, filePath) =>
                {
                    if (filePath?.Length < 1)
                        return;

                    Load(filePath[0]);
                })
            };

            vm.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(vm.IsImageLoaded) && !vm.IsImageLoaded)
                    CloseSavePanel();
            };

            BulkDropDragHandler = new UsefulThings.WPF.DragDropHandler<NewViewModel>(this)
            {
                DropValidator = files =>
                {
                    if (files == null || files.Length == 0)
                        return false;

                // Check all extensions
                foreach (string file in files)
                    {
                        if (!ImageFormats.GetSupportedExtensions().Contains(Path.GetExtension(file).Replace(".", ""), StringComparison.OrdinalIgnoreCase))  // Checks extension, ignoring '.'
                        return false;
                    }

                    return true;
                },

                DropAction = (model, files) => model.BulkConvertFiles.AddRange(files)
            };
            InitializeComponent();
            DataContext = vm;

            CloseSavePanel();
            ClosePanelButton.Visibility = Visibility.Collapsed;


            // Prevent maximised window overtaking the taskbar
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
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
            UsefulThings.WPF.General.DoBorderlessWindowDragMove(this, e);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UsefulThings.WPF.WindowBlur.EnableBlur(this);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = ImageFormats.GetSupportedExtensionsForDialogBoxAsString(),
                Title = "Select image to load"
            };
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
            SaveFileDialog sfd = new SaveFileDialog()
            {
                InitialDirectory = Path.GetDirectoryName(vm.SavePath),
                FileName = Path.GetFileName(vm.SavePath),
                Title = "Select save destination - Don't worry about File Extension. It'll get updated."  // TODO: Have format option here perhaps? Maybe just allow selection of format here?
            };
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
            BulkDropDragHandler.DragOver(e);
        }

        private void BulkConvertListBox_Drop(object sender, DragEventArgs e)
        {
            BulkDropDragHandler.Drop(sender, e);
        }

        private void BulkBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = ImageFormats.GetSupportedExtensionsForDialogBoxAsString(),
                Title = "Select files to be converted. Needn't be the same format."
            };
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
            vm.BulkConvertFinished = false;
            vm.BulkConvertFiles.Clear();
            vm.BulkConvertFailed.Clear();
            vm.BulkConvertSkipped.Clear();
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

        private void BulkConvertListBox_KeyDown(object sender, KeyEventArgs e)
        {
            var box = sender as ListBox;
            if (box == null)
                return;

            if (e.Key == Key.Delete)
            {
                for (int i = 0; i < box.SelectedItems.Count; i++)
                    vm.BulkConvertFiles.Remove((string)box.SelectedItems[i]);
            }
        }

        private void SaveFormatCombo_DropDownOpened(object sender, EventArgs e)
        {
            var box = (ComboBox)sender;
            if (box.ItemContainerGenerator.Status == System.Windows.Controls.Primitives.GeneratorStatus.NotStarted)
            {
                box.ItemContainerGenerator.StatusChanged += (s, args) =>
                 {
                     var generator = (ItemContainerGenerator)s;
                     if (generator.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                        DisableUnsupportedFormats(box);
                 };
            }
        }

        void DisableUnsupportedFormats(ComboBox box)
        {
            foreach (var item in box.Items)
            {
                var value = UsefulThings.WPF.EnumToItemsSource.GetValueBack(item);

                if (ImageFormats.SaveUnsupported.Contains((ImageEngineFormat)value))
                {
                    var container = (ComboBoxItem)(box.ItemContainerGenerator.ContainerFromItem(item));
                    container.IsEnabled = false;
                }
            }
        }
    }
}
