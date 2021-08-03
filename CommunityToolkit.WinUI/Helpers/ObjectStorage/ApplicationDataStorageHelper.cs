// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Common.Helpers;
using Windows.Storage;
using Windows.System;

#nullable enable

namespace CommunityToolkit.WinUI.Helpers
{
    /// <summary>
    /// Storage helper for files and folders living in Windows.Storage.ApplicationData storage endpoints.
    /// </summary>
    public partial class ApplicationDataStorageHelper : IFileStorageHelper, ISettingsStorageHelper<string>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDataStorageHelper"/> class.
        /// </summary>
        /// <param name="appData">The data store to interact with.</param>
        /// <param name="objectSerializer">Serializer for converting stored values. Defaults to <see cref="Common.Helpers.SystemSerializer"/>.</param>
        public ApplicationDataStorageHelper(ApplicationData appData, Common.Helpers.IObjectSerializer? objectSerializer = null)
        {
            this.AppData = appData ?? throw new ArgumentNullException(nameof(appData));
            this.Serializer = objectSerializer ?? new Common.Helpers.SystemSerializer();
        }

        /// <summary>
        /// Gets the settings container.
        /// </summary>
        public ApplicationDataContainer Settings => this.AppData.LocalSettings;

        /// <summary>
        ///  Gets the storage folder.
        /// </summary>
        public StorageFolder Folder => this.AppData.LocalFolder;

        /// <summary>
        /// Gets the storage host.
        /// </summary>
        protected ApplicationData AppData { get; }

        /// <summary>
        /// Gets the serializer for converting stored values.
        /// </summary>
        protected Common.Helpers.IObjectSerializer Serializer { get; }

        /// <summary>
        /// Get a new instance using ApplicationData.Current and the provided serializer.
        /// </summary>
        /// <param name="objectSerializer">Serializer for converting stored values. Defaults to <see cref="Common.Helpers.SystemSerializer"/>.</param>
        /// <returns>A new instance of ApplicationDataStorageHelper.</returns>
        public static ApplicationDataStorageHelper GetCurrent(Common.Helpers.IObjectSerializer? objectSerializer = null)
        {
            var appData = ApplicationData.Current;
            return new ApplicationDataStorageHelper(appData, objectSerializer);
        }

        /// <summary>
        /// Get a new instance using the ApplicationData for the provided user and serializer.
        /// </summary>
        /// <param name="user">App data user owner.</param>
        /// <param name="objectSerializer">Serializer for converting stored values. Defaults to <see cref="Common.Helpers.SystemSerializer"/>.</param>
        /// <returns>A new instance of ApplicationDataStorageHelper.</returns>
        public static async Task<ApplicationDataStorageHelper> GetForUserAsync(User user, Common.Helpers.IObjectSerializer? objectSerializer = null)
        {
            var appData = await ApplicationData.GetForUserAsync(user);
            return new ApplicationDataStorageHelper(appData, objectSerializer);
        }

        /// <summary>
        /// Determines whether a setting already exists.
        /// </summary>
        /// <param name="key">Key of the setting (that contains object).</param>
        /// <returns>True if a value exists.</returns>
        public bool KeyExists(string key)
        {
            return this.Settings.Values.ContainsKey(key);
        }

        /// <summary>
        /// Retrieves a single item by its key.
        /// </summary>
        /// <typeparam name="T">Type of object retrieved.</typeparam>
        /// <param name="key">Key of the object.</param>
        /// <param name="default">Default value of the object.</param>
        /// <returns>The TValue object.</returns>
        public T? Read<T>(string key, T? @default = default)
        {
            if (this.Settings.Values.TryGetValue(key, out var valueObj) && valueObj is string valueString)
            {
                return this.Serializer.Deserialize<T>(valueString);
            }

            return @default;
        }

        /// <inheritdoc />
        public bool TryRead<T>(string key, out T? value)
        {
            if (this.Settings.Values.TryGetValue(key, out var valueObj) && valueObj is string valueString)
            {
                value = this.Serializer.Deserialize<T>(valueString);
                return true;
            }

            value = default;
            return false;
        }

        /// <inheritdoc />
        public void Save<T>(string key, T value)
        {
            this.Settings.Values[key] = this.Serializer.Serialize(value);
        }

        /// <inheritdoc />
        public bool TryDelete(string key)
        {
            return this.Settings.Values.Remove(key);
        }

        /// <inheritdoc />
        public void Clear()
        {
            this.Settings.Values.Clear();
        }

