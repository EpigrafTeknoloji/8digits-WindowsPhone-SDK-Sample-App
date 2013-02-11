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
using EightDigits;

namespace EightDigitsTest {
    public partial class DetailsPage : PhoneApplicationPage {
        // Constructor
        public DetailsPage() {
            
            InitializeComponent();

            this.Title = "Detail";
            this.GetHit().Path = "/detail";

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        // When page is navigated to set data context to selected item in list
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            if (DataContext == null) {
                string selectedIndex = "";
                if (NavigationContext.QueryString.TryGetValue("selectedItem", out selectedIndex)) {
                    int index = int.Parse(selectedIndex);
                    DataContext = App.ViewModel.Items[index];
                }
            }

            this.StartHit();
   
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            base.OnNavigatedFrom(e);
            this.EndHit();
        }

        // Trigger an event associated with this PhoneApplicationPage object
        private void PageEventButton_Click(object sender, RoutedEventArgs e) {
            this.TriggerEvent("test-event", "page-event");
        }

        // Trigger a custom event by creating an Event object and sending it a Trigger message
        private void CustomEventButton_Click(object sender, RoutedEventArgs e) {
            Event customEvent = new Event("test-event", "custom-event", this.GetHit());
            customEvent.Trigger();
        }

        // Trigger an event which is not associated with any hits or pages by sending the current Visit object a TriggerEvent message
        private void AnonymousEventButton_Click(object sender, RoutedEventArgs e) {
            Visit.Current.TriggerEvent("test-event", "anonymous-event");
        }


        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}