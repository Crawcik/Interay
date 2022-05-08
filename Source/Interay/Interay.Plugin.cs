using System;
using FlaxEngine;

namespace Interay
{
	/// <summary>
	/// Interay plugin.
	/// </summary>
	internal class PluginInfo : GamePlugin
	{
		/// <inheritdoc />
		public override PluginDescription Description => new PluginDescription
		{
			Name = "Interay",
			Category = "Network",
			Author = "Crawcik",
			AuthorUrl = "https://github.com/Crawcik",
			HomepageUrl = "https://github.com/Crawcik/Interay",
			RepositoryUrl = "https://github.com/Crawcik/Interay",
			Description = "Flexible networking plugin for many multiplayer games.",
			Version = new Version(1, 0),
			IsAlpha = true,
			IsBeta = false,
		};
	}
}
