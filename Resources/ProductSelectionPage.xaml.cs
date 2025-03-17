using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Aquatir
{
    public partial class ProductSelectionPage : ContentPage
    {
        private readonly string _groupName;
        private readonly MainPage _mainPage;
        private List<ProductItem> _allProducts = new List<ProductItem>();
        private ObservableCollection<ProductItem> _displayedProducts = new ObservableCollection<ProductItem>();
        private bool _isLoading = false;
        private int _currentPage = 0;
        private const int PAGE_SIZE = 30;
        private string _searchText = string.Empty;

        public ProductSelectionPage(string groupName, MainPage mainPage)
        {
            InitializeComponent();
            _groupName = groupName;
            _mainPage = mainPage;
            ProductCollectionView.ItemsSource = _displayedProducts;
            if (_groupName == "Вся продукция")
            {
                ProductCollectionView.RemainingItemsThreshold = 5;
                ProductCollectionView.RemainingItemsThresholdReached += OnRemainingItemsThresholdReached;
            }
            
            LoadProducts();
        }

        public async void LoadProducts()
        {
            ShowLoadingIndicator();

            await Task.Run(async () =>
            {
                try
                {
                    bool showPackagedProducts = Preferences.Get("ShowPackagedProducts", true);
                    List<ProductItem> tempProducts = new List<ProductItem>();

                    if (_groupName == "Вся продукция")
                    {
                        foreach (var group in ProductCache.CachedProducts)
                        {
                            var productsInGroup = group.Value.Where(product =>
                            {
                                if (!showPackagedProducts)
                                {
                                    return !product.Name.EndsWith("УП.", StringComparison.OrdinalIgnoreCase);
                                }
                                return true;
                            });

                            tempProducts.AddRange(productsInGroup);
                        }
                    }
                    else
                    {
                        if (ProductCache.CachedProducts.ContainsKey(_groupName))
                        {
                            tempProducts = ProductCache.CachedProducts[_groupName].Where(product =>
                            {
                                if ((_groupName == "Холодное Копчение" || _groupName == "Вяленая Продукция") && !showPackagedProducts)
                                {
                                    return !product.Name.EndsWith("УП.", StringComparison.OrdinalIgnoreCase);
                                }
                                return true;
                            }).ToList();
                        }
                    }

                    if (!string.IsNullOrEmpty(_searchText))
                    {
                        tempProducts = tempProducts
                            .Where(product => product.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    tempProducts = tempProducts.OrderBy(p => p.Name).ToList();

                    // Update all products on background thread
                    _allProducts = tempProducts;

                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        if (_groupName == "Вся продукция")
                        {
                            // Load first page immediately
                            await LoadNextPage();
                        }
                        else
                        {
                            // For smaller groups, just replace the entire collection at once
                            _displayedProducts = new ObservableCollection<ProductItem>(_allProducts);
                            ProductCollectionView.ItemsSource = _displayedProducts;
                        }

                        HideLoadingIndicator();
                    });
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        DisplayAlert("Ошибка", $"Не удалось загрузить продукты: {ex.Message}", "OK");
                        HideLoadingIndicator();
                    });
                }
            });
        }

        private void OnRemainingItemsThresholdReached(object sender, EventArgs e)
        {
            if (_groupName == "Вся продукция" && !_isLoading)
            {
                MainThread.BeginInvokeOnMainThread(() => {
                    LoadNextPage();
                });
            }
        }

        private async Task LoadNextPage()
        {
            if (_isLoading || _currentPage * PAGE_SIZE >= _allProducts.Count)
                return;

            _isLoading = true;

            try
            {
                int startIndex = _currentPage * PAGE_SIZE;
                int itemsToLoad = Math.Min(PAGE_SIZE, _allProducts.Count - startIndex);

                if (itemsToLoad <= 0)
                    return;

                var newItems = _allProducts.GetRange(startIndex, itemsToLoad);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    foreach (var item in newItems)
                    {
                        _displayedProducts.Add(item);
                    }
                });

                _currentPage++;
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = e.NewTextValue?.Trim() ?? string.Empty;
            _allProducts.Clear();
            _displayedProducts.Clear();
            _currentPage = 0;
            LoadProducts();
        }

        private void ShowLoadingIndicator()
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
        }

        private void HideLoadingIndicator()
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }

        private async void OnProductSelected(object sender, SelectionChangedEventArgs e)
        {
            var selectedProduct = e.CurrentSelection.FirstOrDefault() as ProductItem;
            if (selectedProduct != null)
            {
                string cleanProductName = Regex.Replace(selectedProduct.Name, @"<\/?color.*?>", string.Empty);
                _mainPage.SaveSelectedOrderDate();

                if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    var quantityPopup = new QuantityInputPopup();
                    quantityPopup.QuantityConfirmed += async (sender, quantity) =>
                    {
                        _mainPage.AddProductToOrder(cleanProductName, quantity);

                        if (Preferences.Get("AutoReturnEnabled", false))
                        {
                            await Navigation.PopAsync();
                        }
                    };
                    await Application.Current.MainPage.ShowPopupAsync(quantityPopup);
                }
                else if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    string result = await DisplayPromptAsync("Укажите количество", $"Укажите количество для {cleanProductName}", "OK", "Отмена", keyboard: Keyboard.Numeric);

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        result = result.Replace(",", ".");
                        if (decimal.TryParse(result, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal quantity) && quantity > 0)
                        {
                            _mainPage.AddProductToOrder(cleanProductName, quantity);
                            await DisplayAlert("Успех", $"{cleanProductName} успешно добавлен(а) в заказ.", "OK");

                            if (Preferences.Get("AutoReturnEnabled", false))
                            {
                                await Navigation.PopAsync();
                            }
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Укажите корректное количество.", "OK");
                        }
                    }
                }

                ProductCollectionView.SelectedItem = null;
            }
        }

        public class QuantityInputPopup : Popup
        {
            public Entry QuantityEntry { get; private set; }
            public Button ConfirmButton { get; private set; }

            public event EventHandler<decimal> QuantityConfirmed;

            public QuantityInputPopup()
            {
                QuantityEntry = new Entry
                {
                    Keyboard = Keyboard.Numeric,
                    Placeholder = "Укажите количество",
                    HorizontalOptions = LayoutOptions.FillAndExpand,
                    VerticalOptions = LayoutOptions.Start,
                    HeightRequest = 50,
                    FontSize = 18,
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2D2D30"),
                    TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#E1E1E1")
                };

                QuantityEntry.TextChanged += (s, e) =>
                {
                    if (e.NewTextValue != null)
                    {
                        QuantityEntry.Text = e.NewTextValue.Replace(',', '.');
                    }
                };

                QuantityEntry.Completed += (s, e) => OnConfirmButtonClicked(s, e);

                ConfirmButton = new Button
                {
                    Text = "OK",
                    HorizontalOptions = LayoutOptions.Center,
                    HeightRequest = 50,
                    FontSize = 18,
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#3C3F41"),
                    TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#E1E1E1")
                };
                ConfirmButton.Clicked += OnConfirmButtonClicked;

                var stackLayout = new StackLayout
                {
                    Padding = new Thickness(20),
                    Spacing = 20,
                    Children = { QuantityEntry, ConfirmButton }
                };

                var contentLayout = new Frame
                {
                    Padding = new Thickness(10),
                    CornerRadius = 10,
                    Content = stackLayout,
                    HeightRequest = 200,
                    WidthRequest = 300,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2D2D30")
                };

                Content = contentLayout;
                this.Opened += OnPopupOpened;
            }

            private async void OnPopupOpened(object sender, EventArgs e)
            {
                await Task.Delay(100);
                QuantityEntry.Focus();
            }

            private void OnConfirmButtonClicked(object sender, EventArgs e)
            {
                if (decimal.TryParse(QuantityEntry.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal quantity) && quantity > 0)
                {
                    QuantityConfirmed?.Invoke(this, quantity);
                    Close();
                }
                else
                {
                    Application.Current.MainPage.DisplayAlert("Ошибка", "Укажите корректное количество.", "OK");
                }
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        public void SelectProductsForOrder(Order order)
        {
            order.Products = ProductCache.CachedProducts.ContainsKey(_groupName)
                ? ProductCache.CachedProducts[_groupName]
                : new List<ProductItem>();
        }
    }
}
