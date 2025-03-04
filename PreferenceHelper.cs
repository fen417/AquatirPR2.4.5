using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using System.Collections.Generic;
namespace Aquatir;
public class PreferenceHelper
{
    // Сохраняем ключ и обновляем список всех ключей
    public static void SaveKey(string key, string value)
    {
        Preferences.Set(key, value);

        // Получаем текущий список всех ключей
        var allKeys = Preferences.Get("AllKeys", "[]");
        var keysList = JsonConvert.DeserializeObject<List<string>>(allKeys);

        // Добавляем новый ключ, если его ещё нет
        if (!keysList.Contains(key))
        {
            keysList.Add(key);
            Preferences.Set("AllKeys", JsonConvert.SerializeObject(keysList));
        }
    }

    // Получаем все ключи
    public static List<string> GetAllKeys()
    {
        var allKeys = Preferences.Get("AllKeys", "[]");
        return JsonConvert.DeserializeObject<List<string>>(allKeys);
    }

    // Удаляем ключ и обновляем список всех ключей
    public static void RemoveKey(string key)
    {
        Preferences.Remove(key);

        var allKeys = Preferences.Get("AllKeys", "[]");
        var keysList = JsonConvert.DeserializeObject<List<string>>(allKeys);

        // Удаляем ключ из списка
        keysList.Remove(key);
        Preferences.Set("AllKeys", JsonConvert.SerializeObject(keysList));
    }
}