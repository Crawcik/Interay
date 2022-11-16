using System;
using FlaxEngine;

namespace Interay
{
	/// <summary>
	/// Interay plugin.
	/// </summary>
	internal class PluginInfo : GamePlugin
	{
		#if FLAX_1_3 || FLAX_1_2 || FLAX_1_1 || FLAX_1_0
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
		#else
		public PluginInfo() : base()
		{
			_description = new PluginDescription
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
		#endif
	}
}
