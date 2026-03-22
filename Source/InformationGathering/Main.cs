using System;
using System.IO;
using SML;
using UnityEngine;

[Mod.SalemMod]
public class Main
{
	public void Start()
	{
		Console.WriteLine("Loading InformationGathering mod");
		Console.WriteLine("check for directory");
		if (!Directory.Exists(Path.GetDirectoryName(Application.dataPath) + "/SalemModLoader/ModFolders/InformationGathering"))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(Application.dataPath) + "/SalemModLoader/ModFolders/InformationGathering");
		}
	}
}