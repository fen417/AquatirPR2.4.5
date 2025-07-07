using Microsoft.Maui.Storage;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aquatir
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _database;
        private Task _initializationTask;

        public DatabaseService()
        {
            _initializationTask = InitializeDatabaseAsync();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                string databasePath = Path.Combine(FileSystem.AppDataDirectory, "aquatir.db");

                _database = new SQLiteAsyncConnection(databasePath,
                    SQLiteOpenFlags.ReadWrite |
                    SQLiteOpenFlags.Create |
                    SQLiteOpenFlags.FullMutex);


                await _database.CreateTableAsync<ProductGroup>();
                await _database.CreateTableAsync<ProductItem>();
                var groupCount = await _database.Table<ProductGroup>().CountAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseService] КРИТИЧЕСКАЯ ОШИБКА инициализации: {ex.Message}");
                Console.WriteLine($"[DatabaseService] Стек трейс: {ex.StackTrace}");
                throw;
            }
        }
        public async Task EnsureInitializedAsync()
        {
            await _initializationTask;
        }

        public async Task SaveProductGroupsAsync(Dictionary<string, List<ProductItem>> productGroups)
        {
            Console.WriteLine($"[SaveProductGroupsAsync] Начало сохранения. Групп: {productGroups.Count}");

            try
            {
                await _database.DeleteAllAsync<ProductGroup>();
                Console.WriteLine("[SaveProductGroupsAsync] Все записи из таблицы ProductGroup удалены.");

                await _database.DeleteAllAsync<ProductItem>();
                Console.WriteLine("[SaveProductGroupsAsync] Все записи из таблицы ProductItem удалены.");

                foreach (var group in productGroups)
                {
                    var productGroup = new ProductGroup { Name = group.Key };
                    await _database.InsertAsync(productGroup);
                    Console.WriteLine($"[SaveProductGroupsAsync] Группа '{group.Key}' добавлена в таблицу ProductGroup.");

                    foreach (var product in group.Value)
                    {
                        product.GroupId = productGroup.Id;
                        await _database.InsertAsync(product);
                        Console.WriteLine($"[SaveProductGroupsAsync] Продукт '{product.Name}' добавлен в таблицу ProductItem.");
                    }
                }

                Console.WriteLine("[SaveProductGroupsAsync] Группы продукции успешно сохранены.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveProductGroupsAsync] Ошибка при сохранении групп продукции: {ex.Message}");
                throw;
            }
        }

       
        // Загрузка групп продукции
        public async Task<Dictionary<string, List<ProductItem>>> LoadProductGroupsAsync()
        {
            await EnsureInitializedAsync();
            var productGroups = new Dictionary<string, List<ProductItem>>();

            var groups = await _database.Table<ProductGroup>().ToListAsync();
            foreach (var group in groups)
            {
                var products = await _database.Table<ProductItem>()
                    .Where(p => p.GroupId == group.Id)
                    .ToListAsync();
                productGroups[group.Name] = products;
            }

            return productGroups;
        }
    }

    public class ProductGroup
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ProductItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Quantity { get; set; }
        public bool IsNew { get; set; }
        public bool IsRes { get; set; }
        public bool IsEnd { get; set; }
        [Ignore]
        public decimal PricePerKg { get; set; }
        [Ignore]
        public decimal PricePerUnit { get; set; }
        [Ignore]
        public decimal PricePerVedro { get; set; }
        [Ignore]
        public decimal PricePerPiece { get; set; }
        [Ignore]
        public decimal PricePerCont { get; set; }
        public int GroupId { get; set; }
        public string DisplayName => RemoveUnitFromName(Name);

        public string DisplayQuantity
        {
            get
            {
                string unit = GetUnitFromName(Name);
                return Quantity % 1 == 0 ? $"{Quantity:0} {unit}" : $"{Quantity:0.##} {unit}";
            }
        }

        // Метод для получения единицы измерения
        private string GetUnitFromName(string productName)
        {
            if (productName.EndsWith("УП.", StringComparison.OrdinalIgnoreCase))
            {
                return "уп.";
            }
            else if (productName.EndsWith("ШТ.", StringComparison.OrdinalIgnoreCase))
            {
                return "шт.";
            }
            else if (productName.EndsWith("ВЕС.", StringComparison.OrdinalIgnoreCase))
            {
                return "кг";
            }
            else if (productName.EndsWith("В.", StringComparison.OrdinalIgnoreCase))
            {
                return "в.";
            }
            else if (productName.EndsWith("КОНТ.", StringComparison.OrdinalIgnoreCase))
            {
                return "конт.";
            }
            return string.Empty;
        }

        // Метод для удаления суффиксов из названия продукта
        private string RemoveUnitFromName(string productName)
        {
            if (string.IsNullOrEmpty(productName)) return productName;

            string[] units = { "УП.", "ШТ.", "ВЕС.", "В.", "КОНТ." };
            foreach (var unit in units)
            {
                if (productName.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
                {
                    return productName.Substring(0, productName.Length - unit.Length).Trim();
                }
            }
            return productName;
        }
    }
}