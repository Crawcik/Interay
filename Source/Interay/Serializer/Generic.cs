using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Interay.Serializer
{
	/// <summary>
	/// Serializes and deserializes generic types from/to packets.
	/// </summary>
	public class General : NetworkSerializer
	{
		private bool _initialized;
		private int[] _typeSizes;

		/// <inheritdoc/>
		public override bool IsInitialized => _initialized;

		/// <inheritdoc/>
		public override bool Initialize()
		{
			_typeSizes = new int[]
			{
				0, 0, 0, // Empry, Object, DBNull
				1, 1, 1, 1, // Boolean, Char, SByte, Byte
				2, 2, 4, 4, // Int16, UInt16, Int32, UInt32
				8, 8, 4, 8, // Int64, UInt64, Single, Double
				16, 8, 0, 0 // Decimal, DateTime, ?, String
			};
			 _initialized = true;
			 return true;
		}

		/// <inheritdoc/>
		public override unsafe bool Serialize(INetworkPacket packet, Type type, object data)
		{
			var isArray = type.IsArray;
			if (isArray)
				type = type.GetElementType();
			var flag = Type.GetTypeCode(type);
			if (flag == TypeCode.DBNull || flag == TypeCode.Empty || flag == TypeCode.Object || (isArray && flag == TypeCode.String))
				return false;
			// packet.WriteByte((byte)((int)flag | (isArray ? 0x80 : 0))); // Not needed cause we check type on designated method

			var func = GetWriteFunc(flag);
			var typeSize = _typeSizes[(int)flag];
			byte[] buffer;
			if (isArray)
			{
				var array = (Array)data;
				var count = (array.Length * typeSize) + 2;
				buffer = new byte[count];
				Utility.Write(buffer, 0, (ushort)count);
				var index = 0;
				for (var offset = 2; offset < count; offset += typeSize)
				{
					func(buffer, offset, array.GetValue(index));
				}
			}
			else
			{
				if (flag == TypeCode.String)
				{
					Utility.Write(packet, (string)data);
					return true;
				}
				buffer = new byte[typeSize];
				func(buffer, 0, data);
			}
			packet.WriteBytes(buffer);
			return true;

		}

		/// <inheritdoc/>
		public override bool Deserialize(INetworkPacket packet, Type type, out object data)
		{
			var isArray = type.IsArray;
			if (isArray)
				type = type.GetElementType();
			var flag = Type.GetTypeCode(type);
			if (flag == TypeCode.DBNull || flag == TypeCode.Empty || flag == TypeCode.Object || (isArray && flag == TypeCode.String))
			{
				data = null;
				return false;
			}

			var func = GetReadFunc(flag);
			var typeSize = _typeSizes[(int)flag];
			byte[] buffer;
			if (isArray)
			{
				buffer = packet.ReadBytes(2);
				var count = (int)Utility.ReadUShort(buffer, 0);
				buffer = packet.ReadBytes(count * typeSize);
				var result = CreateArray(flag, count);
				for (int i = 0; i < count; i++)
				{
					var value = func(buffer, typeSize * i);
					result.SetValue(value, i);
				}
				data = result;
			}
			else if (flag == TypeCode.String)
			{
				data = Utility.ReadString(packet);
			}
			else
			{
				buffer = packet.ReadBytes(typeSize);
				data = func(buffer, 0);
			}
			return true;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			_initialized = false;
		}

		private Array CreateArray(TypeCode typeCode, int size) 
		{
			switch (typeCode)
			{
				case TypeCode.Boolean:
					return new bool[size];
				case TypeCode.SByte:
					return new sbyte[size];
				case TypeCode.Byte:
					return new byte[size];
				case TypeCode.Char:
					return new char[size];
				case TypeCode.Int16:
					return new short[size];
				case TypeCode.UInt16:
					return new ushort[size];
				case TypeCode.Int32:
					return new int[size];
				case TypeCode.UInt32:
					return new uint[size];
				case TypeCode.Int64:
					return new long[size];
				case TypeCode.UInt64:
					return new ulong[size];
				case TypeCode.Single:
					return new float[size];
				case TypeCode.Double:
					return new double[size];
				case TypeCode.Decimal:
					return new decimal[size];
				case TypeCode.DateTime:
					return new DateTime[size];
				default:
					return null;
			}
		}

		private Func<byte[], int, object> GetReadFunc(TypeCode typeCode) 
		{
			switch (typeCode)
			{
				case TypeCode.Boolean:
					return (buffer, offset) => buffer[offset] == 1;
				case TypeCode.SByte:
					return (buffer, offset) => (sbyte)buffer[offset];
				case TypeCode.Byte:
					return (buffer, offset) => buffer[offset];
				case TypeCode.Char:
					return (buffer, offset) => (char)buffer[offset];
				case TypeCode.Int16:
					return (buffer, offset) => Utility.ReadShort(buffer, offset);
				case TypeCode.UInt16:
					return (buffer, offset) => Utility.ReadUShort(buffer, offset);
				case TypeCode.Int32:
					return (buffer, offset) => Utility.ReadInt(buffer, offset);
				case TypeCode.UInt32:
					return (buffer, offset) => Utility.ReadUInt(buffer, offset);
				case TypeCode.Int64:
					return (buffer, offset) => Utility.ReadLong(buffer, offset);
				case TypeCode.UInt64:
					return (buffer, offset) => Utility.ReadULong(buffer, offset);
				case TypeCode.Single:
					return (buffer, offset) => Utility.ReadFloat(buffer, offset);
				case TypeCode.Double:
					return (buffer, offset) => Utility.ReadDouble(buffer, offset);
				case TypeCode.Decimal:
					return (buffer, offset) => Utility.ReadDecimal(buffer, offset);
				case TypeCode.DateTime:
					return (buffer, offset) => new DateTime(Utility.ReadLong(buffer, offset));
				default:
					return null;
			}
		}

		private Action<byte[], int, object> GetWriteFunc(TypeCode typeCode) 
		{
			switch (typeCode)
			{
				case TypeCode.Boolean:
					return (buffer, offset, data) => buffer[offset] = (byte)(((bool)data) ? 1 : 0);
				case TypeCode.SByte:
					return (buffer, offset, data) => buffer[offset] = (byte)(sbyte)data;
				case TypeCode.Byte:
					return (buffer, offset, data) => buffer[offset] = (byte)data;
				case TypeCode.Char:
					return (buffer, offset, data) => buffer[offset] = (byte)(char)data;
				case TypeCode.Int16:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (short)data);
				case TypeCode.UInt16:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (ushort)data);
				case TypeCode.Int32:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (int)data);
				case TypeCode.UInt32:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (uint)data);
				case TypeCode.Int64:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (long)data);
				case TypeCode.UInt64:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (ulong)data);
				case TypeCode.Single:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (float)data);
				case TypeCode.Double:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (double)data);
				case TypeCode.Decimal:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (decimal)data);
				case TypeCode.DateTime:
					return (buffer, offset, data) => Utility.Write(buffer, offset, ((DateTime)data).Ticks);
				default:
					return null;
			}
		}

		public static class Utility
		{
			#region Read
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static short ReadShort(byte[] buffer, int offset) 
				=> (short)(buffer[offset++] | buffer[offset] << 8);
				
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static ushort ReadUShort(byte[] buffer, int offset) 
				=> (ushort)(buffer[offset++] | buffer[offset] << 8);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static int ReadInt(byte[] buffer, int offset) 
				=> buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset] << 24;
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static uint ReadUInt(byte[] buffer, int offset) 
				=> (uint)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset] << 24);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static float ReadFloat(byte[] buffer, int offset) 
				=> (float)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset] << 24);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static long ReadLong(byte[] buffer, int offset) 
				=> (long)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24
				| buffer[offset++] << 32 | buffer[offset++] << 40 | buffer[offset++] << 48 | buffer[offset] << 56);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static ulong ReadULong(byte[] buffer, int offset) 
				=> (ulong)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24
				| buffer[offset++] << 32 | buffer[offset++] << 40 | buffer[offset++] << 48 | buffer[offset] << 56);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static double ReadDouble(byte[] buffer, int offset) 
				=> (double)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24
				| buffer[offset++] << 32 | buffer[offset++] << 40 | buffer[offset++] << 48 | buffer[offset] << 56);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static decimal ReadDecimal(byte[] buffer, int offset)
			{
				var bits = new int[4];
				bits[0] = buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24;
				bits[1] = buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24;
				bits[2] = buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24;
				bits[3] = buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset] << 24;
				return new Decimal(bits);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static string ReadString(INetworkPacket packet) 
			{
				// NOTE: This can be faster!
				var lenght = ReadUShort(packet.ReadBytes(2), 0);
				return Encoding.UTF8.GetString(packet.ReadBytes(lenght));
			}
			#endregion

			#region Write
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Write(byte[] buffer, int offset, short value) 
			{
				buffer[offset++] = (byte)value;
            	buffer[offset] = (byte)(value >> 8);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Write(byte[] buffer, int offset, ushort value) 
			{
				buffer[offset++] = (byte)value;
            	buffer[offset] = (byte)(value >> 8);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Write(byte[] buffer, int offset, int value) 
			{
				buffer[offset++] = (byte)value;
            	buffer[offset++] = (byte)(value >> 8);
				buffer[offset++] = (byte)(value >> 16);
				buffer[offset] = (byte)(value >> 24);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Write(byte[] buffer, int offset, uint value) 
			{
				buffer[offset++] = (byte)value;
            	buffer[offset++] = (byte)(value >> 8);
				buffer[offset++] = (byte)(value >> 16);
				buffer[offset] = (byte)(value >> 24);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe void Write(byte[] buffer, int offset, float value) 
			{
				var tmpValue = *(uint *)&value;
				buffer[offset++] = (byte)tmpValue;
            	buffer[offset++] = (byte)(tmpValue >> 8);
				buffer[offset++] = (byte)(tmpValue >> 16);
				buffer[offset] = (byte)(tmpValue >> 24);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Write(byte[] buffer, int offset, long value) 
			{
				buffer[offset++] = (byte)value;
            	buffer[offset++] = (byte)(value >> 8);
				buffer[offset++] = (byte)(value >> 16);
				buffer[offset++] = (byte)(value >> 24);
				buffer[offset++] = (byte)(value >> 32);
            	buffer[offset++] = (byte)(value >> 40);
				buffer[offset++] = (byte)(value >> 48);
				buffer[offset] = (byte)(value >> 56);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Write(byte[] buffer, int offset, ulong value) 
			{
				buffer[offset++] = (byte)value;
            	buffer[offset++] = (byte)(value >> 8);
				buffer[offset++] = (byte)(value >> 16);
				buffer[offset++] = (byte)(value >> 24);
				buffer[offset++] = (byte)(value >> 32);
            	buffer[offset++] = (byte)(value >> 40);
				buffer[offset++] = (byte)(value >> 48);
				buffer[offset] = (byte)(value >> 56);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe void Write(byte[] buffer, int offset, double value) 
			{
				var tmpValue = *(ulong *)&value;
				buffer[offset++] = (byte)tmpValue;
            	buffer[offset++] = (byte)(tmpValue >> 8);
				buffer[offset++] = (byte)(tmpValue >> 16);
				buffer[offset++] = (byte)(tmpValue >> 24);
				buffer[offset++] = (byte)(tmpValue >> 32);
            	buffer[offset++] = (byte)(tmpValue >> 40);
				buffer[offset++] = (byte)(tmpValue >> 48);
				buffer[offset] = (byte)(tmpValue >> 56);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void Write(byte[] buffer, int offset, decimal value) 
			{
				var bits = Decimal.GetBits(value);
				buffer[offset++] = (byte)bits[0];
				buffer[offset++] = (byte)(bits[0] >> 8);
				buffer[offset++] = (byte)(bits[0] >> 16);
				buffer[offset++] = (byte)(bits[0] >> 24);
				
				buffer[offset++] = (byte)bits[1];
				buffer[offset++] = (byte)(bits[1] >> 8);
				buffer[offset++] = (byte)(bits[1] >> 16);
				buffer[offset++] = (byte)(bits[1] >> 24);
	
				buffer[offset++] = (byte)bits[2];
				buffer[offset++] = (byte)(bits[2] >> 8);
				buffer[offset++] = (byte)(bits[2] >> 16);
				buffer[offset++] = (byte)(bits[2] >> 24);
				
				buffer[offset++] = (byte)bits[3];
				buffer[offset++] = (byte)(bits[3] >> 8);
				buffer[offset++] = (byte)(bits[3] >> 16);
				buffer[offset] = (byte)(bits[3] >> 24);
			}

			public static unsafe void Write(INetworkPacket packet, string value) 
			{
				// NOTE: This can be faster!
				var buffer = new byte[2];
				var lenght = value.Length;
				Write(buffer, 0, (ushort)lenght);
				Array.Resize(ref buffer, lenght + 2);
				Encoding.UTF8.GetBytes(value, 0, lenght, buffer, 2);
				packet.WriteBytes(buffer);
			}
			#endregion
		}
	}
}