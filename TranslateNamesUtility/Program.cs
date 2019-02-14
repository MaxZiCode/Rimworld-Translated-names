using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml.Linq;

namespace TranslateNamesUtility
{
	class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Ok let's translate those files... You can type 'q' to quit the program anywhere. n to next step.");

			Console.WriteLine("Type a directory path to files with names:");
			DirectoryInfo diNames;
			FileInfo[] filesWithNames;
			for (;;)
			{
				string s = Console.ReadLine().ToLower();
				if (s == "q")
					return;

				diNames = new DirectoryInfo(s);
				DirectoryInfo diFormated = new DirectoryInfo(@"./Formated");
				DirectoryInfo diTranslated = new DirectoryInfo(@"./Translated");
				diFormated.Create();
				diTranslated.Create();

				if (!diNames.Exists)
				{
					Console.WriteLine("***The path doesn't exist!\n");
					continue;
				}

				bool flag = false;
				filesWithNames = diNames.GetFiles();
				if (filesWithNames.Length == 0)
				{
					Console.WriteLine("***The path doesn't contain any files!\n");
					continue;
				}
				foreach (var f in filesWithNames)
				{
					try
					{
						int i = 0;
						using (StreamReader sr = f.OpenText())
							while (!sr.EndOfStream)
							{
								string fileName = f.Name.Insert(f.Name.IndexOf("."), (i / 300).ToString());
								string filePath = diFormated.FullName + "/" + "Formated " + fileName;
								using (StreamWriter sw = File.CreateText(filePath))
								{
									while (!sr.EndOfStream)
									{
										sw.WriteLine($"Name \"{sr.ReadLine()}\"");
										if (++i % 300 == 0)
											break;
									}
								}
								File.Copy(filePath, diTranslated.FullName + "/" + "Translated " + fileName, true);
							}
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
						flag = true;
						break;
					}
				}
				Process.Start("explorer.exe", diTranslated.FullName);
				if (!flag)
					break;
			}
			Console.WriteLine("Formating success!\n");

			Console.WriteLine("And now translate these files with google translate, but don't change files names after translating.");
			Console.WriteLine("Then type a directory path to translated files.");
			for (;;)
			{
				string s = Console.ReadLine().ToLower();
				if (s == "q")
					return;

				DirectoryInfo diTranslated = new DirectoryInfo(s);
				DirectoryInfo diDictionaries = new DirectoryInfo(@"./Dictionaries");
				diDictionaries.Create();

				if (!diTranslated.Exists)
				{
					Console.WriteLine("***The path doesn't exits!\n");
					continue;
				}
				
				FileInfo[] FilesTanslated = diTranslated.GetFiles();
				if (FilesTanslated.Length == 0)
				{
					Console.WriteLine("***The path doesn't contain any files!\n");
					continue;
				}
				
				string[] wordsToRemove = { "имя", "наименование", "название" };

				bool flag = false;
				foreach (var f in filesWithNames)
				{
					try
					{
						XElement xe = new XElement("Translations_dictionary");
						string onlyName = f.Name.Remove(f.Name.Length - 4);

						using (StreamReader srNames = f.OpenText())
							foreach(var ft in from ft in diTranslated.GetFiles() where ft.Name.Contains(onlyName) orderby int.Parse(Regex.Match(ft.Name, @"\d+").Value) select ft)
								using (StreamReader srTranslations = ft.OpenText())
								{
									Console.WriteLine(ft.Name);
									while (!srNames.EndOfStream && !srTranslations.EndOfStream)
									{
										string translation = srTranslations.ReadLine().ToLower();
										foreach (var word in wordsToRemove)
										{
											if (translation.Contains(word.ToLower()))
												translation = translation.Remove(translation.IndexOf(word.ToLower()), word.Length);
										}
										while (translation.Contains("\""))
											translation = translation.Remove(translation.IndexOf("\""), 1);

										translation = translation.Trim();
										if (Regex.IsMatch(translation, "[A-Za-z]"))
											Console.WriteLine("*Word didn't translate " + translation);
										string tName = translation.First().ToString().ToUpper() + translation.Substring(1);
										xe.Add(new XElement("translation", new XAttribute("name", srNames.ReadLine()),
											new XAttribute("t-Name", tName)
											));
									}
								}

						string path = diDictionaries.FullName + "/" + f.Name;
						xe.Save(path.Replace(".txt", ".xml"));
					}
					catch (Exception ex)
					{
						Console.WriteLine(ex);
						flag = true;
						break;
					}
				}
				if (!flag)
					break;
			}
			Console.WriteLine("Dictionaries have been created!\n");

			Console.Write("Press any key to continue...");
			Console.ReadKey();
		}
	}
}
