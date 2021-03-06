﻿namespace Gu.Localization.Tests
{
    using System;
    using System.Globalization;

    using NUnit.Framework;

    public partial class TranslatorTests
    {
        public class OneParameter
        {
            [TestCase("en", 1, "Value: 1")]
            [TestCase("sv", 1, "Värde: 1")]
            [TestCase(null, 1, "Neutral: 1")]
            public void HappyPath(string cultureName, object arg, string expected)
            {
                var culture = cultureName != null
                                         ? CultureInfo.GetCultureInfo(cultureName)
                                         : CultureInfo.InvariantCulture;
                var key = nameof(Properties.Resources.ValidFormat__0__);

                Translator.CurrentCulture = culture;
                var actual = Translator.Translate(Properties.Resources.ResourceManager, key, arg);
                Assert.AreEqual(expected, actual);

                Translator.CurrentCulture = null;
                actual = Translator.Translate(Properties.Resources.ResourceManager, key, culture, arg);
                Assert.AreEqual(expected, actual);
            }

            [TestCase("en", 1, "Invalid format string: \"Value: {0} {2}\".")]
            [TestCase("sv", 1, "Invalid format string: \"Värde: \" for the single argument: 1.")]
            public void Throws(string cultureName, object arg, string expected)
            {
                var culture = cultureName != null
                                         ? CultureInfo.GetCultureInfo(cultureName)
                                         : CultureInfo.InvariantCulture;
                var key = nameof(Properties.Resources.InvalidFormat__0__);

                Translator.CurrentCulture = culture;
                var actual = Assert.Throws<FormatException>(() => Translator.Translate(Properties.Resources.ResourceManager, key, arg));
                Assert.AreEqual(expected, actual.Message);

                Translator.CurrentCulture = null;
                actual = Assert.Throws<FormatException>(() => Translator.Translate(Properties.Resources.ResourceManager, key, culture, arg));
                Assert.AreEqual(expected, actual.Message);
            }

            [TestCase("en", 1, "{\"Value: {0} {2}\" : 1}")]
            [TestCase("sv", 1, "{\"Värde: \" : 1}")]
            public void ReturnsInfo(string cultureName, object arg, string expected)
            {
                var culture = cultureName != null
                                         ? CultureInfo.GetCultureInfo(cultureName)
                                         : CultureInfo.InvariantCulture;
                var key = nameof(Properties.Resources.InvalidFormat__0__);

                Translator.ErrorHandling = ErrorHandling.ReturnErrorInfo;
                Translator.CurrentCulture = culture;
                var actual = Translator.Translate(Properties.Resources.ResourceManager, key, arg);
                Assert.AreEqual(expected, actual);

                Translator.ErrorHandling = ErrorHandling.Throw;
                Translator.CurrentCulture = null;
                actual = Translator.Translate(Properties.Resources.ResourceManager, key, culture, arg, ErrorHandling.ReturnErrorInfo);
                Assert.AreEqual(expected, actual);
            }
        }
    }
}
