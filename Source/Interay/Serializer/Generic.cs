using System;
using System.Runtime.CompilerServices;
using System.Text;
using FlaxEngine;

namespace Interay.Serializer
{
	/// <summary>
	/// Serializes and deserializes generic types from/to packets.
	/// </summary>
	public class General : NetworkSerializer
	{
		#region Fields
		private bool _initialized;
		private int[] _systemTypeSizes;
		private int[] _flaxTypeSizes;
		#endregion

		/// <inheritdoc/>
		public override bool IsInitialized => _initialized;

		#region Method
		/// <inheritdoc/>
		public override bool Initialize()
		{
			_systemTypeSizes = new int[]
			{
				0, 0, 0, // Empty, Object, DBNull
				1, 1, 1, 1, // Boolean, Char, SByte, Byte
				2, 2, 4, 4, // Int16, UInt16, Int32, UInt32
				8, 8, 4, 8, // Int64, UInt64, Single, Double
				16, 8, 0, 0 // Decimal, DateTime, ?, String
			};
			_flaxTypeSizes = new int[]
			{
				8, 12, 16, // Vector 2-4
				8, 12, 16, // Float 2-4
				16, 24, 32, // Double 2-4
				4, 6, 8, // Half 2-4 
				8, 12, 16, // Int 2-4
				16, 40, // Quaternion, Transform
				64, 16, 36, // Matrix 4x4,2x2,3x3
				16, 4, 16, // Colors
				24, 64, 16, 0, // Bounding Box, Fustrum, Sphere, Oriented
				16, 24, 16, 24 // Plane, Ray, Rectangle, Viewport
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
			var flag = (TypeCodeExt)Type.GetTypeCode(type);
			if (flag == TypeCodeExt.Object)
				flag = CheckIfFlaxType(type);
			packet.WriteByte((byte)((int)flag | (isArray ? 0x80 : 0)));
			if (flag == TypeCodeExt.DBNull || flag == TypeCodeExt.Empty || (isArray && flag == TypeCodeExt.String))
				return false;

			var func = GetWriteFunc(flag);
			var typeSize = ((int)flag & 0x40) == 0x40 ? _flaxTypeSizes[(int)flag & 0x3F] : _systemTypeSizes[(int)flag];
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
			else if (flag == TypeCodeExt.String)
			{
				Utility.Write(packet, (string)data);
				return true;
			}
			else
			{
				buffer = new byte[typeSize];
				func(buffer, 0, data);
			}
			packet.WriteBytes(buffer);
			return true;

		}

		/// <inheritdoc/>
		public override bool Deserialize(INetworkPacket packet, out object data)
		{
			var bit = packet.ReadByte();

			var isArray = (bit & 0x80) == 0x80;
			var flag = (TypeCodeExt)(bit & 0x7F);
			if (flag == TypeCodeExt.DBNull || flag == TypeCodeExt.Empty || flag == TypeCodeExt.Object || (isArray && flag == TypeCodeExt.String))
			{
				data = null;
				return false;
			}

			var func = GetReadFunc(flag);
			var typeSize = ((int)flag & 0x40) == 0x40 ? _flaxTypeSizes[(int)flag & 0x3F] : _systemTypeSizes[(int)flag];
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
			else if (flag == TypeCodeExt.String)
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

		private Array CreateArray(TypeCodeExt typeCode, int size) 
		{
			switch (typeCode)
			{
				case TypeCodeExt.Boolean:
					return new bool[size];
				case TypeCodeExt.SByte:
					return new sbyte[size];
				case TypeCodeExt.Byte:
					return new byte[size];
				case TypeCodeExt.Char:
					return new char[size];
				case TypeCodeExt.Int16:
					return new short[size];
				case TypeCodeExt.UInt16:
					return new ushort[size];
				case TypeCodeExt.Int32:
					return new int[size];
				case TypeCodeExt.UInt32:
					return new uint[size];
				case TypeCodeExt.Int64:
					return new long[size];
				case TypeCodeExt.UInt64:
					return new ulong[size];
				case TypeCodeExt.Single:
					return new float[size];
				case TypeCodeExt.Double:
					return new double[size];
				case TypeCodeExt.Decimal:
					return new decimal[size];
				case TypeCodeExt.DateTime:
					return new DateTime[size];
				case TypeCodeExt.Vector2:
					return new Vector2[size];
				case TypeCodeExt.Vector3:
					return new Vector3[size];
				case TypeCodeExt.Vector4:
					return new Vector4[size];
				case TypeCodeExt.Float2:
					return new Float2[size];
				case TypeCodeExt.Float3:
					return new Float3[size];
				case TypeCodeExt.Float4:
					return new Float4[size];
				case TypeCodeExt.Double2:
					return new Double2[size];
				case TypeCodeExt.Double3:
					return new Double3[size];
				case TypeCodeExt.Double4:
					return new Double4[size];
				case TypeCodeExt.Quaternion:
					return new Quaternion[size];
				case TypeCodeExt.Transform:
					return new Transform[size];
				case TypeCodeExt.Matrix:
					return new Float4[size];
				case TypeCodeExt.Matrix2x2:
					return new Matrix2x2[size];
				case TypeCodeExt.Matrix3x3:
					return new Matrix3x3[size];
				case TypeCodeExt.Color:
					return new Color[size];
				case TypeCodeExt.Color32:
					return new Color32[size];
				case TypeCodeExt.Half2:
					return new Half2[size];
				case TypeCodeExt.Half3:
					return new Half3[size];
				case TypeCodeExt.Half4:
					return new Half4[size];
				case TypeCodeExt.Int2:
					return new Int2[size];
				case TypeCodeExt.Int3:
					return new Int3[size];
				case TypeCodeExt.Int4:
					return new Int4[size];
				case TypeCodeExt.BoundingBox:
					return new BoundingBox[size];
				case TypeCodeExt.BoundingFrustum:
					return new BoundingFrustum[size];
				case TypeCodeExt.BoundingSphere:
					return new BoundingSphere[size];
				case TypeCodeExt.Plane:
					return new Plane[size];
				case TypeCodeExt.Ray:
					return new Ray[size];
				case TypeCodeExt.Rectangle:
					return new Rectangle[size];
				case TypeCodeExt.Viewport:
					return new Viewport[size];
				default:
					return null;
			}
		}

		private TypeCodeExt CheckIfFlaxType(Type type)
		{
			// My eyes are bleeding
			if (type == typeof(Vector2))
				return TypeCodeExt.Vector2;
			else if (type == typeof(Vector3))
				return TypeCodeExt.Vector3;
			else if (type == typeof(Vector4))
				return TypeCodeExt.Vector4;
			else if (type == typeof(Float2))
				return TypeCodeExt.Float2;
			else if (type == typeof(Float3))
				return TypeCodeExt.Float3;
			else if (type == typeof(Float4))
				return TypeCodeExt.Float4;
			else if (type == typeof(Double2))
				return TypeCodeExt.Double2;
			else if (type == typeof(Double3))
				return TypeCodeExt.Double3;
			else if (type == typeof(Double4))
				return TypeCodeExt.Double4;
			else if (type == typeof(Half2))
				return TypeCodeExt.Half2;
			else if (type == typeof(Half3))
				return TypeCodeExt.Half3;
			else if (type == typeof(Half4))
				return TypeCodeExt.Half4;
			else if (type == typeof(Int2))
				return TypeCodeExt.Int2;
			else if (type == typeof(Int3))
				return TypeCodeExt.Int3;
			else if (type == typeof(Int4))
				return TypeCodeExt.Int4;
			else if (type == typeof(Quaternion))
				return TypeCodeExt.Quaternion;
			else if (type == typeof(Transform))
				return TypeCodeExt.Transform;
			else if (type == typeof(Matrix))
				return TypeCodeExt.Matrix;
			else if (type == typeof(Matrix2x2))
				return TypeCodeExt.Matrix2x2;
			else if (type == typeof(Matrix3x3))
				return TypeCodeExt.Matrix3x3;
			else if (type == typeof(Color))
				return TypeCodeExt.Color;
			else if (type == typeof(Color32))
				return TypeCodeExt.Color32;
			else if (type == typeof(BoundingBox))
				return TypeCodeExt.BoundingBox;
			else if (type == typeof(BoundingFrustum))
				return TypeCodeExt.BoundingFrustum;
			else if (type == typeof(BoundingSphere))
				return TypeCodeExt.BoundingSphere;
			else if (type == typeof(Plane))
				return TypeCodeExt.Plane;
			else if (type == typeof(Ray))
				return TypeCodeExt.Ray;
			else if (type == typeof(Rectangle))
				return TypeCodeExt.Rectangle;
			else if (type == typeof(Viewport))
				return TypeCodeExt.Viewport;
			return TypeCodeExt.Empty;
		}

		private float[] ReadFloatArray(byte[] buffer, int offset, int arraySize)
		{
			var array = new float[arraySize];
			var end = offset + (arraySize * 4);
			for (var i = 0; i < arraySize; i++)
				array[i] = Utility.ReadFloat(buffer, offset + (i * 4));
			return array;
		}
		#endregion

		#region ReadWrite Funcions
		private Func<byte[], int, object> GetReadFunc(TypeCodeExt typeCode) 
		{
			switch (typeCode)
			{
				// System
				case TypeCodeExt.Boolean:
					return (buffer, offset) => buffer[offset] == 1;
				case TypeCodeExt.SByte:
					return (buffer, offset) => (sbyte)buffer[offset];
				case TypeCodeExt.Byte:
					return (buffer, offset) => buffer[offset];
				case TypeCodeExt.Char:
					return (buffer, offset) => (char)buffer[offset];
				case TypeCodeExt.Int16:
					return (buffer, offset) => Utility.ReadShort(buffer, offset);
				case TypeCodeExt.UInt16:
					return (buffer, offset) => Utility.ReadUShort(buffer, offset);
				case TypeCodeExt.Int32:
					return (buffer, offset) => Utility.ReadInt(buffer, offset);
				case TypeCodeExt.UInt32:
					return (buffer, offset) => Utility.ReadUInt(buffer, offset);
				case TypeCodeExt.Int64:
					return (buffer, offset) => Utility.ReadLong(buffer, offset);
				case TypeCodeExt.UInt64:
					return (buffer, offset) => Utility.ReadULong(buffer, offset);
				case TypeCodeExt.Single:
					return (buffer, offset) => Utility.ReadFloat(buffer, offset);
				case TypeCodeExt.Double:
					return (buffer, offset) => Utility.ReadDouble(buffer, offset);
				case TypeCodeExt.Decimal:
					return (buffer, offset) => Utility.ReadDecimal(buffer, offset);
				case TypeCodeExt.DateTime:
					return (buffer, offset) => new DateTime(Utility.ReadLong(buffer, offset));

				// Flax
				case TypeCodeExt.Vector2:
					return (buffer, offset) => new Vector2(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4));
				case TypeCodeExt.Vector3:
					return (buffer, offset) => new Vector3(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4), 
						Utility.ReadFloat(buffer, offset + 8));
				case TypeCodeExt.Vector4:
					return (buffer, offset) => new Vector4(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4),
						Utility.ReadFloat(buffer, offset + 8), Utility.ReadFloat(buffer, offset + 12));
				case TypeCodeExt.Float2:
					return (buffer, offset) => new Float2(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4));
				case TypeCodeExt.Float3:
					return (buffer, offset) => new Float3(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4), 
						Utility.ReadFloat(buffer, offset + 8));
				case TypeCodeExt.Float4:
					return (buffer, offset) => new Float4(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4),
						Utility.ReadFloat(buffer, offset + 8), Utility.ReadFloat(buffer, offset + 12));
				case TypeCodeExt.Double2:
					return (buffer, offset) => new Double2(Utility.ReadDouble(buffer, offset), Utility.ReadDouble(buffer, offset + 4));
				case TypeCodeExt.Double3:
					return (buffer, offset) => new Double3(Utility.ReadDouble(buffer, offset), Utility.ReadDouble(buffer, offset + 4), 
						Utility.ReadDouble(buffer, offset + 8));
				case TypeCodeExt.Double4:
					return (buffer, offset) => new Double4(Utility.ReadDouble(buffer, offset), Utility.ReadDouble(buffer, offset + 4),
						Utility.ReadDouble(buffer, offset + 8), Utility.ReadDouble(buffer, offset + 12));
				case TypeCodeExt.Half2:
					return (buffer, offset) => new Half2(Utility.ReadShort(buffer, offset), Utility.ReadShort(buffer, offset + 4));
				case TypeCodeExt.Half3:
					return (buffer, offset) => new Half3(Utility.ReadShort(buffer, offset), Utility.ReadShort(buffer, offset + 4), 
						Utility.ReadShort(buffer, offset + 8));
				case TypeCodeExt.Half4:
					return (buffer, offset) => new Half4(new Half(Utility.ReadShort(buffer, offset)), new Half(Utility.ReadShort(buffer, offset + 4)),
						new Half(Utility.ReadShort(buffer, offset + 8)), new Half(Utility.ReadShort(buffer, offset + 12)));
				case TypeCodeExt.Int2:
					return (buffer, offset) => new Int2(Utility.ReadInt(buffer, offset), Utility.ReadInt(buffer, offset + 4));
				case TypeCodeExt.Int3:
					return (buffer, offset) => new Int3(Utility.ReadInt(buffer, offset), Utility.ReadInt(buffer, offset + 4), 
						Utility.ReadInt(buffer, offset + 8));
				case TypeCodeExt.Int4:
					return (buffer, offset) => new Int4(Utility.ReadInt(buffer, offset), Utility.ReadInt(buffer, offset + 4),
						Utility.ReadInt(buffer, offset + 8), Utility.ReadInt(buffer, offset + 12));
				case TypeCodeExt.Quaternion:
					return (buffer, offset) => new Quaternion(ReadFloatArray(buffer, offset, 4));
				case TypeCodeExt.Transform:
					return (buffer, offset) => new Transform(
						new Vector3(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4), 
							Utility.ReadFloat(buffer, offset + 8)),
						new Quaternion(ReadFloatArray(buffer, offset + 12, 4)),
						new Float3(Utility.ReadFloat(buffer, offset + 28), Utility.ReadFloat(buffer, offset + 32), 
							Utility.ReadFloat(buffer, offset + 36)));
				case TypeCodeExt.Matrix:
					return (buffer, offset) => new Matrix(ReadFloatArray(buffer, offset, 16));
				case TypeCodeExt.Matrix2x2:
					return (buffer, offset) => new Matrix2x2(ReadFloatArray(buffer, offset, 9));
				case TypeCodeExt.Matrix3x3:
					return (buffer, offset) => new Matrix3x3(ReadFloatArray(buffer, offset, 4));
				case TypeCodeExt.Color:
					return (buffer, offset) => new Color(ReadFloatArray(buffer, offset, 4));
				case TypeCodeExt.Color32:
					return (buffer, offset) => new Color32(buffer[offset], buffer[offset + 1], buffer[offset + 2], buffer[offset + 3]);
				case TypeCodeExt.BoundingBox:
					return (buffer, offset) => new BoundingBox(
						new Vector3(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4), 
							Utility.ReadFloat(buffer, offset + 8)),
						new Vector3(Utility.ReadFloat(buffer, offset + 12), Utility.ReadFloat(buffer, offset + 16), 
							Utility.ReadFloat(buffer, offset + 20)));
				case TypeCodeExt.BoundingFrustum:
					return (buffer, offset) => new BoundingFrustum(new Matrix(ReadFloatArray(buffer, offset, 16)));
				case TypeCodeExt.BoundingSphere:
					return (buffer, offset) => new BoundingSphere(
						new Vector3(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4), 
							Utility.ReadFloat(buffer, offset + 8)),
						buffer[offset + 12]);
				case TypeCodeExt.Plane:
					return (buffer, offset) => new Plane(
						new Vector3(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4), 
							Utility.ReadFloat(buffer, offset + 8)),
						buffer[offset + 12]);
				case TypeCodeExt.Ray:
					return (buffer, offset) => new Ray(
						new Vector3(Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4), 
							Utility.ReadFloat(buffer, offset + 8)),
						new Vector3(Utility.ReadFloat(buffer, offset + 12), Utility.ReadFloat(buffer, offset + 16), 
							Utility.ReadFloat(buffer, offset + 20)));
				case TypeCodeExt.Rectangle:
					return (buffer, offset) => new Rectangle(
						Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4),
						Utility.ReadFloat(buffer, offset + 8), Utility.ReadFloat(buffer, offset + 12));
				case TypeCodeExt.Viewport:
					return (buffer, offset) => new Viewport(
						Utility.ReadFloat(buffer, offset), Utility.ReadFloat(buffer, offset + 4),
						Utility.ReadFloat(buffer, offset + 8), Utility.ReadFloat(buffer, offset + 12),
						Utility.ReadFloat(buffer, offset + 16), Utility.ReadFloat(buffer, offset + 20));
				default:
					return null;
			}
		}

		private Action<byte[], int, object> GetWriteFunc(TypeCodeExt typeCode) 
		{
			switch (typeCode)
			{
				// System
				case TypeCodeExt.Boolean:
					return (buffer, offset, data) => buffer[offset] = (byte)(((bool)data) ? 1 : 0);
				case TypeCodeExt.SByte:
					return (buffer, offset, data) => buffer[offset] = (byte)(sbyte)data;
				case TypeCodeExt.Byte:
					return (buffer, offset, data) => buffer[offset] = (byte)data;
				case TypeCodeExt.Char:
					return (buffer, offset, data) => buffer[offset] = (byte)(char)data;
				case TypeCodeExt.Int16:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (short)data);
				case TypeCodeExt.UInt16:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (ushort)data);
				case TypeCodeExt.Int32:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (int)data);
				case TypeCodeExt.UInt32:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (uint)data);
				case TypeCodeExt.Int64:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (long)data);
				case TypeCodeExt.UInt64:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (ulong)data);
				case TypeCodeExt.Single:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (float)data);
				case TypeCodeExt.Double:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (double)data);
				case TypeCodeExt.Decimal:
					return (buffer, offset, data) => Utility.Write(buffer, offset, (decimal)data);
				case TypeCodeExt.DateTime:
					return (buffer, offset, data) => Utility.Write(buffer, offset, ((DateTime)data).Ticks);
				
				// Flax
				case TypeCodeExt.Vector2:
					return (buffer, offset, data) => {
						var castData = (Vector2)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
					};
				case TypeCodeExt.Vector3:
					return (buffer, offset, data) => {
						var castData = (Vector3)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
						Utility.Write(buffer, offset + 8, castData.Z);
					};
				case TypeCodeExt.Vector4:
					return (buffer, offset, data) => {
						var castData = (Vector4)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
						Utility.Write(buffer, offset + 8, castData.Z);
						Utility.Write(buffer, offset + 12, castData.W);
					};
				case TypeCodeExt.Float2:
					return (buffer, offset, data) => {
						var castData = (Float2)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
					};
				case TypeCodeExt.Float3:
					return (buffer, offset, data) => {
						var castData = (Float3)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
						Utility.Write(buffer, offset + 8, castData.Z);
					};
				case TypeCodeExt.Float4:
					return (buffer, offset, data) => {
						var castData = (Float4)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
						Utility.Write(buffer, offset + 8, castData.Z);
						Utility.Write(buffer, offset + 12, castData.W);
					};
				case TypeCodeExt.Double2:
					return (buffer, offset, data) => {
						var castData = (Double2)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 8, castData.Y);
					};
				case TypeCodeExt.Double3:
					return (buffer, offset, data) => {
						var castData = (Double3)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 8, castData.Y);
						Utility.Write(buffer, offset + 16, castData.Z);
					};
				case TypeCodeExt.Double4:
					return (buffer, offset, data) => {
						var castData = (Double4)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 8, castData.Y);
						Utility.Write(buffer, offset + 16, castData.Z);
						Utility.Write(buffer, offset + 24, castData.W);
					};
				case TypeCodeExt.Half2:
					return (buffer, offset, data) => {
						var castData = (Half2)data;
						Utility.Write(buffer, offset, castData.X.RawValue);
						Utility.Write(buffer, offset + 2, castData.Y.RawValue);
					};
				case TypeCodeExt.Half3:
					return (buffer, offset, data) => {
						var castData = (Half3)data;
						Utility.Write(buffer, offset, castData.X.RawValue);
						Utility.Write(buffer, offset + 2, castData.Y.RawValue);
						Utility.Write(buffer, offset + 4, castData.Z.RawValue);
					};
				case TypeCodeExt.Half4:
					return (buffer, offset, data) => {
						var castData = (Half4)data;
						Utility.Write(buffer, offset, castData.X.RawValue);
						Utility.Write(buffer, offset + 2, castData.Y.RawValue);
						Utility.Write(buffer, offset + 4, castData.Z.RawValue);
						Utility.Write(buffer, offset + 6, castData.W.RawValue);
					};
				case TypeCodeExt.Int2:
					return (buffer, offset, data) => {
						var castData = (Int2)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
					};
				case TypeCodeExt.Int3:
					return (buffer, offset, data) => {
						var castData = (Int3)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
						Utility.Write(buffer, offset + 8, castData.Z);
					};
				case TypeCodeExt.Int4:
					return (buffer, offset, data) => {
						var castData = (Int4)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
						Utility.Write(buffer, offset + 8, castData.Z);
						Utility.Write(buffer, offset + 12, castData.W);
					};
				case TypeCodeExt.Quaternion:
					return (buffer, offset, data) => {
						var castData = (Quaternion)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
						Utility.Write(buffer, offset + 8, castData.Z);
						Utility.Write(buffer, offset + 12, castData.W);
					};
				case TypeCodeExt.Transform:
					return (buffer, offset, data) => {
						var castData = (Transform)data;
						var func = GetWriteFunc(TypeCodeExt.Vector3);
						func(buffer, offset, castData.Translation);
						func = GetWriteFunc(TypeCodeExt.Quaternion);
						func(buffer, offset + 12, castData.Orientation);
						func = GetWriteFunc(TypeCodeExt.Float3);
						func(buffer, offset + 28, castData.Scale);
					};
				case TypeCodeExt.Matrix:
					return (buffer, offset, data) => {
						var castData = (Matrix)data;
						var func = GetWriteFunc(TypeCodeExt.Float4);
						func(buffer, offset, castData.Column1);
						func(buffer, offset + 16, castData.Column2);
						func(buffer, offset + 32, castData.Column3);
						func(buffer, offset + 48, castData.Column4);
					};
				case TypeCodeExt.Matrix2x2:
					return (buffer, offset, data) => {
						var castData = (Matrix2x2)data;
						Utility.Write(buffer, offset, castData.M11);
						Utility.Write(buffer, offset + 4, castData.M12);
						Utility.Write(buffer, offset + 8, castData.M21);
						Utility.Write(buffer, offset + 12, castData.M22);
					};
				case TypeCodeExt.Matrix3x3:
					return (buffer, offset, data) => {
						var castData = (Matrix3x3)data;
						var func = GetWriteFunc(TypeCodeExt.Float3);
						func(buffer, offset, castData.Column1);
						func(buffer, offset + 12, castData.Column2);
						func(buffer, offset + 24, castData.Column3);
					};
				case TypeCodeExt.Color:
					return (buffer, offset, data) => {
						var castData = (Color)data;
						Utility.Write(buffer, offset, castData.R);
						Utility.Write(buffer, offset + 4, castData.G);
						Utility.Write(buffer, offset + 8, castData.B);
						Utility.Write(buffer, offset + 12, castData.A);
					};
				case TypeCodeExt.Color32:
					return (buffer, offset, data) => {
						var castData = (Color32)data;
						buffer[offset] = castData.R;
						buffer[offset + 1] = castData.G;
						buffer[offset + 2] = castData.B;
						buffer[offset + 3] = castData.A;
					};
				case TypeCodeExt.BoundingBox:
					return (buffer, offset, data) => {
						var castData = (BoundingBox)data;
						Utility.Write(buffer, offset, castData.Minimum.X);
						Utility.Write(buffer, offset + 4, castData.Minimum.Y);
						Utility.Write(buffer, offset + 8, castData.Minimum.Z);
						Utility.Write(buffer, offset + 12, castData.Maximum.X);
						Utility.Write(buffer, offset + 16, castData.Maximum.Y);
						Utility.Write(buffer, offset + 20, castData.Maximum.Z);
					};
				case TypeCodeExt.BoundingFrustum:
					return (buffer, offset, data) => {
						var castData = (BoundingFrustum)data;
						var func = GetWriteFunc(TypeCodeExt.Float4);
						func(buffer, offset, castData.Matrix.Column1);
						func(buffer, offset + 16, castData.Matrix.Column2);
						func(buffer, offset + 32, castData.Matrix.Column3);
						func(buffer, offset + 48, castData.Matrix.Column4);
					};
				case TypeCodeExt.BoundingSphere:
					return (buffer, offset, data) => {
						var castData = (BoundingSphere)data;
						Utility.Write(buffer, offset, castData.Center.X);
						Utility.Write(buffer, offset + 4, castData.Center.Y);
						Utility.Write(buffer, offset + 8, castData.Center.Z);
						Utility.Write(buffer, offset + 12, castData.Radius);
					};
				case TypeCodeExt.Plane:
					return (buffer, offset, data) => {
						var castData = (Plane)data;
						Utility.Write(buffer, offset, castData.Normal.X);
						Utility.Write(buffer, offset + 4, castData.Normal.Y);
						Utility.Write(buffer, offset + 8, castData.Normal.Z);
						Utility.Write(buffer, offset + 12, castData.D);
					};
				case TypeCodeExt.Ray:
					return (buffer, offset, data) => {
						var castData = (Ray)data;
						Utility.Write(buffer, offset, castData.Position.X);
						Utility.Write(buffer, offset + 4, castData.Position.Y);
						Utility.Write(buffer, offset + 8, castData.Position.Z);
						Utility.Write(buffer, offset + 12, castData.Direction.X);
						Utility.Write(buffer, offset + 16, castData.Direction.Y);
						Utility.Write(buffer, offset + 20, castData.Direction.Z);
					};
				case TypeCodeExt.Rectangle:
					return (buffer, offset, data) => {
						var castData = (Rectangle)data;
						Utility.Write(buffer, offset, castData.Location.X);
						Utility.Write(buffer, offset + 4, castData.Location.Y);
						Utility.Write(buffer, offset + 8, castData.Size.X);
						Utility.Write(buffer, offset + 12, castData.Size.Y);
					};
				case TypeCodeExt.Viewport:
					return (buffer, offset, data) => {
						var castData = (Viewport)data;
						Utility.Write(buffer, offset, castData.X);
						Utility.Write(buffer, offset + 4, castData.Y);
						Utility.Write(buffer, offset + 8, castData.Width);
						Utility.Write(buffer, offset + 12, castData.Height);
						Utility.Write(buffer, offset + 16, castData.MinDepth);
						Utility.Write(buffer, offset + 20, castData.MaxDepth);
					};
				default:
					return null;
			}
		}

		#endregion

		/// <summary>
		/// Contains serialization methods for primitive types
		/// </summary>
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
			public static unsafe float ReadFloat(byte[] buffer, int offset) 
			{
				var tmp =  (uint)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset] << 24);
				return *((float*)&tmp);
			}
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static long ReadLong(byte[] buffer, int offset) 
				=> (long)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24
				| buffer[offset++] << 32 | buffer[offset++] << 40 | buffer[offset++] << 48 | buffer[offset] << 56);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static ulong ReadULong(byte[] buffer, int offset) 
				=> (ulong)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24
				| buffer[offset++] << 32 | buffer[offset++] << 40 | buffer[offset++] << 48 | buffer[offset] << 56);
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static unsafe double ReadDouble(byte[] buffer, int offset) 
			{
				var tmp = (ulong)(buffer[offset++] | buffer[offset++] << 8 | buffer[offset++] << 16 | buffer[offset++] << 24
				| buffer[offset++] << 32 | buffer[offset++] << 40 | buffer[offset++] << 48 | buffer[offset] << 56);
				return *((double*)&tmp);
			}
			
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

		private enum TypeCodeExt
		{
			// System
			Empty = 0,
			Object = 1,
			DBNull = 2,
			Boolean = 3,
			Char = 4,
			SByte = 5,
			Byte = 6,
			Int16 = 7,
			UInt16 = 8,
			Int32 = 9,
			UInt32 = 10,
			Int64 = 11,
			UInt64 = 12,
			Single = 13,
			Double = 14,
			Decimal = 15,
			DateTime = 16,
			String = 18,

			// Flax
			Vector2 = 0 | 0x40,
			Vector3 = 1 | 0x40,
			Vector4 = 2 | 0x40,
			Float2 = 3 | 0x40,
			Float3 = 4 | 0x40,
			Float4 = 5 | 0x40,
			Double2 = 6 | 0x40,
			Double3 = 7 | 0x40,
			Double4 = 8 | 0x40,
			Half2 = 9 | 0x40,
			Half3 = 10 | 0x40,
			Half4 = 11 | 0x40,
			Int2 = 12 | 0x40,
			Int3 = 13 | 0x40,
			Int4 = 14 | 0x40,
			Quaternion = 15 | 0x40,
			Transform = 16 | 0x40,
			Matrix = 17 | 0x40,
			Matrix2x2 = 18 | 0x40,
			Matrix3x3 = 19 | 0x40,
			Color = 20 | 0x40,
			Color32 = 21 | 0x40,
			// ColorHSV = 22 | 0x40,
			BoundingBox = 23 | 0x40,
			BoundingFrustum = 24 | 0x40,
			BoundingSphere = 25 | 0x40,
			// OrientedBoundingBox = 26 | 0x40,
			Plane = 27 | 0x40,
			Ray = 28 | 0x40,
			Rectangle = 29 | 0x40,
			Viewport = 30 | 0x40,
		}
	}
}