﻿namespace dotnes.tests;

public class NESWriterTests
{
    readonly byte[] data;
    readonly MemoryStream stream = new MemoryStream();

    public NESWriterTests()
    {
        using var s = GetType().Assembly.GetManifestResourceStream("dotnes.tests.Data.hello.nes");
        if (s == null)
            throw new Exception("Cannot load hello.nes!");
        data = new byte[s.Length];
        s.Read(data, 0, data.Length);
    }

    NESWriter GetWriter(byte[]? PRG_ROM = null, byte[]? CHR_ROM = null)
    {
        stream.SetLength(0);

        return new NESWriter(stream, leaveOpen: true)
        {
            PRG_ROM = PRG_ROM,
            CHR_ROM = CHR_ROM,
        };
    }

    void AssertInstructions(string assembly)
    {
        Assert.Equal(toByteArray(assembly.Replace(" ", "")), stream.ToArray());

        static byte[] toByteArray(string text)
        {
            int length = text.Length >> 1;
            var bytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes[i] = (byte)((toHex(text[i << 1]) << 4) + (toHex(text[(i << 1) + 1])));
            }
            return bytes;
        }

        static int toHex(char ch)
        {
            int value = (int)ch;
            return value - (value < 58 ? 48 : (value < 97 ? 55 : 87));
        }
    }

    [Fact]
    public void WriteHeader()
    {
        using var writer = GetWriter(new byte[2 * 16384], new byte[1 * 8192]);
        writer.WriteHeader();
        writer.Flush();

        var actual = stream.ToArray();
        for (int i = 0; i < 16; i++)
        {
            Assert.Equal(data[i], actual[i]);
        }
    }

    [Fact]
    public void WriteFullROM()
    {
        using var writer = GetWriter(new byte[2 * 16384], new byte[1 * 8192]);

        Array.Copy(data, 16, writer.PRG_ROM!, 0, 2 * 16384);
        Array.Copy(data, 16 + 2 * 16384, writer.CHR_ROM!, 0, 8192);

        writer.Write();
        writer.Flush();

        Assert.Equal(data, stream.ToArray());
    }

    [Fact]
    public void WriteLDA()
    {
        using var r = GetWriter();
        r.LDA(0x08);
        r.Flush();

        // 8076	A908          	LDA #$08  
        Assert.Equal(new byte[] { 0xA9, 0x08 }, stream.ToArray());
    }

    [Fact]
    public void WriteJSR()
    {
        using var r = GetWriter();
        r.JSR(0x84F4);
        r.Flush();

        // 807A	20F484        	JSR initlib
        Assert.Equal(new byte[] { 0x20, 0xF4, 0x84 }, stream.ToArray());
    }

    [Fact]
    public void Write_pal_all()
    {
        using var r = GetWriter();
        r.WriteBuiltIn(nameof(NESLib.pal_all));
        r.Flush();
        AssertInstructions("8517 8618 A200 A920");
    }
}