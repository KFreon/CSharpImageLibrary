using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Toolkit.Uwp.UI.Controls;

using UWP_UI_Project.Helpers;
using UWP_UI_Project.Models;
using UWP_UI_Project.Services;

namespace UWP_UI_Project.ViewModels
{
    public class MaybeBulkConvertViewModel : Observable
    {
        private SampleOrder _selected;

        public SampleOrder Selected
        {
            get { return _selected; }
            set { Set(ref _selected, value); }
        }

        public ObservableCollection<SampleOrder> SampleItems { get; private set; } = new ObservableCollection<SampleOrder>();

        public MaybeBulkConvertViewModel()
        {
        }

        public async Task LoadDataAsync(MasterDetailsViewState viewState)
        {
            SampleItems.Clear();

            var data = await SampleDataService.GetSampleModelDataAsync();

            foreach (var item in data)
            {
                SampleItems.Add(item);
            }

            if (viewState == MasterDetailsViewState.Both)
            {
                Selected = SampleItems.First();
            }
        }
    }
}
