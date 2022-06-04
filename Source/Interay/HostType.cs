namespace Interay
{
	/// <summary>
	/// Host type of the network
	/// </summary>
	public enum HostType : byte
	{
		/// Server.
		Server = 0b01,
		/// Client.
		Client = 0b10,
		/// Server and Client.
		Host = 0b11
	}
}