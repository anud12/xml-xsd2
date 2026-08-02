using Godot;

namespace NewGameProject.Module;

static class SpriteMapCpu
{
    public static Godot.Image? ComposeSpriteMap(byte[] tiffData, Godot.Image[] skins)
    {
        try
        {
            bool littleEndian = tiffData[0] == 0x49;
            var result = ParseTiff(tiffData, littleEndian);
            if (result == null)
            {
                GD.PrintErr("SpriteMapCpu: failed to parse TIFF");
                return null;
            }

            (int w, int h, ushort[,] rMap, ushort[,] gMap, byte[,] bMap) = result.Value;

            var composed = Godot.Image.CreateEmpty(w, h, false, Godot.Image.Format.Rgba8);
            byte[] composedData = new byte[w * h * 4];

            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    int pixelIdx = py * w + px;
                    float u = w > 1 ? rMap[py, px] / (float)(w - 1) : 0f;
                    float v = h > 1 ? gMap[py, px] / (float)(h - 1) : 0f;
                    int bVal = bMap[py, px];

                    int idx = pixelIdx * 4;
                    int skinIdxVal = System.Math.Clamp(bVal, 0, skins.Length - 1);

                    var skin = skins[skinIdxVal];
                    if (skin == null)
                    {
                        composedData[idx] = 255;
                        composedData[idx + 1] = 255;
                        composedData[idx + 2] = 255;
                        composedData[idx + 3] = 0;
                        continue;
                    }

                    int sw = skin.GetWidth();
                    int sh = skin.GetHeight();
                    if (sw == 0 || sh == 0) continue;

                    int sx = System.Math.Clamp((int)(u * (sw - 1)), 0, sw - 1);
                    int sy = System.Math.Clamp((int)(v * (sh - 1)), 0, sh - 1);

                    byte[] sData = skin.GetData();
                    if (sData == null)
                    {
                        composedData[idx] = 255;
                        composedData[idx + 1] = 255;
                        composedData[idx + 2] = 255;
                        composedData[idx + 3] = 0;
                        continue;
                    }

                    int sIdx = (sy * sw + sx) * 4;
                    composedData[idx] = sData[sIdx];
                    composedData[idx + 1] = sData[sIdx + 1];
                    composedData[idx + 2] = sData[sIdx + 2];
                    composedData[idx + 3] = sData[sIdx + 3];
                }
            }

            composed.SetData(w, h, false, Godot.Image.Format.Rgba8, composedData);
            return composed;
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"SpriteMapCpu: composition failed: {ex.Message}");
            return null;
        }
    }

    static (int w, int h, ushort[,] rMap, ushort[,] gMap, byte[,] bMap)? ParseTiff(byte[] data, bool le)
    {
        int ifdOffset = ReadI32(data, 4, le);
        int numEntries = ReadI16(data, ifdOffset, le);

        int w = 0, h = 0, samplesPerPixel = 3, bitsPerSample = 0, compression = 0;
        int stripOffsets = 0;

        for (int i = 0; i < numEntries; i++)
        {
            int eOff = ifdOffset + 2 + i * 12;
            int tag = ReadI16(data, eOff, le);
            int type = ReadI16(data, eOff + 2, le);
            int count = ReadI32(data, eOff + 4, le);
            int valOrOff = ReadI32(data, eOff + 8, le);

            switch (tag)
            {
                case 0x0100: w = valOrOff; break;
                case 0x0101: h = valOrOff; break;
                case 0x0102:
                    if (count <= 2)
                        bitsPerSample = ReadU16(data, eOff + 8, le);
                    else
                        bitsPerSample = ReadU16(data, valOrOff, le);
                    break;
                case 0x0103: compression = valOrOff; break;
                case 0x0115: samplesPerPixel = valOrOff; break;
                case 0x0111:
                    if (count > 1)
                        stripOffsets = ReadI32(data, valOrOff, le);
                    else
                        stripOffsets = valOrOff;
                    break;
            }
        }

        if (compression != 1)
        {
            GD.PrintErr($"SpriteMapCpu: unsupported compression={compression}");
            return null;
        }

        int bytesPerPixel = samplesPerPixel * bitsPerSample / 8;
        int bytesPerRow = w * bytesPerPixel;

        ushort[,] rMap = new ushort[h, w];
        ushort[,] gMap = new ushort[h, w];
        byte[,] bMap = new byte[h, w];

        for (int py = 0; py < h; py++)
        {
            int rowOffset = stripOffsets + py * bytesPerRow;
            for (int px = 0; px < w; px++)
            {
                int pOff = rowOffset + px * bytesPerPixel;

                ushort r = ReadU16(data, pOff, le);
                ushort g = ReadU16(data, pOff + 2, le);
                ushort b = ReadU16(data, pOff + 4, le);

                rMap[py, px] = r;
                gMap[py, px] = g;
                bMap[py, px] = (byte)(b >> 8);
            }
        }

        return (w, h, rMap, gMap, bMap);
    }

    static ushort ReadU16(byte[] d, int o, bool le)
    {
        return le ? (ushort)(d[o] | (d[o + 1] << 8)) : (ushort)((d[o] << 8) | d[o + 1]);
    }

    static int ReadI16(byte[] d, int o, bool le)
    {
        return le ? d[o] | (d[o + 1] << 8) : (d[o] << 8) | d[o + 1];
    }

    static int ReadI32(byte[] d, int o, bool le)
    {
        return le ? d[o] | (d[o + 1] << 8) | (d[o + 2] << 16) | (d[o + 3] << 24)
                  : (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3];
    }
}
