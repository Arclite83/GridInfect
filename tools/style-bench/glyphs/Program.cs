using System;
using System.IO;
using System.IO.Compression;
using GridInfect.Core;
using GridInfect.Game;
using UnityEngine;

static class Program
{
    // Sheet: every glyph the game can draw, 8 per row, on the well colour.
    static void Main(string[] args)
    {
        int px = int.Parse(args.Length > 0 ? args[0] : "128");
        string outPath = args.Length > 1 ? args[1] : "sheet.png";
        var p = BoardPalette.Default;
        if (args.Length > 2) BoardPalette.Skins.Apply(p, Enum.Parse<BoardPalette.SkinId>(args[2]));

        var specs = new System.Collections.Generic.List<(string, Sprite)>();
        // The 15 orthogonal sets, canonical order like gen-assets: subsets of N E S W
        Dir[] orth = { Dir.U, Dir.R, Dir.D, Dir.L };
        for (int m = 1; m < 16; m++)
        {
            byte arms = 0; string name = "";
            for (int i = 0; i < 4; i++) if ((m & (1 << i)) != 0) { arms |= (byte)(1 << (int)orth[i]); name += "NESW"[i]; }
            specs.Add(("bug_" + name, BugGlyph.Piece(new PieceSpec(arms), p, px)));
        }
        Dir[] diag = { Dir.UR, Dir.DR, Dir.DL, Dir.UL };
        for (int m = 1; m < 16; m++)
        {
            byte arms = 0; string name = "";
            for (int i = 0; i < 4; i++) if ((m & (1 << i)) != 0) { arms |= (byte)(1 << (int)diag[i]); name += new[] { "NE", "SE", "SW", "NW" }[i]; }
            specs.Add(("bug_" + name, BugGlyph.Piece(new PieceSpec(arms), p, px)));
        }
        specs.Add(("bug_N_SE", BugGlyph.Piece(new PieceSpec((byte)((1 << (int)Dir.U) | (1 << (int)Dir.DR))), p, px)));
        specs.Add(("bug_NS_NESW", BugGlyph.Piece(new PieceSpec((byte)((1 << (int)Dir.U) | (1 << (int)Dir.D) | (1 << (int)Dir.UR) | (1 << (int)Dir.DL))), p, px)));
        specs.Add(("bug_AREA", BugGlyph.Piece(new PieceSpec(0, 0, true), p, px)));
        specs.Add(("bug_N_reach1", BugGlyph.Piece(new PieceSpec((byte)(1 << (int)Dir.U)).WithReach(Dir.U, 1), p, px)));
        specs.Add(("bug_NE_reach2", BugGlyph.Piece(new PieceSpec((byte)((1 << (int)Dir.U) | (1 << (int)Dir.R))).WithReach(Dir.R, 2), p, px)));
        specs.Add(("tile_BLOCKER", BugGlyph.Blocker(p, px)));
        specs.Add(("mark_LOCK", BugGlyph.Lock(p, px)));
        specs.Add(("relay_NESW", BugGlyph.Relay((byte)((1 << (int)Dir.U) | (1 << (int)Dir.R) | (1 << (int)Dir.D) | (1 << (int)Dir.L)), p, px)));

        int cols = 8, rows = (specs.Count + cols - 1) / cols;
        int pitch = px + 8;
        int W = cols * pitch, H = rows * pitch;
        var img = new byte[W * H * 4];
        // background: well over mask ≈ dark green, or transparent for overlays
        bool alpha = args.Length > 3 && args[3] == "alpha";
        if (!alpha) for (int i = 0; i < W * H; i++) { img[i * 4] = 60; img[i * 4 + 1] = 90; img[i * 4 + 2] = 50; img[i * 4 + 3] = 255; }
        for (int n = 0; n < specs.Count; n++)
        {
            var tex = specs[n].Item2.Tex;
            int ox = (n % cols) * pitch + 4, oy = (n / cols) * pitch + 4;
            for (int y = 0; y < tex.H; y++)
                for (int x = 0; x < tex.W; x++)
                {
                    var c = tex.Pixels[(tex.H - 1 - y) * tex.W + x];   // texture row 0 = bottom
                    int idx = ((oy + y) * W + ox + x) * 4;
                    float a = c.a;
                    img[idx] = (byte)(c.r * 255 * a + img[idx] * (1 - a));
                    img[idx + 1] = (byte)(c.g * 255 * a + img[idx + 1] * (1 - a));
                    img[idx + 2] = (byte)(c.b * 255 * a + img[idx + 2] * (1 - a));
                    if (alpha) img[idx + 3] = (byte)(a * 255 + img[idx + 3] * (1 - a));
                }
        }
        File.WriteAllBytes(outPath, Png(img, W, H));
        Console.WriteLine($"{specs.Count} glyphs -> {outPath} ({W}x{H})");
    }

    static byte[] Png(byte[] rgba, int w, int h)
    {
        var raw = new MemoryStream();
        for (int y = 0; y < h; y++) { raw.WriteByte(0); raw.Write(rgba, y * w * 4, w * 4); }
        var z = new MemoryStream();
        z.WriteByte(0x78); z.WriteByte(0x9C);
        using (var d = new DeflateStream(z, CompressionLevel.Optimal, true)) d.Write(raw.ToArray(), 0, (int)raw.Length);
        uint adler = Adler(raw.ToArray()); z.Write(new[] { (byte)(adler >> 24), (byte)(adler >> 16), (byte)(adler >> 8), (byte)adler }, 0, 4);
        var o = new MemoryStream();
        o.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, 0, 8);
        Chunk(o, "IHDR", new byte[] { (byte)(w >> 24), (byte)(w >> 16), (byte)(w >> 8), (byte)w, (byte)(h >> 24), (byte)(h >> 16), (byte)(h >> 8), (byte)h, 8, 6, 0, 0, 0 });
        Chunk(o, "IDAT", z.ToArray()); Chunk(o, "IEND", new byte[0]);
        return o.ToArray();
    }
    static void Chunk(Stream o, string type, byte[] data)
    {
        var t = System.Text.Encoding.ASCII.GetBytes(type);
        o.Write(new[] { (byte)(data.Length >> 24), (byte)(data.Length >> 16), (byte)(data.Length >> 8), (byte)data.Length }, 0, 4);
        o.Write(t, 0, 4); o.Write(data, 0, data.Length);
        var crcData = new byte[4 + data.Length]; Array.Copy(t, crcData, 4); Array.Copy(data, 0, crcData, 4, data.Length);
        uint c = Crc(crcData); o.Write(new[] { (byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)c }, 0, 4);
    }
    static uint Adler(byte[] d) { uint a = 1, b = 0; foreach (var x in d) { a = (a + x) % 65521; b = (b + a) % 65521; } return (b << 16) | a; }
    static uint[] _crc;
    static uint Crc(byte[] d)
    {
        if (_crc == null) { _crc = new uint[256]; for (uint n = 0; n < 256; n++) { uint c = n; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; _crc[n] = c; } }
        uint crc = 0xFFFFFFFF; foreach (var x in d) crc = _crc[(crc ^ x) & 0xFF] ^ (crc >> 8); return crc ^ 0xFFFFFFFF;
    }
}
