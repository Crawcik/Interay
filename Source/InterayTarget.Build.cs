using Flax.Build;

/// <inheritdoc />
public class InterayTarget : GameProjectTarget
{
	/// <inheritdoc />
	public override void Init()
	{
		base.Init();

		// Reference the modules for game
		Modules.Add("Interay");
	}
}
