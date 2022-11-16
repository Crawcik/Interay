using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Interay.Transport
{
	internal unsafe class EnetNetworkPacket : INetworkPacket
	{
#if FAST_PACKET
		public const MethodImplOptions FastIO = MethodImplOptions.AggressiveInlining;
#else
		public const MethodImplOptions FastIO = MethodImplOptions.NoInlining;
#endif

		/// <inheritdoc/>
		public readonly IntPtr Packet;
		private readonly IntPtr _bufferPointer;
		private readonly int _size;
		private byte* _buffer;
		private int _position = 0;
		private bool _disposed = false;

		public int Size => _size;
		public int Positon => _position;

		public EnetNetworkPacket(int size, Enet.PacketFlags flags)
		{
			_size = size;
			Packet = Enet.Native.CreatePacket(_bufferPointer, (IntPtr)size, flags);
			_bufferPointer = Enet.Native.GetPacketData(Packet);
			_buffer = (byte*)_bufferPointer;
		}

		public EnetNetworkPacket(Enet.ENetEvent enetEvent)
		{
			Packet = enetEvent.Packet;
			_bufferPointer = Enet.Native.GetPacketData(enetEvent.Packet);
			_buffer = (byte*)_bufferPointer;
			_size = (int)Enet.Native.GetPacketSize(enetEvent.Packet);
		}

		~EnetNetworkPacket()
		{
			Dispose();
		}

		/// <inheritdoc/>
		[MethodImpl(FastIO)]
		public byte ReadByte() 
		{
#if !FAST_PACKET
			if (_position++ >= _size)
			{
				_position--;
				throw new IndexOutOfRangeException();
			}
#endif
			return *_buffer++;
		}

		/// <inheritdoc/>
		[MethodImpl(FastIO)]
		public byte[] ReadBytes(int length) 
		{
#if !FAST_PACKET
			var designated = _position + length;
			if (designated > _size)
				throw new IndexOutOfRangeException();
			_position = designated;
#endif
			var bytes = new byte[length];
			var i = 0;
			while (i < length)
				bytes[i++] = *_buffer++;
			return bytes;
		}

		/// <inheritdoc/>
		[MethodImpl(FastIO)]
		public void WriteByte(byte value)
		{
#if !FAST_PACKET
			if (_position++ >= _size)
			{
				_position--;
				throw new IndexOutOfRangeException();
			}
#endif
			*_buffer++ = value;
		}

		/// <inheritdoc/>
		[MethodImpl(FastIO)]
		public void WriteBytes(byte[] value) => WriteBytes(value, 0, value.Length);

		[MethodImpl(FastIO)]

		public void WriteBytes(byte[] value, int offset, int lenght)
		{
#if !FAST_PACKET
			var designated = _position + lenght;
			if (designated > _size)
				throw new IndexOutOfRangeException();
			_position = designated;
#endif
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
			Enet.Native.DestroyPacket(Packet);
			_buffer = null;
			_position = _size;
			_disposed = true;
		}
	}
}