using Microsoft.Maui.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;

namespace Aquatir.Model;

// Model for product items
public class ProductItem
{
    public string Name { get; set; }
    public double Quantity { get; set; }
    public bool IsEnd { get; set; }
    public bool IsNew { get; set; }
    public bool IsRes { get; set; }
}

// Model for product group
public class ProductGroup
{
    public string Name { get; set; }
    public ObservableCollection<ProductItem> Products { get; set; }
    public bool IsExpanded { get; set; }
    public bool IsLoading { get; set; }
    public bool IsLoaded { get; set; }

    public ProductGroup(string name)
    {
        Name = name;
        Products = new ObservableCollection<ProductItem>();
        IsExpanded = false;
        IsLoading = false;
        IsLoaded = false;
    }
}

public partial class ProductEditorPage : ContentPage
{
    private HttpClient client = new HttpClient();
    private ObservableCollection<ProductGroup> productGroups;
    private JObject originalProductData;
    private string downloadLink;

    public ProductEditorPage()
    {
        InitializeComponent();
        LoadProductsAsync();
    }

    private async void LoadProductsAsync()
    {
        try
        {
            // Show loading indicator
            LoadingIndicator.IsVisible = true;

            // Get the download link
            downloadLink = await GetFileDownloadLinkFromYandex();
            if (string.IsNullOrEmpty(downloadLink))
            {
                await DisplayAlert("Ошибка", "Не удалось получить ссылку для скачивания файла", "OK");
                return;
            }

            // Download the file
            var response = await client.GetAsync(downloadLink);
            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Ошибка", $"Не удалось скачать файл: {response.StatusCode}", "OK");
                return;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            originalProductData = JObject.Parse(jsonContent);

            // Create product groups with empty product collections
            productGroups = new ObservableCollection<ProductGroup>();
            foreach (var groupPair in originalProductData)
            {
                var groupName = groupPair.Key;
                var group = new ProductGroup(groupName);
                productGroups.Add(group);
            }

            // Bind the product groups to the CollectionView
            GroupsCollectionView.ItemsSource = productGroups;

            // Hide loading indicator after groups are loaded
            LoadingIndicator.IsVisible = false;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Произошла ошибка при загрузке данных: {ex.Message}", "OK");
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnGroupTapped(object sender, EventArgs e)
    {
        var group = (sender as BindableObject)?.BindingContext as ProductGroup;
        if (group == null) return;

        // Toggle expanded state
        group.IsExpanded = !group.IsExpanded;

        // If not loaded and expanded, load products
        if (group.IsExpanded && !group.IsLoaded && !group.IsLoading)
        {
            await LoadProductsForGroupAsync(group);
        }
    }

    private async Task LoadProductsForGroupAsync(ProductGroup group)
    {
        try
        {
            group.IsLoading = true;
            // Force UI update
            var index = productGroups.IndexOf(group);
            productGroups.RemoveAt(index);
            productGroups.Insert(index, group);

            // Get products from original data
            if (originalProductData.TryGetValue(group.Name, out JToken productsToken))
            {
                group.Products.Clear();
                foreach (var productToken in productsToken)
                {
                    var product = new ProductItem
                    {
                        Name = productToken["Name"]?.ToString(),
                        Quantity = productToken["Quantity"]?.ToObject<double>() ?? 0.0,
                        IsEnd = productToken["IsEnd"]?.ToObject<bool>() ?? false,
                        IsNew = productToken["IsNew"]?.ToObject<bool>() ?? false,
                        IsRes = productToken["IsRes"]?.ToObject<bool>() ?? false
                    };
                    group.Products.Add(product);
                }
            }

            group.IsLoaded = true;
            group.IsLoading = false;

            // Force UI update
            index = productGroups.IndexOf(group);
            productGroups.RemoveAt(index);
            productGroups.Insert(index, group);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка при загрузке продуктов: {ex.Message}", "OK");
            group.IsLoading = false;
            group.IsExpanded = false;

            // Force UI update
            var index = productGroups.IndexOf(group);
            productGroups.RemoveAt(index);
            productGroups.Insert(index, group);
        }
    }

    private async void OnAddProductClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var group = button.BindingContext as ProductGroup;

        if (group == null) return;

        // Make sure products are loaded
        if (!group.IsLoaded)
        {
            await LoadProductsForGroupAsync(group);
        }

        // Create a custom dialog for adding a new product
        var page = new ContentPage
        {
            Title = "Добавить продукт"
        };

        var nameEntry = new Entry
        {
            Placeholder = "Название продукта",
            Margin = new Thickness(20, 10)
        };

        var endCheckBox = new CheckBox
        {
            IsChecked = false,
            Color = Colors.Orange
        };

        var endLabel = new Label
        {
            Text = "Заканчивается",
            TextColor = Colors.Orange,
            VerticalOptions = LayoutOptions.Center
        };

        var endStack = new HorizontalStackLayout
        {
            Children = { endCheckBox, endLabel },
            Spacing = 5,
            Margin = new Thickness(20, 5)
        };

        var newCheckBox = new CheckBox
        {
            IsChecked = false,
            Color = Colors.Red
        };

        var newLabel = new Label
        {
            Text = "Новинка",
            TextColor = Colors.Red,
            VerticalOptions = LayoutOptions.Center
        };

        var newStack = new HorizontalStackLayout
        {
            Children = { newCheckBox, newLabel },
            Spacing = 5,
            Margin = new Thickness(20, 5)
        };

        var resCheckBox = new CheckBox
        {
            IsChecked = false,
            Color = Colors.Green
        };

        var resLabel = new Label
        {
            Text = "Снова в продаже",
            TextColor = Colors.Green,
            VerticalOptions = LayoutOptions.Center
        };

        var resStack = new HorizontalStackLayout
        {
            Children = { resCheckBox, resLabel },
            Spacing = 5,
            Margin = new Thickness(20, 5)
        };

        // Add hint label for required suffixes
        var hintLabel = new Label
        {
            Text = "Название должно заканчиваться на: ШТ., В., КОНТ., ВЕС. или УП.",
            TextColor = Colors.Gray,
            FontSize = 12,
            Margin = new Thickness(20, 0, 20, 10)
        };

        var cancelButton = new Button
        {
            Text = "Отмена",
            BackgroundColor = Colors.Gray,
            TextColor = Colors.White,
            Margin = new Thickness(20, 10)
        };

        var addButton = new Button
        {
            Text = "Добавить",
            BackgroundColor = Color.FromRgb(0, 120, 215), // #0078D7
            TextColor = Colors.White,
            Margin = new Thickness(20, 0, 20, 20)
        };

        var layout = new VerticalStackLayout
        {
            Children = { nameEntry, hintLabel, endStack, newStack, resStack, cancelButton, addButton }
        };

        page.Content = layout;

        // Set up the actions
        cancelButton.Clicked += (s, args) =>
        {
            Navigation.PopModalAsync();
        };

        addButton.Clicked += async (s, args) =>
        {
            if (string.IsNullOrWhiteSpace(nameEntry.Text))
            {
                await DisplayAlert("Ошибка", "Название продукта не может быть пустым", "OK");
                return;
            }

            // Check if the name ends with one of the required suffixes
            string[] requiredSuffixes = { "ШТ.", "В.", "КОНТ.", "ВЕС.", "УП." };
            bool isValidSuffix = false;

            foreach (var suffix in requiredSuffixes)
            {
                if (nameEntry.Text.TrimEnd().EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    isValidSuffix = true;
                    break;
                }
            }

            if (!isValidSuffix)
            {
                await DisplayAlert("Ошибка", "Название продукта должно заканчиваться на один из вариантов: ШТ., В., КОНТ., ВЕС. или УП.", "OK");
                return;
            }

            // Create a new product
            var newProduct = new ProductItem
            {
                Name = nameEntry.Text,
                Quantity = 0.0,
                IsEnd = endCheckBox.IsChecked,
                IsNew = newCheckBox.IsChecked,
                IsRes = resCheckBox.IsChecked
            };

            group.Products.Add(newProduct);

            // Ensure group is expanded to show the new product
            if (!group.IsExpanded)
            {
                group.IsExpanded = true;
                // Force UI update
                var index = productGroups.IndexOf(group);
                productGroups.RemoveAt(index);
                productGroups.Insert(index, group);
            }

            await Navigation.PopModalAsync();
        };

        await Navigation.PushModalAsync(page);
    }

    private async void OnEditProductClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var product = button.BindingContext as ProductItem;
        var group = GetProductGroup(product);

        if (product == null || group == null) return;

        // Create a custom dialog for editing
        var page = new ContentPage
        {
            Title = "Редактировать продукт"
        };

        var nameEntry = new Entry
        {
            Text = product.Name,
            Placeholder = "Название продукта",
            Margin = new Thickness(20, 10)
        };

        // Add hint label for required suffixes
        var hintLabel = new Label
        {
            Text = "Название должно заканчиваться на: ШТ., В., КОНТ., ВЕС. или УП.",
            TextColor = Colors.Gray,
            FontSize = 12,
            Margin = new Thickness(20, 0, 20, 10)
        };

        var endCheckBox = new CheckBox
        {
            IsChecked = product.IsEnd,
            Color = Colors.Orange
        };

        var endLabel = new Label
        {
            Text = "Заканчивается",
            TextColor = Colors.Orange,
            VerticalOptions = LayoutOptions.Center
        };

        var endStack = new HorizontalStackLayout
        {
            Children = { endCheckBox, endLabel },
            Spacing = 5,
            Margin = new Thickness(20, 5)
        };

        var newCheckBox = new CheckBox
        {
            IsChecked = product.IsNew,
            Color = Colors.Red
        };

        var newLabel = new Label
        {
            Text = "Новинка",
            TextColor = Colors.Red,
            VerticalOptions = LayoutOptions.Center
        };

        var newStack = new HorizontalStackLayout
        {
            Children = { newCheckBox, newLabel },
            Spacing = 5,
            Margin = new Thickness(20, 5)
        };

        var resCheckBox = new CheckBox
        {
            IsChecked = product.IsRes,
            Color = Colors.Green
        };

        var resLabel = new Label
        {
            Text = "Снова в продаже",
            TextColor = Colors.Green,
            VerticalOptions = LayoutOptions.Center
        };

        var resStack = new HorizontalStackLayout
        {
            Children = { resCheckBox, resLabel },
            Spacing = 5,
            Margin = new Thickness(20, 5)
        };

        var cancelButton = new Button
        {
            Text = "Отмена",
            BackgroundColor = Colors.Gray,
            TextColor = Colors.White,
            Margin = new Thickness(20, 10)
        };

        var saveButton = new Button
        {
            Text = "Сохранить",
            BackgroundColor = Color.FromRgb(0, 120, 215), // #0078D7
            TextColor = Colors.White,
            Margin = new Thickness(20, 0, 20, 20)
        };

        var layout = new VerticalStackLayout
        {
            Children = { nameEntry, hintLabel, endStack, newStack, resStack, cancelButton, saveButton }
        };

        page.Content = layout;

        // Set up the actions
        cancelButton.Clicked += (s, args) =>
        {
            Navigation.PopModalAsync();
        };

        saveButton.Clicked += async (s, args) =>
        {
            if (string.IsNullOrWhiteSpace(nameEntry.Text))
            {
                await DisplayAlert("Ошибка", "Название продукта не может быть пустым", "OK");
                return;
            }

            // Check if the name ends with one of the required suffixes
            string[] requiredSuffixes = { "ШТ.", "В.", "КОНТ.", "ВЕС.", "УП." };
            bool isValidSuffix = false;

            foreach (var suffix in requiredSuffixes)
            {
                if (nameEntry.Text.TrimEnd().EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    isValidSuffix = true;
                    break;
                }
            }

            if (!isValidSuffix)
            {
                await DisplayAlert("Ошибка", "Название продукта должно заканчиваться на один из вариантов: ШТ., В., КОНТ., ВЕС. или УП.", "OK");
                return;
            }

            // Update the product
            product.Name = nameEntry.Text;
            product.IsEnd = endCheckBox.IsChecked;
            product.IsNew = newCheckBox.IsChecked;
            product.IsRes = resCheckBox.IsChecked;

            // Force UI update
            var index = group.Products.IndexOf(product);
            group.Products.RemoveAt(index);
            group.Products.Insert(index, product);

            await Navigation.PopModalAsync();
        };

        await Navigation.PushModalAsync(page);
    }

