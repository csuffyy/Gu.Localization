﻿namespace Gu.Localization
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Threading;

    /// <summary> Class for translating resources </summary>
    public static partial class Translator
    {
        private static CultureInfo currentCulture = Thread.CurrentThread.CurrentUICulture;
        private static DirectoryInfo resourceDirectory = ResourceCultures.DefaultResourceDirectory();
        private static SortedSet<CultureInfo> cultures = GetAllCultures();

        /// <summary>
        /// Notifies when the current language changes.
        /// </summary>
        public static event EventHandler<CultureInfo> CurrentCultureChanged;

        /// <summary>
        /// Gets or sets set the current directory where resources are found.
        /// Default is Directory.GetCurrentDirectory()
        /// Changing the default is perhaps useful in tests.
        /// </summary>
        public static DirectoryInfo ResourceDirectory
        {
            get
            {
                return resourceDirectory;
            }

            set
            {
                resourceDirectory = value;
                cultures = GetAllCultures();
            }
        }

        /// <summary>
        /// Gets or sets the culture to translate to
        /// </summary>
        public static CultureInfo CurrentCulture
        {
            get
            {
                return currentCulture ??
                       Cultures.FirstOrDefault() ??
                       CultureInfo.InvariantCulture;
            }

            set
            {
                if (CultureInfoComparer.DefaultEquals(currentCulture, value))
                {
                    return;
                }

                currentCulture = value;
                OnCurrentCultureChanged(value);
            }
        }

        /// <summary>Gets or sets a value indicating how errors are handled. The default is throw</summary>
        public static ErrorHandling ErrorHandling { get; set; } = ErrorHandling.Throw;

        /// <summary> Gets a list with all cultures found for the application </summary>
        public static IEnumerable<CultureInfo> Cultures => cultures;

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(ResourceManager resourceManager, string key)
        {
            return Translate(resourceManager, key, CurrentCulture);
        }

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <param name="errorHandling">Specifies how error handling is performed.</param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(ResourceManager resourceManager, string key, ErrorHandling errorHandling)
        {
            return Translate(resourceManager, key, CurrentCulture, errorHandling);
        }

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <param name="culture">The culture, if null CultureInfo.InvariantCulture is used</param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(ResourceManager resourceManager, string key, CultureInfo culture)
        {
            return Translate(resourceManager, key, culture, ErrorHandling);
        }

        /// <summary>
        /// Translator.Translate(Properties.Resources.ResourceManager, nameof(Properties.Resources.SomeKey));
        /// </summary>
        /// <param name="resourceManager"> The <see cref="ResourceManager"/> containing translations.</param>
        /// <param name="key">The key in <paramref name="resourceManager"/></param>
        /// <param name="culture">The culture, if null CultureInfo.InvariantCulture is used</param>
        /// <param name="errorHandling">Specifies how to handle errors.</param>
        /// <returns>The key translated to the <see cref="CurrentCulture"/></returns>
        public static string Translate(
            ResourceManager resourceManager,
            string key,
            CultureInfo culture,
            ErrorHandling errorHandling)
        {
            string result;
            TryTranslateOrThrow(resourceManager, key, culture, errorHandling, out result);
            return result;
        }

        private static bool TryTranslateOrThrow(
            ResourceManager resourceManager,
            string key,
            CultureInfo culture,
            ErrorHandling errorHandling,
            out string result)
        {
            if (errorHandling == ErrorHandling.Default)
            {
                errorHandling = ErrorHandling == ErrorHandling.Default
                                    ? ErrorHandling.Throw
                                    : ErrorHandling;
            }

            if (resourceManager == null)
            {
                if (errorHandling == ErrorHandling.Throw)
                {
                    throw new ArgumentNullException(nameof(resourceManager));
                }

                result = string.Format(Properties.Resources.NullManagerFormat, key);
                return false;
            }

            if (string.IsNullOrEmpty(key))
            {
                if (errorHandling == ErrorHandling.Throw)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                result = "key == null";
                return false;
            }

            if (!resourceManager.HasCulture(culture))
            {
                if (errorHandling == ErrorHandling.Throw)
                {
                    var message = $"The resourcemanager {resourceManager.BaseName} does not have a translation for the culture: {culture.Name}";
                    throw new ArgumentOutOfRangeException(nameof(culture), message);
                }

                var neutral = resourceManager.GetString(key, CultureInfo.InvariantCulture);
                if (string.IsNullOrEmpty(neutral))
                {
                    result = string.Format(Properties.Resources.MissingCultureFormat, key);
                    return false;
                }

                if (errorHandling == ErrorHandling.ReturnErrorInfoPreserveNeutral)
                {
                    result = neutral;
                    return true;
                }

                result = string.Format(Properties.Resources.MissingCultureFormat, neutral);
                return false;
            }

            if (!resourceManager.HasKey(key, culture))
            {
                if (errorHandling == ErrorHandling.Throw)
                {
                    var message = $"The resourcemanager {resourceManager.BaseName} does not have a translation for the key: {key} for the culture: {culture.Name}";
                    throw new ArgumentOutOfRangeException(nameof(key), message);
                }

                var neutral = resourceManager.GetString(key, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(neutral))
                {
                    if (errorHandling == ErrorHandling.ReturnErrorInfoPreserveNeutral)
                    {
                        result = neutral;
                        return false;
                    }

                    result = string.Format(Properties.Resources.MissingTranslationFormat, key);
                    return false;
                }

                result = string.Format(Properties.Resources.MissingKeyFormat, key);
                return false;
            }

            result = resourceManager.GetString(key, culture);
            return true;
        }

        private static bool ShouldThrow(ErrorHandling errorHandling)
        {
            if (errorHandling == ErrorHandling.Default)
            {
                errorHandling = ErrorHandling;
            }

            var shouldThrow =
                errorHandling != ErrorHandling.ReturnErrorInfo &&
                errorHandling != ErrorHandling.ReturnErrorInfoPreserveNeutral;

            return shouldThrow;
        }

        private static void OnCurrentCultureChanged(CultureInfo e)
        {
            CurrentCultureChanged?.Invoke(null, e);
        }

        private static SortedSet<CultureInfo> GetAllCultures()
        {
            Debug.WriteLine(resourceDirectory);
            return resourceDirectory?.Exists == true
                       ? new SortedSet<CultureInfo>(ResourceCultures.GetAllCultures(resourceDirectory), CultureInfoComparer.Default)
                       : new SortedSet<CultureInfo>(CultureInfoComparer.Default);
        }
    }
}
