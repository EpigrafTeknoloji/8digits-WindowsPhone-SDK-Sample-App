using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using EightDigitsTest.Resources;
using EightDigitsTest.ViewModels;
using EightDigits;

namespace EightDigitsTest {
    public partial class MainPage : PhoneApplicationPage {
        // Constructor
        public MainPage() {

            InitializeComponent();

            // Set the data context of the LongListSelector control to the sample data
            DataContext = App.ViewModel;
            this.Title = "Main";
            
            // Sample code to localize the ApplicationBar
            BuildLocalizedApplicationBar();
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            if (!App.ViewModel.IsDataLoaded) {
                App.ViewModel.LoadData();
            }
            this.StartHit();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            base.OnNavigatedFrom(e);
            this.EndHit();
        }

        // Handle selection changed on LongListSelector
        private void MainLongListSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // If selected item is null (no selection) do nothing
            if (MainLongListSelector.SelectedItem == null)
                return;

            // Navigate to the new page
            NavigationService.Navigate(new Uri("/DetailsPage.xaml?selectedItem=" + (MainLongListSelector.SelectedItem as ItemViewModel).ID, UriKind.Relative));

            // Reset selected item to null (no selection)
            MainLongListSelector.SelectedItem = null;
        }


        private void BuildLocalizedApplicationBar() {
            ApplicationBar = new ApplicationBar();
            ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem("Visitor");
            appBarMenuItem.Click += appBarMenuItem_Click;
            ApplicationBar.MenuItems.Add(appBarMenuItem);
        }

        void appBarMenuItem_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/VisitorPage.xaml", UriKind.Relative));
        }
    }
}