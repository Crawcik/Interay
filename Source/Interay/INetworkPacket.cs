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
		public byte ReadByte();

		/// <summary>
		/// Reads a bytes from the buffer.
		/// </summary>
		public byte[] ReadBytes(int length);

		/// <summary>
		/// Reads a struct type from the buffer.
		/// </summary>
		public T ReadStruct<T>() where T : struct;

		/// <summary>
		/// Writes a byte to the buffer.
		/// </summary>
		public void WriteByte(byte value);

		/// <summary>
		/// Writes a bytes to the buffer.
		/// </summary>
		public void WriteBytes(ref byte[] value);

		/// <summary>
		/// Writes a struct type to the buffer.
		/// </summary>
		public void WriteStruct<T>(ref T value) where T : struct;
		#endregion
	}
}