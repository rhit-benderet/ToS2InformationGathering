using System.IO;
using System.Reflection;

public class Embed
{
	public static string GetEmbeddedText(string filePath)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		StreamReader streamReader = new StreamReader(executingAssembly.GetManifestResourceStream(filePath));
		return streamReader.ReadToEnd();
	}
}
