using CSharpImageLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static CSharpImageLibrary.ImageFormats;

namespace UI_Project
{
    partial class PrimaryResourceDictionary : ResourceDictionary
    {
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
                var details = new ImageEngineFormatDetails(selectedFormat);
                if (!details.ValidSaveFormat)
                    disableContainer = true;

                if (disableContainer)
                {
                    var container = (ComboBoxItem)(box.ItemContainerGenerator.ContainerFromItem(item));
                    container.IsEnabled = false;
                }
            }
        }
    }
}
