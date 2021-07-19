// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Toolkit.Helpers;
using Windows.Storage;
using Windows.System;

namespace Microsoft.Toolkit.Uwp.Helpers
{
    /// <summary>
    /// Storage helper for files and folders living in Windows.Storage.ApplicationData storage endpoints.
    /// </summary>
    public partial class ApplicationDataStorageHelper : IFileStorageHelper, ISettingsStorageHelper
    {
        /// <summary>
        /// Get a new instance using ApplicationData.Current and the provided serializer.
        /// </summary>
        /// <param name="objectSerializer">Serializer for converting stored values. Defaults to <see cref="Toolkit.Helpers.SystemSerializer"/>.</param>
        /// <returns>A new instance of ApplicationDataStorageHelper.</returns>
        public static ApplicationDataStorageHelper GetCurrent(Toolkit.Helpers.IObjectSerializer objectSerializer = null)
        {
            var appData = ApplicationData.Current;
            return new ApplicationDataStorageHelper(appData, objectSerializer);
        }

        /// <summary>
        /// Get a new instance using the ApplicationData for the provided user and serializer.
        /// </summary>
        /// <param name="user">App data user owner.</param>
        /// <param name="objectSerializer">Serializer for converting stored values. Defaults to <see cref="Toolkit.Helpers.SystemSerializer"/>.</param>
        /// <returns>A new instance of ApplicationDataStorageHelper.</returns>
        public static async Task<ApplicationDataStorageHelper> GetForUserAsync(User user, Toolkit.Helpers.IObjectSerializer objectSerializer = null)
        {
            var appData = await ApplicationData.GetForUserAsync(user);
            return new ApplicationDataStorageHelper(appData, objectSerializer);
        }

        /// <summary>
        /// Gets the settings container.
        /// </summary>
        public ApplicationDataContainer Settings => AppData.LocalSettings;

        /// <summary>
        ///  Gets the storage folder.
        /// </summary>
        public StorageFolder Folder => AppData.LocalFolder;

        /// <summary>
        /// Gets the storage host.
        /// </summary>
        protected ApplicationData AppData { get; private set; }

        private readonly Toolkit.Helpers.IObjectSerializer _serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationDataStorageHelper"/> class.
        /// </summary>
        /// <param name="appData">The data store to interact with.</param>
        /// <param name="objectSerializer">Serializer for converting stored values. Defaults to <see cref="Toolkit.Helpers.SystemSerializer"/>.</param>
        public ApplicationDataStorageHelper(ApplicationData appData, Toolkit.Helpers.IObjectSerializer objectSerializer = null)
        {
            AppData = appData ?? throw new ArgumentNullException(nameof(appData));
            _serializer = objectSerializer ?? new Toolkit.Helpers.SystemSerializer();
        }

        /// <inheritdoc />
        public bool KeyExists(string key)
        {
            return Settings.Values.ContainsKey(key);
        }

        /// <inheritdoc />
        public bool KeyExists(string compositeKey, string key)
        {
            if (KeyExists(compositeKey))
            {
                ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Settings.Values[compositeKey];
                if (composite != null)
                {
                    return composite.ContainsKey(key);
                }
            }

            return false;
        }

        /// <inheritdoc />
        public T Read<T>(string key, T @default = default)
        {
            if (!Settings.Values.TryGetValue(key, out var value) || value == null)
            {
                return @default;
            }

            return _serializer.Deserialize<T>(value);
        }

        /// <inheritdoc />
        public T Read<T>(string compositeKey, string key, T @default = default)
        {
            ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Settings.Values[compositeKey];
            if (composite != null)
            {
                string value = (string)composite[key];
                if (value != null)
                {
                    return _serializer.Deserialize<T>(value);
                }
            }

            return @default;
        }

        /// <inheritdoc />
        public void Save<T>(string key, T value)
        {
            Settings.Values[key] = _serializer.Serialize(value);
        }

