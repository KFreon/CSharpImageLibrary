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
                    return ImageFormats.IsExtensionSupported(files[0]);
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
                    var supported = ImageFormats.GetSupportedExtensions(true);
                    foreach (string file in files)
                    {
                        if (!ImageFormats.IsExtensionSupported(file, supported))
                            return false;
                    }

                    return true;
                },

                DropAction = (model, files) => BulkAdd(files)
            };
            InitializeComponent();
            DataContext = vm;

            CloseSavePanel();
            ClosePanelButton.Visibility = Visibility.Collapsed;


            // Prevent maximised window overtaking the taskbar
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            // Setup events for linking image viewbox pan and scroll
            PanZoomLinkButton.Checked += (sender, args) => LoadedImageViewBox.Link(SaveImageViewBox);
            PanZoomLinkButton.Unchecked += (sender, args) => LoadedImageViewBox.Unlink(SaveImageViewBox);

            // Linked by default
            LoadedImageViewBox.Link(SaveImageViewBox);


            // Fix window size if on low res, < 1920x1080
            if (this.Height >= SystemParameters.WorkArea.Height)
                this.Height = SystemParameters.WorkArea.Height - 100; // For a bit of space and in case of taskbar weirdness

            // "Global" exception handler - kills the application if this is hit.
            if (!Debugger.IsAttached)
                Application.Current.DispatcherUnhandledException += (sender, args) =>
                {
                    MessageBox.Show("Unhandled exception occured." + Environment.NewLine + args.Exception);
                    this.Close();  // Might not work I guess, but either way, it's going down.
                };
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            UsefulThings.WPF.WindowBlur.EnableBlur(this);
        }

        string prev_LoadDialogFolder = null;
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = ImageFormats.GetSupportedExtensionsForDialogBoxAsString(),
                Title = "Select image to load",
            };

            if (prev_LoadDialogFolder != null)
                ofd.InitialDirectory = prev_LoadDialogFolder;

            if (ofd.ShowDialog() == true)
            {
                Load(ofd.FileName);
                prev_LoadDialogFolder = Path.GetDirectoryName(ofd.FileName);
            }
        }

        void Load(string filename)
        {
            ConvertButton.Visibility = Visibility.Visible;
            ClosePanelButton.Visibility = Visibility.Collapsed;

            // Reset zoom etc
            SaveImageViewBox.Reset();
            LoadedImageViewBox.Reset();

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

        string prev_bulkBrowseSingleFolder = null;
        private void BulkBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog()
            {
                Filter = ImageFormats.GetSupportedExtensionsForDialogBoxAsString(),
                Title = "Select files to be converted. Needn't be the same format.",
                Multiselect = true
            };

            if (prev_bulkBrowseSingleFolder != null)
                ofd.InitialDirectory = prev_bulkBrowseSingleFolder;

            if (ofd.ShowDialog() == true)
            {
                BulkAdd(ofd.FileNames);
                prev_bulkBrowseSingleFolder = Path.GetDirectoryName(ofd.FileNames[0]);
            }
        }

        private async void BulkConvertButton_Click(object sender, RoutedEventArgs e)
        {
            await vm.DoBulkConvert();
        }

        private void BulkCloseButton_Click(object sender, RoutedEventArgs e)
        {
            vm.BulkConvertFiles.Clear();
            vm.BulkConvertFailed.Clear();
            vm.BulkConvertOpen = false;
            vm.BulkConvertFinished = false;
        }

        private void BulkConvertOpenButton_Click(object sender, RoutedEventArgs e)
        {
            vm.BulkConvertOpen = true;
        }

        string prev_bulkSaveDialogFolder = null;
        private void BulkSaveBrowse_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog folderBrowser = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Title = "Select folder to save converted files to."
            };

            if (prev_bulkSaveDialogFolder != null)
                folderBrowser.InitialDirectory = prev_bulkSaveDialogFolder;

            if (folderBrowser.ShowDialog() == CommonFileDialogResult.Ok)
            {
                vm.BulkSaveFolder = folderBrowser.FileName;
                prev_bulkSaveDialogFolder = folderBrowser.FileName;
            }
        }

        private void BulkConvertListBox_KeyDown(object sender, KeyEventArgs e)
        {
            var box = sender as ListBox;
            if (box == null)
                return;

            if (e.Key == Key.Delete)
                vm.BulkConvertFiles.RemoveRange(box.SelectedItems.Cast<string>());
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
                ImageEngineFormat selectedFormat = (ImageEngineFormat)value;
                bool disableContainer = false;

                // Check supported save formats.
                if (ImageFormats.SaveUnsupported.Contains(selectedFormat))
                    disableContainer = true;

                if (disableContainer)
                {
                    var container = (ComboBoxItem)(box.ItemContainerGenerator.ContainerFromItem(item));
                    container.IsEnabled = false;
                }
            }
        }

        private void TOPWINDOW_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (vm.InfoPanelOpen)
                    vm.InfoPanelOpen = false;
                else if (vm.SettingsPanelOpen)
                    vm.SettingsPanelOpen = false;
                else if (vm.BulkConvertOpen)
                    vm.BulkConvertOpen = false;
                else
                    vm.CloseCommand.Execute(null);
            }
        }

        private void ImageCloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset scaling and position
            LoadedImageViewBox.Reset();
            SaveImageViewBox.Reset();
        }

        private void TOPWINDOW_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            UsefulThings.WPF.General.DoBorderlessWindowDragMove(this, e);
        }

        private void TOPWINDOW_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine();
        }

        private void SavePathBox_LostFocus(object sender, RoutedEventArgs e)
        {
            vm.FixExtension(true);  // Indicate property should notify
        }

        string prev_bulkBrowseManyFolder = null;
        private void BulkFolderBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                Title = "Select folder to add",
            };

            if (prev_bulkBrowseManyFolder != null)
                fbd.InitialDirectory = prev_bulkBrowseManyFolder;

            if (fbd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                BulkAdd(Directory.EnumerateFiles(fbd.FileName, "*", vm.BulkFolderBrowseRecurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
                prev_bulkBrowseManyFolder = fbd.FileName;
            }
        }

        void BulkAdd(IEnumerable<string> files)
        {
            List<string> newFiles = new List<string>();

            int count = 0;
            foreach (var file in files.Where(t => ImageFormats.IsExtensionSupported(t)))
            {
                // Prevent duplicates
                if (!vm.BulkConvertFiles.Contains(file, StringComparison.OrdinalIgnoreCase))
                {
                    newFiles.Add(file);
                    count++;
                }
            }

            vm.BulkConvertFiles.AddRange(newFiles);

            if (count == 0)
                vm.BulkStatus = "No suitable files found.";
            else
                vm.BulkStatus = $"Added {count} files.";
        }

        private void SettingsPanelOpenButton_Click(object sender, RoutedEventArgs e)
        {
            vm.SettingsPanelOpen = true;
        }

        private void SettingsPanelCloseButton_Click(object sender, RoutedEventArgs e)
        {
            vm.SettingsPanelOpen = false;
        }

        private void InfoPanelOpenButton_Click(object sender, RoutedEventArgs e)
        {
            vm.InfoPanelOpen = true;
        }

        private void InfoPanelCloseButton_Click(object sender, RoutedEventArgs e)
        {
            vm.InfoPanelOpen = false;
        }
    }
}