    private async void OnDeleteProductClicked(object sender, EventArgs e)
    {
        var button = sender as Button;
        var product = button.BindingContext as ProductItem;
        var group = GetProductGroup(product);

        if (product == null || group == null) return;

        var confirm = await DisplayAlert("Подтверждение",
            $"Вы действительно хотите удалить продукт '{product.Name}'?",
            "Удалить",
            "Отмена");

        if (confirm)
        {
            group.Products.Remove(product);
        }
    }

    private ProductGroup GetProductGroup(ProductItem product)
    {
        foreach (var group in productGroups)
        {
            if (group.Products.Contains(product))
            {
                return group;
            }
        }
        return null;
    }

    // Обработчики для чекбоксов
    private void OnEndCheckboxChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is ProductItem product)
        {
            product.IsEnd = e.Value;
        }
    }

    private void OnNewCheckboxChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is ProductItem product)
        {
            product.IsNew = e.Value;
        }
    }

    private void OnResCheckboxChanged(object sender, CheckedChangedEventArgs e)
    {
        if (sender is CheckBox checkBox && checkBox.BindingContext is ProductItem product)
        {
            product.IsRes = e.Value;
        }
    }

    private async void OnSaveChangesClicked(object sender, EventArgs e)
    {
        try
        {
            var updatedData = new JObject();

            foreach (var group in productGroups)
            {
                var productsArray = new JArray();

                // If the group wasn't loaded, use original data
                if (!group.IsLoaded)
                {
                    updatedData[group.Name] = originalProductData[group.Name];
                    continue;
                }

                foreach (var product in group.Products)
                {
                    var productObj = new JObject
                    {
                        ["Name"] = product.Name,
                        ["Quantity"] = product.Quantity
                    };

                    if (product.IsEnd)
                    {
                        productObj["IsEnd"] = true;
                    }

                    if (product.IsNew)
                    {
                        productObj["IsNew"] = true;
                    }

                    if (product.IsRes)
                    {
                        productObj["IsRes"] = true;
                    }

                    productsArray.Add(productObj);
                }
                updatedData[group.Name] = productsArray;
            }

            var confirm = await DisplayAlert("Подтверждение",
                "Вы уверены, что хотите сохранить изменения?",
                "Сохранить",
                "Отмена");

            if (!confirm) return;

            // Save to Yandex.Disk
            await SaveToYandexDisk(updatedData.ToString());
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Произошла ошибка при сохранении данных: {ex.Message}", "OK");
        }
    }

    private async Task<string> GetFileDownloadLinkFromYandex()
    {
        try
        {
            string yandexFilePath = "/productsCOLOR.json";  // Путь к файлу на Яндекс.Диске
            string token = "y0_AgAAAAB4zZe6AAzBewAAAAEYCy0wAAABOYgsBbpL6pQDgfd6pphTlGUu3Q";  // OAuth токен
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");
            var response = await client.GetAsync($"https://cloud-api.yandex.net/v1/disk/resources/download?path={Uri.EscapeDataString(yandexFilePath)}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonConvert.DeserializeObject<dynamic>(content);
                return json.href;
            }
            else
            {
                Console.WriteLine("Ошибка получения ссылки для скачивания: " + response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении ссылки для скачивания: {ex.Message}");
            return null;
        }
    }

    private async Task SaveToYandexDisk(string jsonContent)
    {
        try
        {
            // Get upload URL
            string yandexFilePath = "/productsCOLOR.json";
            string token = "y0_AgAAAAB4zZe6AAzBewAAAAEYCy0wAAABOYgsBbpL6pQDgfd6pphTlGUu3Q";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Authorization", $"OAuth {token}");

            var response = await client.GetAsync($"https://cloud-api.yandex.net/v1/disk/resources/upload?path={Uri.EscapeDataString(yandexFilePath)}&overwrite=true");
            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Ошибка", $"Не удалось получить ссылку для загрузки: {response.StatusCode}", "OK");
                return;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonConvert.DeserializeObject<dynamic>(content);
            string uploadUrl = json.href;

            // Upload file
            var fileContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var uploadResponse = await client.PutAsync(uploadUrl, fileContent);

            if (uploadResponse.IsSuccessStatusCode)
            {
                await DisplayAlert("Успех", "Файл успешно сохранен на Яндекс.Диск", "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", $"Не удалось загрузить файл: {uploadResponse.StatusCode}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Произошла ошибка при сохранении файла: {ex.Message}", "OK");
        }
    }
}