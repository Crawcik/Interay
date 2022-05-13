// Very unsafe! Change stuff carefully!
using System;
using System.Runtime.InteropServices;

namespace Interay
{
	/// <summary>
	/// The <see cref="NetworkPacket"/> class is used to serialize and deserialize messages.
	/// </summary>
	public unsafe class NetworkPacket : IDisposable
	{
		#region Fields
		/// <summary>
		/// The size of the packet.
		/// </summary>
		public readonly int Size;
		private readonly GCHandle _handle;
		private readonly bool _allocated = false;
		private byte* _buffer;
		private int _position = 0;
		private bool _disposed = false;
		#endregion

		#region Constructors
		/// <summary>
		/// When using this constructor, remember to call <see cref="Dispose"/> method or use using().
		/// </summary>
		/// <param name="bufferSize">The size of the buffer.</param>
		public NetworkPacket(int bufferSize)
		{
			_buffer = (byte*)Marshal.AllocHGlobal(bufferSize);
			Size = bufferSize;
			_allocated = true;
		}

		/// <summary>
		/// When using this constructor, you will have to dispose the buffer yourself if buffer is pinned.
		/// </summary>
		/// <param name="buffer">Pointer to the buffer.</param>
		/// <param name="bufferSize">The size of the buffer.</param>
		public NetworkPacket(byte* buffer, int bufferSize)
		{
			_buffer = buffer;
			Size = bufferSize;
		}

		/// <summary>
		/// When using this constructor, you will have to dispose the buffer yourself if buffer is pinned.
		/// </summary>
		/// <param name="buffer">Pointer to the buffer.</param>
		/// <param name="bufferSize">The size of the buffer.</param>
		public NetworkPacket(IntPtr buffer, int bufferSize)
		{
			_buffer = (byte*)buffer;
			Size = bufferSize;
		}

		/// <summary>
		/// When using this constructor, remember to call <see cref="Dispose"/> method or use using().
		/// </summary>
		/// <param name="buffer">Packet buffer.</param>
		public NetworkPacket(ref byte[] buffer)
		{
			if(buffer is null)
				throw new ArgumentNullException(nameof(buffer));
			_handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
			_buffer = (byte*)_handle.AddrOfPinnedObject();
			Size = buffer.Length;
		}

		/// <summary>
		/// Destructor.
		/// </summary>
		~NetworkPacket()
		{
			this.Dispose();
		}
		#endregion

		#region Methods
		/// <summary>
		/// Reads a value from the buffer.
		/// </summary>
		public byte ReadByte() 
		{
			if (_position++ >= Size)
			{
				_position--;
				throw new IndexOutOfRangeException();
			}
			return *_buffer++;
		}

		/// <summary>
		/// Reads a bytes from the buffer.
		/// </summary>
		public byte[] ReadBytes(int length) 
		{
			var designated = _position + length;
			if (designated > Size)
				throw new IndexOutOfRangeException();
			_position = designated;
			var bytes = new byte[length];
			var i = 0;
			while (i < length)
				bytes[i++] = *_buffer++;
			return bytes;
		}

		/// <summary>
		/// Reads a struct type from the buffer.
		/// </summary>
		/// <remarks>Unsafe method!!!</remarks>
		public T ReadStruct<T>() where T : struct
		{
			var designated = _position + Marshal.SizeOf<T>();
			if (designated > Size)
				throw new IndexOutOfRangeException();
			_position = designated;
			var ret = Marshal.PtrToStructure<T>((IntPtr)_buffer);
			_buffer += Marshal.SizeOf<T>();
			return ret;
		}

		/// <summary>
		/// Writes a byte to the buffer.
		/// </summary>
		public void WriteByte(byte value)
		{
			if (_position++ >= Size)
			{
				_position--;
				throw new IndexOutOfRangeException();
			}
			*_buffer++ = value;
		}

		/// <summary>
		/// Writes a bytes to the buffer.
		/// </summary>
		public void WriteBytes(ref byte[] value)
		{
			var designated = _position + value.Length;
			if (designated > Size)
				throw new IndexOutOfRangeException();
			_position = designated;
			var i = 0;
			while (i < value.Length)
				*_buffer++ = value[i++];
		}

		/// <summary>
		/// Writes a struct type to the buffer.
		/// </summary>
		public void WriteStruct<T>(ref T value) where T : struct
		{
			var designated = _position + Marshal.SizeOf<T>();
			if (designated > Size)
				throw new IndexOutOfRangeException();
			_position = designated;
			Marshal.StructureToPtr(value, (IntPtr)_buffer, false);
		}

		/// <summary>
		/// Gets a pointer to the buffer.
		/// </summary>
		public IntPtr GetPointer() => new IntPtr(_buffer - _position);

		/// <summary>
		/// Disposes all alocated resources.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			 	return;
			_buffer -= _position;
			if (_allocated)
				Marshal.FreeHGlobal((IntPtr)_buffer);
			if(_handle.IsAllocated)
				_handle.Free();
			_disposed = true;
		}
		#endregion
	}
}