using CSharpImageLibrary;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
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
        UsefulThings.WPF.DragDropHandler<NewViewModel> MergeDropHandler = null;

        public NewMainWindow()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ProfileOptimization.SetProfileRoot(path);
            ProfileOptimization.StartProfile("Startup.Profile_ImageEngine_UI");

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
                else if (args.PropertyName == nameof(vm.UseHighQualityScaling))
                {
                    BitmapScalingMode mode = BitmapScalingMode.HighQuality;
                    if (!vm.UseHighQualityScaling)
                        mode = BitmapScalingMode.NearestNeighbor;

                    RenderOptions.SetBitmapScalingMode(LoadedImageImage, mode);
                    RenderOptions.SetBitmapScalingMode(SaveImageImage, mode);
                }
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

            MergeDropHandler = new UsefulThings.WPF.DragDropHandler<NewViewModel>(this)
            {
                DropValidator = BulkDropDragHandler.DropValidator,  // Same validator as bulk
                DropAction = (model, files) => Task.Run(() => vm.MergeLoad(files))
            };

            InitializeComponent();
            DataContext = vm;

            CloseSavePanel();
            ClosePanelButton.Visibility = Visibility.Collapsed;


            // Prevent maximised window overtaking the taskbar
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight - 5;

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

            // Make sure Minimise/Maximise functionality from dragging the title bar is connected to any margin adjustments required.
            this.StateChanged += (sender, args) => WindowMinMaxButton_Click(sender, null);


            // Handle Command Line parameters
            string[] cmdLineParams = Environment.GetCommandLineArgs();

            // For now, just loading image.
            if (cmdLineParams.Length != 1)
            {
                // Since only loading, just use the first one.
                Load(cmdLineParams[2]);
            }
        }

        void CloseSavePanel()
        {
            // Only animate if not already closed.
            if (SecondColumn.Width == new GridLength(0, GridUnitType.Star))
                return;

            vm.SavePanelOpen = false;

            Storyboard closer = FindStoryBoard(SecondColumn, "SecondColumnCloser");
            closer.Begin(SecondColumn);
        }

        void OpenSavePanel()
        {
            // Only animate if not already closed.
            if (SecondColumn.Width != new GridLength(0, GridUnitType.Star))
                return;

            vm.SavePanelOpen = false;

            Storyboard opener = FindStoryBoard(SecondColumn, "SecondColumnOpener");
            opener.Begin(SecondColumn);
        }

        Storyboard FindStoryBoard<T>(T element, string storyboardName) where T : FrameworkContentElement
        {
            return (Storyboard)element.FindResource(storyboardName);
        }

        private void WindowMinMaxButton_Click(object sender, RoutedEventArgs e)
        {
            // Only do this if coming from the button.
            if (e != null)
                this.WindowState = this.WindowState == WindowState.Normal ? WindowState.Maximized : WindowState.Normal;

            if (this.WindowState == WindowState.Maximized)
                MainGrid.Margin = new Thickness(10, 7, 10,7);
            else
                MainGrid.Margin = new Thickness(0,0,3,5);
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
            if (vm.IsWindowBlurred)
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
            OpenSavePanel();
            vm.SavePanelOpen = true;
            ConvertButton.Visibility = Visibility.Collapsed;
            ClosePanelButton.Visibility = Visibility.Visible;

            vm.WindowTitle = "ImageEngine - View and Convert";
            vm.LoadDuration = ""; // Blank this out as it's no longer meaningful, and takes up space.
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

            CloseSavePanel();

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
            PerformBulkBrowse();
        }

        internal bool PerformBulkBrowse()
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
                return true;
            }

            return false;
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
                else if (vm.MergeChannelsPanelOpen)
                    vm.MergeChannelsPanelOpen = false;
                else
                    vm.CloseCommand.Execute(null);
            }
            else if(e.Key == Key.PageDown)
            {
                if (vm.IsImageLoaded)
                {
                    if (vm.MipIndex != 0)
                        vm.MipIndex--;
                }
            }
            else if(e.Key == Key.PageUp)
            {
                if (vm.IsImageLoaded)
                {
                    if (vm.MipIndex < vm.MipCount)
                        vm.MipIndex++;
                }
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


        private void ChannelMergerOpenButton_Click(object sender, RoutedEventArgs e)
        {
            vm.MergeChannelsPanelOpen = true;
        }

        private void MergeRedSelector_Click(object sender, RoutedEventArgs e)
        {
            var item = (MergeChannelsImage)((Button)e.OriginalSource).DataContext;

            // Set red on item exclusively
            item.IsRed = true;
            item.IsGreen = false;
            item.IsBlue = false;
            item.IsAlpha = false;

            // Make sure nothing else has it
            foreach (var image in vm.MergeChannelsImages)
            {
                if (image == item)
                    continue;

                image.IsRed = false;
            }
        }

        private void MergeGreenSelector_Click(object sender, RoutedEventArgs e)
        {
            var item = (MergeChannelsImage)((Button)e.OriginalSource).DataContext;

            // Set green on item exclusively
            item.IsRed = false;
            item.IsGreen = true;
            item.IsBlue = false;
            item.IsAlpha = false;

            // Make sure nothing else has it
            foreach (var image in vm.MergeChannelsImages)
            {
                if (image == item)
                    continue;

                image.IsGreen = false;
            }
        }

        private void MergeBlueSelector_Click(object sender, RoutedEventArgs e)
        {
            var item = (MergeChannelsImage)((Button)e.OriginalSource).DataContext;

            // Set blue on item exclusively
            item.IsRed = false;
            item.IsGreen = false;
            item.IsBlue = true;
            item.IsAlpha = false;

            // Make sure nothing else has it
            foreach (var image in vm.MergeChannelsImages)
            {
                if (image == item)
                    continue;

                image.IsBlue = false;
            }
        }

        private void MergeAlphaSelector_Click(object sender, RoutedEventArgs e)
        {
            var item = (MergeChannelsImage)((Button)e.OriginalSource).DataContext;

            // Set alpha on item exclusively
            item.IsRed = false;
            item.IsGreen = false;
            item.IsBlue = false;
            item.IsAlpha = true;

            // Make sure nothing else has it
            foreach (var image in vm.MergeChannelsImages)
            {
                if (image == item)
                    continue;

                image.IsAlpha = false;
            }
        }

        int mergeStart = 0;
        string prev_MergeLoadDialogFolder = null;
        private async void MergeLoadButton_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog fb = new CommonOpenFileDialog()
            {
                Multiselect = true,
                Title = "Select images to load as channels."
            };

            if (prev_MergeLoadDialogFolder != null)
                fb.InitialDirectory = prev_MergeLoadDialogFolder;

            if (fb.ShowDialog() == CommonFileDialogResult.Ok)
            {
                await Task.Run(() => vm.MergeLoad(fb.FileNames));
                prev_MergeLoadDialogFolder = Path.GetDirectoryName(fb.FileNames.First());
                mergeStart = vm.MergeChannelsImages.Count;
            }
        }

        private void MergeDeselector_Click(object sender, RoutedEventArgs e)
        {
            var item = (MergeChannelsImage)((Button)e.OriginalSource).DataContext;

            // Clear everything
            item.IsRed = false;
            item.IsGreen = false;
            item.IsBlue = false;
            item.IsAlpha = false;
        }

        private void MergeCloseButton_Click(object sender, RoutedEventArgs e)
        {
            vm.MergeChannelsPanelOpen = false;
            vm.MergeChannelsImages.Clear();
        }

        private async void MergeMergeButton_Click(object sender, RoutedEventArgs e)
        {
            await vm.MergeChannels();
            vm.MergeChannelsImages.Clear();
        }

        private void MergeChannelPanel_Drop(object sender, DragEventArgs e)
        {
            MergeDropHandler.Drop(sender, e);
        }

        private void MergeChannelPanel_DragOver(object sender, DragEventArgs e)
        {
            MergeDropHandler.DragOver(e);
        }

        private void UseWindowTransparencyChecker_Checked(object sender, RoutedEventArgs e)
        {
            UsefulThings.WPF.WindowBlur.EnableBlur(this);
            vm.IsWindowBlurred = true;
        }

        private void UseWindowTransparencyChecker_Unchecked(object sender, RoutedEventArgs e)
        {
            UsefulThings.WPF.WindowBlur.DisableBlur(this);
            vm.IsWindowBlurred = false;
        }

        private void TOPWINDOW_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.NumThreads = vm.EnableThreading ? vm.NumThreads : 1;
            Properties.Settings.Default.BackgroundAlpha = vm.WindowBackground_Alpha;
            Properties.Settings.Default.BackgroundRed = vm.WindowBackground_Red;
            Properties.Settings.Default.BackgroundGreen = vm.WindowBackground_Green;
            Properties.Settings.Default.BackgroundBlue = vm.WindowBackground_Blue;
            Properties.Settings.Default.UseWindowsCodecs = vm.UseWindowsCodecs;
            Properties.Settings.Default.IsWindowBlurred = vm.IsWindowBlurred;
            Properties.Settings.Default.Save();
        }

        private void Settings_ColoursDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            vm.WindowBackground_Alpha = 158;
            vm.WindowBackground_Red = 0;
            vm.WindowBackground_Green = 0;
            vm.WindowBackground_Blue = 0;
            UseWindowTransparencyChecker_Checked(null, null); // Blur background
        }

        private void HelpAboutButton_Click(object sender, RoutedEventArgs e)
        {
            vm.ShowHelpAbout = true;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            vm.ShowHelpAbout = false;
        }

        private void PropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            // Want to animate this but it starts at the bottom all the time. Unknown reason, perhaps in gridlengthanimation?
            if (InfoRow.Height.Value == 1.2)
                InfoRow.Height = new GridLength(80, GridUnitType.Pixel);
            else
                InfoRow.Height = new GridLength(1.2, GridUnitType.Star);
        }

        private void LoadingLayerCancelButton_Click(object sender, RoutedEventArgs e)
        {
            vm.Cancel();
        }

        private void TopLevelBulkConvertLoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (PerformBulkBrowse())
                vm.BulkConvertOpen = true;
        }
    }
}