        /// <inheritdoc />
        public void Save<T>(string compositeKey, IDictionary<string, T> values)
        {
            if (KeyExists(compositeKey))
            {
                ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Settings.Values[compositeKey];

                foreach (KeyValuePair<string, T> setting in values)
                {
                    if (composite.ContainsKey(setting.Key))
                    {
                        composite[setting.Key] = _serializer.Serialize(setting.Value);
                    }
                    else
                    {
                        composite.Add(setting.Key, _serializer.Serialize(setting.Value));
                    }
                }
            }
            else
            {
                ApplicationDataCompositeValue composite = new ApplicationDataCompositeValue();
                foreach (KeyValuePair<string, T> setting in values)
                {
                    composite.Add(setting.Key, _serializer.Serialize(setting.Value));
                }

                Settings.Values[compositeKey] = composite;
            }
        }

        /// <inheritdoc />
        public void Delete(string key)
        {
            Settings.Values.Remove(key);
        }

        /// <inheritdoc />
        public void Delete(string compositeKey, string key)
        {
            if (KeyExists(compositeKey))
            {
                ApplicationDataCompositeValue composite = (ApplicationDataCompositeValue)Settings.Values[compositeKey];
                composite.Remove(key);
            }
        }

        /// <inheritdoc />
        public Task<bool> ItemExistsAsync(string itemName)
        {
            return ItemExistsAsync(Folder, itemName);
        }

        /// <inheritdoc />
        public Task<T> ReadFileAsync<T>(string filePath, T @default = default)
        {
            return ReadFileAsync<T>(Folder, filePath, @default);
        }

        /// <inheritdoc />
        public Task<IList<Tuple<DirectoryItemType, string>>> ReadFolderAsync(string folderPath)
        {
            return ReadFolderAsync(Folder, folderPath);
        }

        /// <inheritdoc />
        public Task CreateFileAsync<T>(string filePath, T value)
        {
            return SaveFileAsync<T>(Folder, filePath, value);
        }

        /// <inheritdoc />
        public Task CreateFolderAsync(string folderPath)
        {
            return CreateFolderAsync(Folder, folderPath);
        }

        /// <inheritdoc />
        public Task DeleteItemAsync(string itemPath)
        {
            return DeleteItemAsync(Folder, itemPath);
        }

        /// <summary>
        /// Determine the existance of a file at the specified path.
        /// To check for folders, use <see cref="ItemExistsAsync(string)" />.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="isRecursive">Whether the file should be searched for recursively.</param>
        /// <returns>A task with the result of the file query.</returns>
        public Task<bool> FileExistsAsync(string fileName, bool isRecursive = false)
        {
            return FileExistsAsync(Folder, fileName, isRecursive);
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
            return SaveFileAsync<T>(Folder, filePath, value);
        }

        private async Task<bool> ItemExistsAsync(StorageFolder folder, string itemName)
        {
            var item = await folder.TryGetItemAsync(itemName);
            return item != null;
        }

        private Task<bool> FileExistsAsync(StorageFolder folder, string fileName, bool isRecursive)
        {
            return folder.FileExistsAsync(fileName, isRecursive);
        }

        private async Task<T> ReadFileAsync<T>(StorageFolder folder, string filePath, T @default = default)
        {
            string value = await StorageFileHelper.ReadTextFromFileAsync(folder, filePath);
            return (value != null) ? _serializer.Deserialize<T>(value) : @default;
        }

        private async Task<IList<Tuple<DirectoryItemType, string>>> ReadFolderAsync(StorageFolder folder, string folderPath)
        {
            var targetFolder = await folder.GetFolderAsync(folderPath);
            var items = await targetFolder.GetItemsAsync();

            return items.Select((item) =>
            {
                var itemType = item.IsOfType(StorageItemTypes.File) ? DirectoryItemType.File
                    : item.IsOfType(StorageItemTypes.Folder) ? DirectoryItemType.Folder
                    : DirectoryItemType.None;

                return new Tuple<DirectoryItemType, string>(itemType, item.Name);
            }).ToList();
        }

        private Task<StorageFile> SaveFileAsync<T>(StorageFolder folder, string filePath, T value)
        {
            return StorageFileHelper.WriteTextToFileAsync(folder, _serializer.Serialize(value)?.ToString(), filePath, CreationCollisionOption.ReplaceExisting);
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