        /// <summary>
        /// Determines whether a setting already exists in composite.
        /// </summary>
        /// <param name="compositeKey">Key of the composite (that contains settings).</param>
        /// <param name="key">Key of the setting (that contains object).</param>
        /// <returns>True if a value exists.</returns>
        public bool KeyExists(string compositeKey, string key)
        {
            if (this.TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
            {
                return composite.ContainsKey(key);
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve a single item by its key in composite.
        /// </summary>
        /// <typeparam name="T">Type of object retrieved.</typeparam>
        /// <param name="compositeKey">Key of the composite (that contains settings).</param>
        /// <param name="key">Key of the object.</param>
        /// <param name="value">The value of the object retrieved.</param>
        /// <returns>The T object.</returns>
        public bool TryRead<T>(string compositeKey, string key, out T? value)
        {
            if (this.TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
            {
                string compositeValue = (string)composite[key];
                if (compositeValue != null)
                {
                    value = this.Serializer.Deserialize<T>(compositeValue);
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Retrieves a single item by its key in composite.
        /// </summary>
        /// <typeparam name="T">Type of object retrieved.</typeparam>
        /// <param name="compositeKey">Key of the composite (that contains settings).</param>
        /// <param name="key">Key of the object.</param>
        /// <param name="default">Default value of the object.</param>
        /// <returns>The T object.</returns>
        public T? Read<T>(string compositeKey, string key, T? @default = default)
        {
            if (this.TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
            {
                if (composite.TryGetValue(key, out object valueObj) && valueObj is string value)
                {
                    return this.Serializer.Deserialize<T>(value);
                }
            }

            return @default;
        }

        /// <summary>
        /// Saves a group of items by its key in a composite.
        /// This method should be considered for objects that do not exceed 8k bytes during the lifetime of the application
        /// and for groups of settings which need to be treated in an atomic way.
        /// </summary>
        /// <typeparam name="T">Type of object saved.</typeparam>
        /// <param name="compositeKey">Key of the composite (that contains settings).</param>
        /// <param name="values">Objects to save.</param>
        public void Save<T>(string compositeKey, IDictionary<string, T> values)
        {
            if (this.TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
            {
                foreach (KeyValuePair<string, T> setting in values)
                {
                    if (composite.ContainsKey(setting.Key))
                    {
                        composite[setting.Key] = this.Serializer.Serialize(setting.Value);
                    }
                    else
                    {
                        composite.Add(setting.Key, this.Serializer.Serialize(setting.Value));
                    }
                }
            }
            else
            {
                composite = new ApplicationDataCompositeValue();
                foreach (KeyValuePair<string, T> setting in values)
                {
                    composite.Add(setting.Key, this.Serializer.Serialize(setting.Value));
                }

                this.Settings.Values[compositeKey] = composite;
            }
        }

        /// <summary>
        /// Deletes a single item by its key in composite.
        /// </summary>
        /// <param name="compositeKey">Key of the composite (that contains settings).</param>
        /// <param name="key">Key of the object.</param>
        /// <returns>A boolean indicator of success.</returns>
        public bool TryDelete(string compositeKey, string key)
        {
            if (this.TryRead(compositeKey, out ApplicationDataCompositeValue? composite) && composite != null)
            {
                return composite.Remove(key);
            }

            return false;
        }

        /// <inheritdoc />
        public Task<T?> ReadFileAsync<T>(string filePath, T? @default = default)
        {
            return this.ReadFileAsync<T>(this.Folder, filePath, @default);
        }

        /// <inheritdoc />
        public Task<IEnumerable<(DirectoryItemType ItemType, string Name)>> ReadFolderAsync(string folderPath)
        {
            return this.ReadFolderAsync(this.Folder, folderPath);
        }

        /// <inheritdoc />
        public Task CreateFileAsync<T>(string filePath, T value)
        {
            return this.SaveFileAsync<T>(this.Folder, filePath, value);
        }

        /// <inheritdoc />
        public Task CreateFolderAsync(string folderPath)
        {
            return this.CreateFolderAsync(this.Folder, folderPath);
        }

        /// <inheritdoc />
        public Task DeleteItemAsync(string itemPath)
        {
            return this.DeleteItemAsync(this.Folder, itemPath);
        }

        /// <summary>
        /// Saves an object inside a file.
        /// </summary>
        /// <typeparam name="T">Type of object saved.</typeparam>
        /// <param name="filePath">Path to the file that will contain the object.</param>
        /// <param name="value">Object to save.</param>
        /// <returns>Waiting task until completion.</returns>
        public Task<StorageFile> SaveFileAsync<T>(string filePath, T value)
        {
            return this.SaveFileAsync(this.Folder, filePath, value);
        }

        private async Task<T?> ReadFileAsync<T>(StorageFolder folder, string filePath, T? @default = default)
        {
            string value = await StorageFileHelper.ReadTextFromFileAsync(folder, filePath);
            return (value != null) ? this.Serializer.Deserialize<T>(value) : @default;
        }

        private async Task<IEnumerable<(DirectoryItemType, string)>> ReadFolderAsync(StorageFolder folder, string folderPath)
        {
            var targetFolder = await folder.GetFolderAsync(folderPath);
            var items = await targetFolder.GetItemsAsync();

            return items.Select((item) =>
            {
                var itemType = item.IsOfType(StorageItemTypes.File) ? DirectoryItemType.File
                    : item.IsOfType(StorageItemTypes.Folder) ? DirectoryItemType.Folder
                    : DirectoryItemType.None;

                return (itemType, item.Name);
            });
        }

        private Task<StorageFile> SaveFileAsync<T>(StorageFolder folder, string filePath, T value)
        {
            return StorageFileHelper.WriteTextToFileAsync(folder, this.Serializer.Serialize(value)?.ToString(), filePath, CreationCollisionOption.ReplaceExisting);
        }

        private async Task CreateFolderAsync(StorageFolder folder, string folderPath)
        {
            await folder.CreateFolderAsync(folderPath, CreationCollisionOption.OpenIfExists);
        }

        private async Task DeleteItemAsync(StorageFolder folder, string itemPath)
        {
            var item = await folder.GetItemAsync(itemPath);
            await item.DeleteAsync();
        }
    }
}
