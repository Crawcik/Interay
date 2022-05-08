using Flax.Build;

/// <inheritdoc />
public class InterayEditorTarget : GameProjectEditorTarget
{
	/// <inheritdoc />
	public override void Init()
	{
		base.Init();

		// Reference the modules for editor
		Modules.Add("Interay");
	}
}
