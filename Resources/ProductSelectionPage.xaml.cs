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
                    // Windows implementation remains the same
                    var quantityPopup = new QuantityInputPopup(cleanProductName);
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
                    // For Android - new improved implementation
                    bool showProductImages = Preferences.Get("ShowProductImages", false);
                    string imageUrl = showProductImages ? await ProductImageCache.GetImageUrlForProduct(cleanProductName) : null;

                    // Create a custom popup for Android
                    var popupLayout = new VerticalStackLayout
                    {
                        Spacing = 20,
                        Padding = new Thickness(30),
                        BackgroundColor = Colors.White
                    };

                    // Add product name
                    var productNameLabel = new Label
                    {
                        Text = cleanProductName,
                        HorizontalOptions = LayoutOptions.Center,
                        FontSize = 18,
                        TextColor = Colors.Black,
                        FontAttributes = FontAttributes.Bold,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    popupLayout.Add(productNameLabel);

                    // Add image if available
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var image = new Image
                        {
                            HeightRequest = 200,
                            WidthRequest = 200,
                            Aspect = Aspect.AspectFit,
                            HorizontalOptions = LayoutOptions.Center,
                            BackgroundColor = Colors.Transparent
                        };

                        try
                        {
                            image.Source = ImageSource.FromUri(new Uri(imageUrl));
                            popupLayout.Add(image);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error setting image source: {ex.Message}");
                        }
                    }

                    // Quantity input
                    var entry = new Entry
                    {
                        Placeholder = "Укажите количество",
                        Keyboard = Keyboard.Numeric,
                        HorizontalOptions = LayoutOptions.Fill,
                        BackgroundColor = Colors.White,
                        TextColor = Colors.Black,
                        FontSize = 16,
                        HeightRequest = 50,
                        Margin = new Thickness(0, 10, 0, 10)
                    };

                    // Buttons layout
                    var buttonsLayout = new HorizontalStackLayout
                    {
                        Spacing = 20,
                        HorizontalOptions = LayoutOptions.Center
                    };

                    var confirmButton = new Button
                    {
                        Text = "OK",
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        BackgroundColor = Colors.Green, // MAUI primary color
                        TextColor = Colors.White,
                        FontSize = 16,
                        HeightRequest = 50,
                        WidthRequest = 120
                    };

                    var cancelButton = new Button
                    {
                        Text = "Отмена",
                        HorizontalOptions = LayoutOptions.FillAndExpand,
                        BackgroundColor = Colors.LightGray,
                        TextColor = Colors.Black,
                        FontSize = 16,
                        HeightRequest = 50,
                        WidthRequest = 120
                    };

                    buttonsLayout.Add(confirmButton);
                    buttonsLayout.Add(cancelButton);

                    popupLayout.Add(entry);
                    popupLayout.Add(buttonsLayout);

                    // Create the popup
                    var popup = new Popup
                    {
                        Content = new Frame
                        {
                            Content = popupLayout,
                            CornerRadius = 10,
                            BackgroundColor = Colors.White,
                            BorderColor = Colors.LightGray,
                            Padding = new Thickness(0),
                            HasShadow = true,
                            WidthRequest = DeviceInfo.Idiom == DeviceIdiom.Phone ? 300 : 400,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        },
                        Color = Color.FromArgb("#80000000") // Semi-transparent background
                    };

                    // Set up event handlers
                    var tcs = new TaskCompletionSource<bool>();

                    confirmButton.Clicked += async (s, args) =>
                    {
                        string inputText = entry.Text?.Replace(',', '.') ?? "";

                        if (decimal.TryParse(inputText, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out decimal quantity) && quantity > 0)
                        {
                            _mainPage.AddProductToOrder(cleanProductName, quantity);
                            popup.Close();
                            tcs.SetResult(true);

                            if (Preferences.Get("AutoReturnEnabled", false))
                            {
                                await Navigation.PopAsync();
                            }
                        }
                        else
                        {
                            await DisplayAlert("Ошибка", "Укажите корректное количество.", "OK");
                        }
                    };

                    cancelButton.Clicked += (s, args) =>
                    {
                        popup.Close();
                        tcs.SetResult(false);
                    };

                    // Show the popup
                    await Application.Current.MainPage.ShowPopupAsync(popup);
                    await tcs.Task;

                    ProductCollectionView.SelectedItem = null;
                }
            }
        }

        public class QuantityInputPopup : Popup
        {
            public Entry QuantityEntry { get; private set; }
            public Button ConfirmButton { get; private set; }
            public Image ProductImage { get; private set; }
            private string _productName;

            public event EventHandler<decimal> QuantityConfirmed;

            public QuantityInputPopup(string productName)
            {
                _productName = productName;

                // Product image (will be shown only if enabled in settings)
                ProductImage = new Image
                {
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Start,
                    HeightRequest = 150,
                    WidthRequest = 150,
                    Aspect = Aspect.AspectFit,
                    IsVisible = false // Will be set to true if image is loaded
                };

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
                    Children = { ProductImage, QuantityEntry, ConfirmButton }
                };

                var contentLayout = new Frame
                {
                    Padding = new Thickness(10),
                    CornerRadius = 10,
                    Content = stackLayout,
                    HeightRequest = 300, // Increased height to accommodate image
                    WidthRequest = 320, // Increased width slightly
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#2D2D30")
                };

                Content = contentLayout;
                this.Opened += OnPopupOpened;

                // Load product image if feature is enabled
                bool showProductImages = Preferences.Get("ShowProductImages", false);
                if (showProductImages)
                {
                    LoadProductImage();
                }
            }

            private async void LoadProductImage()
            {
                try
                {
                    string imageUrl = await ProductImageCache.GetImageUrlForProduct(_productName);
                    Console.WriteLine($"QuantityInputPopup - Retrieved image URL: {imageUrl}");

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        ProductImage.Source = ImageSource.FromUri(new Uri(imageUrl));
                    }
                    else
                    {
                        ProductImage.IsVisible = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading product image: {ex.Message}");
                    ProductImage.IsVisible = false;
                }
            }

            private async void OnPopupOpened(object sender, EventArgs e)
            {
                await Task.Delay(100);
                QuantityEntry.Focus();
            }

            private void OnConfirmButtonClicked(object sender, EventArgs e)
            {
                string inputText = QuantityEntry.Text?.Replace(',', '.') ?? "";

                if (decimal.TryParse(inputText, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal quantity) && quantity > 0)
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