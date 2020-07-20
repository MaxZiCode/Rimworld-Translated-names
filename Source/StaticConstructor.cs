using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Xml.Linq;

using Verse;
using RimWorld;
using HarmonyLib;

namespace TranslatedNames
{
	[StaticConstructorOnStartup]
	public static class StaticConstructor
	{
		const string firstMale = "First_Male";
		const string firstFemale = "First_Female";
		const string nickMale = "Nick_Male";
		const string nickFemale = "Nick_Female";
		const string nickUnisex = "Nick_Unisex";
		const string last = "Last";

		public static readonly string rootPath = ModLister.AllInstalledMods.First(m => m.Name == "Translated names").RootDir.FullName;

		public static readonly string translationPath;

		static StaticConstructor()
		{
#if DEBUG
			GetFilesWithNames();
#endif
			string lang = GetTranslationLanguage(rootPath + @"\Translations\Main settings.xml");
			if (!string.IsNullOrEmpty(lang))
			{
				translationPath = rootPath + $@"\Translations\{lang}\";

				var harmony = new Harmony("rimworld.maxzicode.translatednames.mainconstructor");
				harmony.PatchAll(Assembly.GetExecutingAssembly());
#if DEBUG
				Log.Message("Loaded translation for the language: " + lang); 
#endif
			}
		}

		private static void GetFilesWithNames()
		{
			string[] namesLabels = { firstMale, firstFemale, nickMale, nickFemale, nickUnisex, last };
			string debugPath = rootPath + @"\Debug\";

			Dictionary<string, List<string>> namesDict = new Dictionary<string, List<string>>();
			foreach (var name in namesLabels)
				namesDict.Add(name, GenFile.LinesFromFile(@"Names\" + name).ToList());

			foreach (var gender in new GenderPossibility[] { GenderPossibility.Male, GenderPossibility.Female, GenderPossibility.Either })
				foreach (var n in PawnNameDatabaseSolid.GetListForGender(gender))
					FillByGender(gender, n, namesDict);

			foreach (var bio in SolidBioDatabase.allBios)
				FillByGender(bio.gender, bio.name, namesDict);

			foreach (string name in namesLabels)
			{
				DirectoryInfo dir = Directory.CreateDirectory(debugPath);
				using (StreamWriter sw = File.CreateText(dir.FullName + $@"\{name}.txt"))
				{
					foreach (var s in namesDict[name])
						sw.WriteLine(s);
				}
			}
			Log.Message("Files with names have been created in: " + debugPath);
		}

		private static void FillByGender(GenderPossibility gender, NameTriple name, Dictionary<string, List<string>> dict)
		{
			string first = name.First;
			string nick = name.Nick;
			AddName(dict[last], name.Last);

			switch (gender)
			{
				case GenderPossibility.Male:
				{
					AddName(dict[firstMale], first);
					if (!string.IsNullOrEmpty(nick) && !dict[nickUnisex].Contains(nick))
						AddName(dict[nickMale], nick);
				}
				break;
				case GenderPossibility.Female:
				{
					AddName(dict[firstFemale], first);
					if (!string.IsNullOrEmpty(nick) && !dict[nickUnisex].Contains(nick))
						AddName(dict[nickFemale], nick);
				}
				break;
				case GenderPossibility.Either:
				{
					AddName(dict[firstMale], first);
					AddName(dict[firstFemale], first);
					AddName(dict[nickUnisex], nick);
				}
				break;
				default:
					Log.Error("There is an error in the gender switch for name " + name.ToString());
					break;
			}
		}

		private static void AddName(List<string> collection, string s)
		{
			if (!string.IsNullOrEmpty(s) && !collection.Contains(s))
				collection.Add(s);
		}

		private static string GetTranslationLanguage(string mainSettingsPath)
		{
			XDocument xd = XDocument.Load(mainSettingsPath);
			string langEng = LanguageDatabase.activeLanguage.FriendlyNameEnglish;
			string langNat = LanguageDatabase.activeLanguage.FriendlyNameNative;

			foreach (var tLanguage in xd.Root.Elements())
			{
				foreach (var supLanguage in tLanguage.Element("supported_languages").Elements())
				{
					if (langEng.Contains(supLanguage.Value.Trim()) || langNat.Contains(supLanguage.Value.Trim()))
						return tLanguage.Attribute("folderName").Value;
				}
			}
			return null;
		}


		[HarmonyPatch(typeof(PawnBioAndNameGenerator), "GiveAppropriateBioAndNameTo", MethodType.Normal)]
		class PatchNameGiver
		{
			public static void Postfix(Pawn pawn)
			{
				if (!(pawn.Name is NameTriple realName))
				{
					Log.Error("Trying to translate not a NameTriple!");
					return;
				}

				string first = TranslationInfo.GetTranslation(realName.First);
				string nick = TranslationInfo.GetTranslation(realName.Nick);
				string last = TranslationInfo.GetTranslation(realName.Last);

				NameTriple newNameTriple = new NameTriple(first, nick, last);
#if DEBUG
				Log.Message(pawn.Name.ToStringFull + " | " + newNameTriple.ToStringFull);
#endif
				pawn.Name = newNameTriple;
			}
		}

		[HarmonyPatch(typeof(Pawn), "ExposeData", MethodType.Normal)]
		class PatchPawn_ExposeData
		{
			static void Postfix(Pawn __instance)
			{
				if (__instance.def.race.intelligence == Intelligence.Humanlike)
					PatchNameGiver.Postfix(__instance);
			}
		}
	}
}
