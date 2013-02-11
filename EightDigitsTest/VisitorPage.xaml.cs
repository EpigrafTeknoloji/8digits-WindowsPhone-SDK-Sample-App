using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using EightDigits;

namespace EightDigitsTest {
    public partial class VisitorPage : PhoneApplicationPage {
        public VisitorPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            this.GetHit().Title = "VisitorPage";
            this.GetHit().Path = "/visitor";
            this.GetHit().Start();
            TitleBlock.Text = "visitor " + Visitor.Current.VisitorCode;

            if (Visitor.Current.Score == Visitor.ScoreNotLoaded) {
                Visitor.Current.OnScoreLoaded += Current_OnScoreLoaded;
                Visitor.Current.LoadScore();
            }

            else {
                VisitorScoreBlock.Text = Visitor.Current.Score.ToString();
            }

        }

        void Current_OnBadgesLoaded(Visitor sender, VisitorEventArgs e) {
            if (e.Error != null) {
                // Error is not nil, badge load failed, do something with the error
            }
            else {
            }
        }

        void Current_OnScoreLoaded(Visitor sender, VisitorEventArgs e) {
            if (e.Error != null) {
                // Score load failed, do something with the error
            }

            else {
                VisitorScoreBlock.Text = Visitor.Current.Score.ToString();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            base.OnNavigatedFrom(e);
            this.GetHit().End();
        }

    }
}