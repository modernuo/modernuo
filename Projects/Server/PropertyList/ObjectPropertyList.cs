#nullable enable
using System;
using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Server.Buffers;
using Server.Network;
using Server.Text;

namespace Server;

public sealed class ObjectPropertyList : IPropertyList, IDisposable
{
    // Each of these are localized to "~1_NOTHING~" which allows the string argument to be used
    private static readonly int[] _stringNumbers =
    {
        1042971,
        1070722,
        1114057, // ~1_val~
        1114778, // ~1_val~
        1114779 // ~1_val~
    };

    private int _hash;
    private int _stringNumbersIndex;
    private byte[] _buffer;
    private int _bufferPos;

    // For string interpolation
    private int _pos;
    private char[]? _arrayToReturnToPool;

    public ObjectPropertyList(IEntity e)
    {
        Entity = e;
        _buffer = GC.AllocateUninitializedArray<byte>(64);

        var writer = new SpanWriter(_buffer);
        writer.Write((byte)0xD6); // Packet ID
        writer.Seek(2, SeekOrigin.Current);
        writer.Write((ushort)1);
        writer.Write(e.Serial);
        writer.Write((ushort)0);
        _bufferPos = writer.Position + 4; // Hash
    }

    public IEntity Entity { get; }

    public int Hash => 0x40000000 + _hash;

    public int Header { get; set; }

    public string HeaderArgs { get; set; }

    public static bool Enabled { get; set; }

    public byte[] Buffer => _buffer;

    public void Reset()
    {
        _bufferPos = 15;
        _hash = 0;
        _stringNumbersIndex = 0;
        Header = 0;
        HeaderArgs = null;
        STArrayPool<char>.Shared.Return(_arrayToReturnToPool);
        _pos = 0;
    }

