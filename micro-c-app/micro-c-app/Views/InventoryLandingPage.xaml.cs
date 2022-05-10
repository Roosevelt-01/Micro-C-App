﻿using micro_c_app.Views.InventoryAudit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace micro_c_app.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class InventoryLandingPage : ContentPage
    {
        public List<LandingItem> Items { get; set; }

        public ICommand Clicked { get; }
        public InventoryLandingPage()
        {
            BindingContext = this;
            Items = new List<LandingItem>()
            {
                new LandingItem("Scan", "Scan products into inventory.", "\uf030"),
                new LandingItem("Zero Stock", "Lists products which have been scanned into inventory which currently display zero stock on microcenter.com.", "\uf059"),
                new LandingItem("Unknown Location", "Items which have not been scanned into inventory, but are listed as in stock on MicroCenter.com.", "\uf14e"),
                new LandingItem("Consolidation", "This displays items which have been scanned in multiple areas.  Consider consolidating them into fewer areas if stock warrants.", "\uf0e8"),
                new LandingItem("Freshness", "This displays the previous time that all sections of a given category have been scanned", "\uf5d0")
            };

            Clicked = new Command<string>(OnClicked);

            InitializeComponent();
        }

        private void OnClicked(string item)
        {
            ContentPage? page = null;
            switch (item)
            {
                case "Scan":
                    Device.BeginInvokeOnMainThread(async () =>
                    {
                        bool allowed = await GoogleVisionBarCodeScanner.Methods.AskForRequiredPermission();
                        if (!allowed)
                        {
                            return;
                        }

                        await Navigation.PushAsync(new InventoryView());
                    });
                    break;
                case "Zero Stock":
                    page = new ZeroStockPage();
                    break;
                case "Unknown Location":
                    page = new UnknownLocationPage();
                    break;
                case "Consolidation":
                    page = new ConsolidationPage();
                    break;
                case "Freshness":
                    page = new FreshnessPage();
                    break;
            }
            if (page != null)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    await Navigation.PushAsync(page);
                });
            }
        }
    }

    public class LandingItem
    {
        public string Name { get; }
        public string Description { get; }
        public string Icon { get; }

        public LandingItem(string name, string description, string icon)
        {
            Name = name;
            Description = description;
            Icon = icon;
        }
    }
}