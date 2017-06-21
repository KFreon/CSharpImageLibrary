using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UI_Project
{
    /// <summary>
    /// Interaction logic for MyButtonTest.xaml
    /// </summary>
    public partial class DropFriendlyButton : Button
    {
        Brush origBorderBrush = null;


        public DropFriendlyButton()
        {
            InitializeComponent();


            //Save initial border colour for later.
            origBorderBrush = BorderBrush;
        }

        protected override void OnDragOver(DragEventArgs e)
        {
            base.OnDragOver(e);

            // Change Border Colour when dragging over
            BorderBrush = Brushes.Red;
        }

        protected override void OnDragLeave(DragEventArgs e)
        {
            base.OnDragLeave(e);
            ResetDragEffects();
        }

        protected override void OnDrop(DragEventArgs e)
        {
            base.OnDrop(e);
            ResetDragEffects();
        }

        void ResetDragEffects()
        {
            // Reset drag effects
            BorderBrush = origBorderBrush;
        }
    }
}
