using System.IO;
using Flax.Build;
using Flax.Build.NativeCpp;

/// <inheritdoc />
public class Interay : GameModule
{
	/// <inheritdoc />
	public override void Init()
	{
		base.Init();

		// C#-only scripting
		BuildNativeCode = false;
	}

	/// <inheritdoc />
	public override void Setup(BuildOptions options)
	{
		base.Setup(options);
		string path = Path.Combine(FolderPath, "..", "..", "Content", "Enet");
		switch (options.Platform.Target)
		{
			case TargetPlatform.Windows:
				options.DependencyFiles.Add(Path.Combine(path, "enet.dll"));
				break;
			case TargetPlatform.Linux:
			case TargetPlatform.Mac:
				options.DependencyFiles.Add(Path.Combine(path, "libenet.so"));
				break;
		}
		options.ScriptingAPI.IgnoreMissingDocumentationWarnings = false;

		// Here you can modify the build options for your game module
		// To reference another module use: options.PublicDependencies.Add("Audio");
		// To add C++ define use: options.PublicDefinitions.Add("COMPILE_WITH_FLAX");
		// To learn more see scripting documentation.
	}
}
