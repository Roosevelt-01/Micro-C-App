﻿using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.QueryParsers.Xml.Builders;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using MicroCLib.Models;
using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace MicroCBuilder.Views
{
    public sealed partial class SearchResults : UserControl, INotifyPropertyChanged
    {
        private List<Item> Results { get; set; }

        public int Count => Results.Count;
        public string Query
        {
            get { return (string)GetValue(QueryProperty); }
            set { SetValue(QueryProperty, value); HandleQuery(value); }
        }

        // Using a DependencyProperty as the backing store for Query.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty QueryProperty =
            DependencyProperty.Register("Query", typeof(string), typeof(SearchResults), new PropertyMetadata("", new PropertyChangedCallback(QueryChanged)));



        public List<Item> Items => BuildComponentCache.Current.FromType(ComponentType);
        public BuildComponent.ComponentType ComponentType
        {
            get { return (BuildComponent.ComponentType)GetValue(ComponentTypeProperty); }
            set { SetValue(ComponentTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ComponentType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ComponentTypeProperty =
            DependencyProperty.Register("ComponentType", typeof(BuildComponent.ComponentType), typeof(SearchResults), new PropertyMetadata(BuildComponent.ComponentType.CaseFan, new PropertyChangedCallback(ComponentChanged)));

        private static void ComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is SearchResults comp)
            {
                comp.ComponentUpdated();
            }
        }

        public ICommand ItemSelected
        {
            get { return (ICommand)GetValue(ItemSelectedProperty); }
            set { SetValue(ItemSelectedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemSelected.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemSelectedProperty =
            DependencyProperty.Register("ItemSelected", typeof(ICommand), typeof(SearchResults), new PropertyMetadata(null));

        public delegate void ItemSelectedEventArgs(object sender, Item item);
        public event ItemSelectedEventArgs OnItemSelected;

        public SearchResults()
        {
            this.InitializeComponent();
            Results = new List<Item>();
            DataContext = this;
            dataGrid.CanUserSortColumns = true;

            LocalSearch.Init();
        }

        private void ComponentUpdated()
        {
            Results.Clear();
            dataGrid.ItemsSource = null;

            LocalSearch.ReplaceItems(Items);
        }

        private static void QueryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SearchResults s)
            {
                s.Update();
            }
        }

        public void Update()
        {
            HandleQuery(Query);
        }

        private void HandleQuery(string query)
        {
            Results.Clear();
            if (string.IsNullOrWhiteSpace(query))
            {
                Results = new List<Item>(Items);
            }
            else
            {
                if(query.Length == 6)
                {
                    var skuMatch = Items.FirstOrDefault(i => i.SKU == query);
                    if(skuMatch != null)
                    {
                        Results.Add(skuMatch);
                    }
                }

                Results.AddRange(LocalSearch.Search(query, Items));
                
            }
            dataGrid.ItemsSource = new ObservableCollection<Item>(Results);
            OnPropertyChanged(nameof(Count));
        }

        private bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "", Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        private void dataGrid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var i = dataGrid.SelectedIndex;
            if(i == -1)
            {
                return;
            }
            var source = dataGrid.ItemsSource as ObservableCollection<Item>;
            var item = source[i].CloneAndResetQuantity();
            System.Diagnostics.Debug.WriteLine(item.Name);
            ItemSelected?.Execute(item);
            OnItemSelected?.Invoke(this, item);
        }

        private void dataGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                e.Handled = true;
                dataGrid_DoubleTapped(sender, new DoubleTappedRoutedEventArgs());
            }
        }

        private void dataGrid_Sorting(object sender, Microsoft.Toolkit.Uwp.UI.Controls.DataGridColumnEventArgs e)
        {
            if (e.Column.SortDirection == null || e.Column.SortDirection == DataGridSortDirection.Ascending)
            {
                e.Column.SortDirection = DataGridSortDirection.Descending;
            }
            else
            {
                e.Column.SortDirection = DataGridSortDirection.Ascending;
            }

            foreach(var column in dataGrid.Columns)
            {
                if(column != e.Column)
                {
                    column.SortDirection = null;
                }
            }

            var asc = e.Column.SortDirection == DataGridSortDirection.Ascending;
            Func<Item, object>? sort = null;
            switch (e.Column.Header.ToString())
            {
                case "SKU":
                    sort = (i) => i.SKU;
                    break;
                case "Stock":
                    sort = (i) => i.Stock;
                    break;
                case "Price":
                    sort = (i) => i.Price;
                    break;
                case "Name":
                    sort = (i) => i.Name;
                    break;
                case "Brand":
                    sort = (i) => i.Brand;
                    break;
                default:
                    Debug.WriteLine($"column not sorted {e.Column.Tag}");
                    dataGrid.ItemsSource = new ObservableCollection<Item>(Results);
                    break;
            }

            dataGrid.ItemsSource = asc ? new ObservableCollection<Item>(Results.OrderBy(sort)) : new ObservableCollection<Item>(Results.OrderByDescending(sort));
        }
    }
}
