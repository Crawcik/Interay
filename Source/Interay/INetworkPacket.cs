// Very unsafe! Change stuff carefully!
using System;

namespace Interay
{
	/// <summary>
	/// The <see cref="INetworkPacket"/> is used to serialize and deserialize messages.
	/// </summary>
	public interface INetworkPacket : IDisposable
	{
		#region Properties
		/// <summary>
		/// The size of the packet.
		/// </summary>
		public int Size { get; }
		#endregion

		#region Methods
		/// <summary>
		/// Reads a value from the buffer.
		/// </summary>
		byte ReadByte();

		/// <summary>
		/// Reads a bytes from the buffer.
		/// </summary>
		byte[] ReadBytes(int length);

		/// <summary>
		/// Writes a byte to the buffer.
		/// </summary>
		void WriteByte(byte value);

		/// <summary>
		/// Writes a bytes to the buffer.
		/// </summary>
		void WriteBytes(byte[] value);
		#endregion
	}
}