// Very unsafe! Change stuff carefully!
using System;
using System.Runtime.InteropServices;

namespace Interay
{
	/// <summary>
	/// The <see cref="DefaultNetworkPacket"/> is default serializing class with internal allocation methods.
	/// </summary>
	public unsafe class DefaultNetworkPacket : INetworkPacket
	{
		#region Fields
		internal readonly IntPtr Pointer;
		private readonly bool _allocated = false;
		private byte* _buffer;
		private int _position = 0;
		private bool _disposed = false;
		private int _size;
		#endregion

		#region Properties
		/// <inheritdoc/>
		public int Size => _size;
		#endregion

		#region Constructors
		/// <summary>
		/// When using this constructor, remember to call <see cref="Dispose"/> method or use using().
		/// </summary>
		/// <param name="bufferSize">The size of the buffer.</param>
		public DefaultNetworkPacket(int bufferSize)
		{
			Pointer = Marshal.AllocHGlobal(bufferSize);
			_size = bufferSize;
			_buffer = (byte*)Pointer;
			_allocated = true;
		}

		/// <summary>
		/// When using this constructor, you will have to dispose the buffer yourself if buffer is pinned.
		/// </summary>
		/// <param name="buffer">Pointer to the buffer.</param>
		/// <param name="bufferSize">The size of the buffer.</param>
		public DefaultNetworkPacket(byte* buffer, int bufferSize)
		{
			Pointer = (IntPtr)buffer;
			_size = bufferSize;
			_buffer = buffer;
		}

		/// <summary>
		/// When using this constructor, you will have to dispose the buffer yourself if buffer is pinned.
		/// </summary>
		/// <param name="buffer">Pointer to the buffer.</param>
		/// <param name="bufferSize">The size of the buffer.</param>
		public DefaultNetworkPacket(IntPtr buffer, int bufferSize)
		{
			Pointer = buffer;
			_size = bufferSize;
			_buffer = (byte*)buffer;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~DefaultNetworkPacket()
		{
			this.Dispose();
		}
		#endregion

		#region Methods
		/// <inheritdoc/>
		public byte ReadByte() 
		{
			if (_position++ >= _size)
			{
				_position--;
				throw new IndexOutOfRangeException();
			}
			return *_buffer++;
		}

		/// <inheritdoc/>
		public byte[] ReadBytes(int length) 
		{
			var designated = _position + length;
			if (designated > _size)
				throw new IndexOutOfRangeException();
			_position = designated;
			var bytes = new byte[length];
			var i = 0;
			while (i < length)
				bytes[i++] = *_buffer++;
			return bytes;
		}

		/// <inheritdoc/>
		public void WriteByte(byte value)
		{
			if (_position++ >= _size)
			{
				_position--;
				throw new IndexOutOfRangeException();
			}
			*_buffer++ = value;
		}

		/// <inheritdoc/>
		public void WriteBytes(byte[] value) => WriteBytes(value, 0, value.Length);

		/// <inheritdoc/>
		public void WriteBytes(byte[] value, int offset, int lenght)
		{
			var designated = _position + lenght;
			if (designated > _size)
				throw new IndexOutOfRangeException();
			_position = designated;
			lenght += offset;
			var i = offset;
			while (i < lenght)
				*_buffer++ = value[i++];
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_disposed)
			 	return;
			_buffer = null;
			_position = _size;
			if (_allocated)
				Marshal.FreeHGlobal(Pointer);
			_disposed = true;
			GC.SuppressFinalize(this);
		}
		#endregion
	}
}