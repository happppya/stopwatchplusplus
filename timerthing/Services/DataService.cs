using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using timerthing.ItemModels;
using System.IO;

namespace timerthing.Services
{
    class DataService
    {
        public static void SaveItems(ObservableCollection<ItemInfo> items, ObservableCollection<ItemGroup> groups)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var itemJSON = JsonSerializer.Serialize(items, options);
            var groupJSON = JsonSerializer.Serialize(groups, options);

            File.WriteAllText("items.json", itemJSON);
            File.WriteAllText("groups.json", groupJSON);
        }

        public static void LoadItems(ObservableCollection<ItemInfo> items, ObservableCollection<ItemGroup> groups)
        {
            const string itemPath = "items.json";
            const string groupPath = "groups.json";

            if ((!File.Exists(itemPath)) || (!File.Exists(groupPath)))
                return;

            var itemJSON = File.ReadAllText(itemPath);
            var groupJSON = File.ReadAllText(groupPath);

            var loadedGroups = JsonSerializer.Deserialize<ObservableCollection<ItemGroup>>(groupJSON);
            var loadedItems = JsonSerializer.Deserialize<ObservableCollection<ItemInfo>>(itemJSON);

            if (loadedItems != null)
            {
                items.Clear();
                foreach (ItemInfo item in loadedItems)
                    items.Add(item);
            }

            if (loadedGroups != null)
            {
                groups.Clear();
                foreach (ItemGroup group in loadedGroups)
                    groups.Add(group);
            }
        }
    }
}