    private void Flush()
    {
        Resize(_buffer.Length * 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Resize(int amount)
    {
        var newBuffer = GC.AllocateUninitializedArray<byte>(amount);
        _buffer.AsSpan(0, Math.Min(amount, _buffer.Length)).CopyTo(newBuffer);
        _buffer = newBuffer;
    }

    public void Terminate()
    {
        int length = _bufferPos + 4;
        if (length != _buffer.Length)
        {
            Resize(length);
        }

        var writer = new SpanWriter(_buffer);
        writer.Seek(_bufferPos, SeekOrigin.Begin);
        writer.Write(0);

        writer.Seek(11, SeekOrigin.Begin);
        writer.Write(_hash);
        writer.WritePacketLength();
    }

    private void AddHash(int val)
    {
        _hash ^= val & 0x3FFFFFF;
        _hash ^= (val >> 26) & 0x3F;
    }

    public void Add(int number, string? arguments = null)
    {
        if (number == 0)
        {
            return;
        }

        arguments ??= "";

        if (Header == 0)
        {
            Header = number;
            HeaderArgs = arguments;
        }

        AddHash(number);
        if (arguments.Length > 0)
        {
            AddHash(arguments.GetHashCode(StringComparison.Ordinal));
        }

        int strLength = arguments.Length * 2;
        int length = _bufferPos + 6 + strLength;
        while (length > _buffer.Length)
        {
            Flush();
        }

        var writer = new SpanWriter(_buffer.AsSpan(_bufferPos));
        writer.Write(number);
        writer.Write((ushort)strLength);
        writer.WriteLittleUni(arguments);

        _bufferPos += writer.BytesWritten;
        _pos = 0;
    }

    // public void Add(int number, string format, object arg0)
    // {
    //     Add(number, string.Format(format, arg0));
    // }
    //
    // public void Add(int number, string format, object arg0, object arg1)
    // {
    //     Add(number, string.Format(format, arg0, arg1));
    // }
    //
    // public void Add(int number, string format, object arg0, object arg1, object arg2)
    // {
    //     Add(number, string.Format(format, arg0, arg1, arg2));
    // }
    //
    // public void Add(int number, string format, params object[] args)
    // {
    //     Add(number, string.Format(format, args));
    // }

    private int GetStringNumber() => _stringNumbers[_stringNumbersIndex++ % _stringNumbers.Length];

    public void Add(string text) => Add(GetStringNumber(), text);

    // public void Add(string format, string arg0) => Add(GetStringNumber(), string.Format(format, arg0));
    //
    // public void Add(string format, string arg0, string arg1) =>
    //     Add(GetStringNumber(), string.Format(format, arg0, arg1));
    //
    // public void Add(string format, string arg0, string arg1, string arg2) =>
    //     Add(GetStringNumber(), string.Format(format, arg0, arg1, arg2));
    //
    // public void Add(string format, params object[] args) => Add(GetStringNumber(), string.Format(format, args));

    // String Interpolation
    public void Add(
        int number,
        ref IPropertyList.PropertyListInterpolatedStringHandler handler)
    {
        if (number == 0)
        {
            return;
        }

        var chars = _arrayToReturnToPool.AsSpan(0, _pos);

        if (Header == 0)
        {
            Header = number;
            HeaderArgs = chars.ToString();
            HeaderArgs.GetHashCode(StringComparison.Ordinal);
        }

        AddHash(number);
        if (chars.Length > 0)
        {
            AddHash(string.GetHashCode(chars, StringComparison.Ordinal));
        }

        int strLength = chars.Length * 2;
        int length = _bufferPos + 6 + strLength;
        while (length > _buffer.Length)
        {
            Flush();
        }

        var writer = new SpanWriter(_buffer.AsSpan(_bufferPos));
        writer.Write(number);
        writer.Write((ushort)strLength);
        writer.Write(chars, TextEncoding.UnicodeLE);

        _bufferPos += writer.BytesWritten;
    }

    public void InitializePropertyListInterpolation(int literalLength, int formattedCount)
    {
        _arrayToReturnToPool ??= STArrayPool<char>.Shared.Rent(GetDefaultLength(literalLength, formattedCount));
        _pos = 0;
    }

    // Copied from RawInterpolatedStringHandler

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetDefaultLength(int literalLength, int formattedCount) =>
        Math.Max(256, literalLength + formattedCount * 11);

    public void AppendLiteral(string value)
    {
        if (value.Length == 1)
        {
            Span<char> chars = _arrayToReturnToPool.AsSpan();
            int pos = _pos;
            if ((uint)pos < (uint)chars.Length)
            {
                chars[pos] = value[0];
                _pos = pos + 1;
            }
            else
            {
                GrowThenCopyString(value);
            }
            return;
        }

        AppendStringDirect(value);
    }

    private void AppendStringDirect(string value)
    {
        if (value.TryCopyTo(_arrayToReturnToPool.AsSpan(_pos..)))
        {
            _pos += value.Length;
        }
        else
        {
            GrowThenCopyString(value);
        }
    }

    public void AppendFormatted<T>(T value)
    {

        string? s;
        if (value is IFormattable)
        {
            if (value is ISpanFormattable)
            {
                int charsWritten;
                while (!((ISpanFormattable)value).TryFormat(_arrayToReturnToPool.AsSpan(_pos..), out charsWritten, default, null))
                {
                    Grow();
                }

                _pos += charsWritten;
                return;
            }

            s = ((IFormattable)value).ToString(format: null, null);
        }
        else
        {
            s = value?.ToString();
        }

        if (s is not null)
        {
            AppendStringDirect(s);
        }
    }

    public void AppendFormatted<T>(T value, string? format)
    {
        string? s;
        if (value is IFormattable)
        {
            if (value is ISpanFormattable)
            {
                int charsWritten;
                while (!((ISpanFormattable)value).TryFormat(_arrayToReturnToPool.AsSpan(_pos..), out charsWritten, format, null))
                {
                    Grow();
                }

                _pos += charsWritten;
                return;
            }

            s = ((IFormattable)value).ToString(format, null);
        }
        else
        {
            s = value?.ToString();
        }

        if (s is not null)
        {
            AppendStringDirect(s);
        }
    }

    public void AppendFormatted<T>(T value, int alignment)
    {
        int startingPos = _pos;
        AppendFormatted(value);
        if (alignment != 0)
        {
            AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
        }
    }

    public void AppendFormatted<T>(T value, int alignment, string? format)
    {
        int startingPos = _pos;
        AppendFormatted(value, format);
        if (alignment != 0)
        {
            AppendOrInsertAlignmentIfNeeded(startingPos, alignment);
        }
    }

    public void AppendFormatted(ReadOnlySpan<char> value)
    {
        if (value.TryCopyTo(_arrayToReturnToPool.AsSpan(_pos..)))
        {
            _pos += value.Length;
        }
        else
        {
            GrowThenCopySpan(value);
        }
    }

    public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null)
    {
        bool leftAlign = false;
        if (alignment < 0)
        {
            leftAlign = true;
            alignment = -alignment;
        }

        int paddingRequired = alignment - value.Length;
        if (paddingRequired <= 0)
        {
            AppendFormatted(value);
            return;
        }

        EnsureCapacityForAdditionalChars(value.Length + paddingRequired);
        var chars = _arrayToReturnToPool.AsSpan();
        if (leftAlign)
        {
            value.CopyTo(chars[_pos..]);
            _pos += value.Length;
            chars.Slice(_pos, paddingRequired).Fill(' ');
            _pos += paddingRequired;
        }
        else
        {
            chars.Slice(_pos, paddingRequired).Fill(' ');
            _pos += paddingRequired;
            value.CopyTo(chars[_pos..]);
            _pos += value.Length;
        }
    }

    public void AppendFormatted(string? value)
    {
        if (value?.TryCopyTo(_arrayToReturnToPool.AsSpan(_pos..)) == true)
        {
            _pos += value.Length;
        }
        else
        {
            AppendFormattedSlow(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void AppendFormattedSlow(string? value)
    {
        if (value is not null)
        {
            EnsureCapacityForAdditionalChars(value.Length);
            value.CopyTo(_arrayToReturnToPool.AsSpan(_pos..));
            _pos += value.Length;
        }
    }

    public void AppendFormatted(string? value, int alignment = 0, string? format = null) =>
        AppendFormatted<string?>(value, alignment, format);

    public void AppendFormatted(object? value, int alignment = 0, string? format = null) =>
        AppendFormatted<object?>(value, alignment, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool HasCustomFormatter(IFormatProvider provider) =>
        provider.GetType() != typeof(CultureInfo) &&
        provider.GetFormat(typeof(ICustomFormatter)) != null;

    private void AppendOrInsertAlignmentIfNeeded(int startingPos, int alignment)
    {
        Debug.Assert(startingPos >= 0 && startingPos <= _pos);
        Debug.Assert(alignment != 0);

        int charsWritten = _pos - startingPos;

        bool leftAlign = false;
        if (alignment < 0)
        {
            leftAlign = true;
            alignment = -alignment;
        }

        int paddingNeeded = alignment - charsWritten;
        if (paddingNeeded > 0)
        {
            EnsureCapacityForAdditionalChars(paddingNeeded);

            var chars = _arrayToReturnToPool.AsSpan();
            if (leftAlign)
            {
                chars.Slice(_pos, paddingNeeded).Fill(' ');
            }
            else
            {
                chars.Slice(startingPos, charsWritten).CopyTo(chars[(startingPos + paddingNeeded)..]);
                chars.Slice(startingPos, paddingNeeded).Fill(' ');
            }

            _pos += paddingNeeded;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacityForAdditionalChars(int additionalChars)
    {
        if (_arrayToReturnToPool.Length - _pos < additionalChars)
        {
            Grow(additionalChars);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopyString(string value)
    {
        Grow(value.Length);
        value.CopyTo(_arrayToReturnToPool.AsSpan(_pos..));
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowThenCopySpan(ReadOnlySpan<char> value)
    {
        Grow(value.Length);
        value.CopyTo(_arrayToReturnToPool.AsSpan(_pos..));
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int additionalChars)
    {
        Debug.Assert(additionalChars > _arrayToReturnToPool.Length - _pos);
        GrowCore((uint)_pos + (uint)additionalChars);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow()
    {
        GrowCore((uint)_arrayToReturnToPool.Length + 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GrowCore(uint requiredMinCapacity)
    {
        uint newCapacity = Math.Max(requiredMinCapacity, Math.Min((uint)_arrayToReturnToPool.Length * 2, 0x3FFFFFDF));
        int arraySize = (int)Math.Clamp(newCapacity, 256, int.MaxValue);

        char[] newArray = STArrayPool<char>.Shared.Rent(arraySize);
        _arrayToReturnToPool.AsSpan(.._pos).CopyTo(newArray);

        char[] toReturn = _arrayToReturnToPool;
        _arrayToReturnToPool = newArray;

        STArrayPool<char>.Shared.Return(toReturn);
    }


    public void Dispose()
    {
        STArrayPool<char>.Shared.Return(_arrayToReturnToPool);
        _arrayToReturnToPool = null;
    }

    ~ObjectPropertyList()
    {
        STArrayPool<char>.Shared.Return(_arrayToReturnToPool);
        _arrayToReturnToPool = null;
    }
}