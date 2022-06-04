using System;
using System.Runtime.InteropServices;

namespace Interay.Transport
{
	internal unsafe class EnetNetworkPacket : INetworkPacket
	{
		/// <inheritdoc/>
		public readonly IntPtr Packet;
		private readonly IntPtr _bufferPointer;
		private readonly int _size;
		private byte* _buffer;
		private int _position = 0;
		private bool _disposed = false;

		public int Size => _size;

		public EnetNetworkPacket(int size, Enet.PacketFlags flags)
		{
			_bufferPointer = Marshal.AllocHGlobal(size);
			_size = size;
			_buffer = (byte*)_bufferPointer;
			Packet = Enet.Native.CreatePacket(_bufferPointer, (IntPtr)size, flags);
		}

		public EnetNetworkPacket(Enet.ENetEvent enetEvent)
		{
			Packet = enetEvent.Packet;
			_bufferPointer = Enet.Native.GetPacketData(enetEvent.Packet);
			_buffer = (byte*)_bufferPointer;
			_size = (int)Enet.Native.GetPacketSize(enetEvent.Packet);
		}

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
		public void WriteBytes(byte[] value)
		{
			var designated = _position + value.Length;
			if (designated > _size)
				throw new IndexOutOfRangeException();
			_position = designated;
			var i = 0;
			while (i < value.Length)
				*_buffer++ = value[i++];
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (_disposed)
			 	return;
			Enet.Native.DestroyPacket(Packet);
			Marshal.FreeHGlobal(_bufferPointer);
			_buffer = null;
			_position = -1;
			_disposed = true;
		}
	}
}