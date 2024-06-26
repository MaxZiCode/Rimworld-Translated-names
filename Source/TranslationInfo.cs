﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Text.RegularExpressions;

using RimWorld;
using Verse;

namespace TranslatedNames
{
    public static class TranslationInfo
    {
        private static readonly Dictionary<string, string> translations = new Dictionary<string, string>();

        static TranslationInfo()
        {
            foreach (var filePath in Directory.GetFiles(StaticConstructor.translationPath))
                try
                {
                    XDocument xd = XDocument.Load(filePath);

                    foreach (var xe in xd.Root.Descendants())
                    {
                        try
                        {
                            string key = (string)xe.Attribute("name");
                            string value = (string)xe.Attribute("t-Name");
                            if (!translations.ContainsKey(key))
                                translations.Add(key, value);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.ToString());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
        }

        public static string GetTranslation(string name)
        {
            string tName = name;
            string engPattern = "[A-Za-z]+";

            if (Regex.IsMatch(name, engPattern))
            {
                if (translations.ContainsKey(name))
                {
                    tName = translations[name];

#if DEBUG
                    if (Regex.IsMatch(tName, engPattern))
                        Log.Warning($"The name '{tName}' not translated in the translation dictionary!");
#endif
                }
#if DEBUG
                else
                    Log.Warning($"The translation dictionary doesn't contain the key '{tName}'.");
#endif
            }
            return tName;
        }
    }
}
