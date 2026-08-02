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

            (int w, int h, ushort[,] rMap, ushort[,] gMap, byte[,] bMap, byte[,] aMap) = result.Value;

            var composed = Godot.Image.CreateEmpty(w, h, false, Godot.Image.Format.Rgba8);
            byte[] composedData = new byte[w * h * 4];

            // Multi-layer compositing: each layer draws across the full map and is alpha-blended.
            // The TIFF A channel modulates each layer's alpha before blending.
            for (int layer = 0; layer < skins.Length; layer++)
            {
                var skin = skins[layer];
                if (skin == null) continue;

                int sw = skin.GetWidth();
                int sh = skin.GetHeight();
                if (sw == 0 || sh == 0) continue;

                byte[] sData = skin.GetData();
                if (sData == null) continue;

                for (int py = 0; py < h; py++)
                {
                    for (int px = 0; px < w; px++)
                    {
                        float u = w > 1 ? rMap[py, px] / (float)(w - 1) : 0f;
                        float v = h > 1 ? gMap[py, px] / (float)(h - 1) : 0f;

                        int sx = System.Math.Clamp((int)(u * (sw - 1)), 0, sw - 1);
                        int sy = System.Math.Clamp((int)(v * (sh - 1)), 0, sh - 1);

                        int sIdx = (sy * sw + sx) * 4;
                        byte sr = sData[sIdx];
                        byte sg = sData[sIdx + 1];
                        byte sb = sData[sIdx + 2];
                        byte sa = sData[sIdx + 3];

                        // Modulate skin alpha by TIFF mask alpha
                        byte maskA = aMap[py, px];
                        float maskAlpha = maskA / 255f;
                        float effectiveAlpha = (sa / 255f) * maskAlpha;

                        int idx = (py * w + px) * 4;
                        byte dr = composedData[idx];
                        byte dg = composedData[idx + 1];
                        byte db = composedData[idx + 2];
                        byte da = composedData[idx + 3];

                        // Alpha blend: src over dst
                        float as_ = effectiveAlpha;
                        float ad = da / 255f;
                        float aout = as_ + ad * (1 - as_);

                        if (aout > 0)
                        {
                            float aoutInv = 1f / aout;
                            composedData[idx]     = (byte)System.Math.Clamp(((sr * as_ + dr * ad * (1 - as_)) * aoutInv), 0, 255);
                            composedData[idx + 1] = (byte)System.Math.Clamp(((sg * as_ + dg * ad * (1 - as_)) * aoutInv), 0, 255);
                            composedData[idx + 2] = (byte)System.Math.Clamp(((sb * as_ + db * ad * (1 - as_)) * aoutInv), 0, 255);
                            composedData[idx + 3] = (byte)System.Math.Clamp(aout * 255, 0, 255);
                        }
                    }
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

    static (int w, int h, ushort[,] rMap, ushort[,] gMap, byte[,] bMap, byte[,] aMap)? ParseTiff(byte[] data, bool le)
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
        byte[,] aMap = new byte[h, w];

        for (int py = 0; py < h; py++)
        {
            int rowOffset = stripOffsets + py * bytesPerRow;
            for (int px = 0; px < w; px++)
            {
                int pOff = rowOffset + px * bytesPerPixel;

                ushort r = ReadU16(data, pOff, le);
                ushort g = ReadU16(data, pOff + 2, le);
                ushort b = ReadU16(data, pOff + 4, le);
                ushort a = ReadU16(data, pOff + 6, le);

                rMap[py, px] = r;
                gMap[py, px] = g;
                bMap[py, px] = (byte)(b >> 8);
                aMap[py, px] = (byte)(a >> 8);
            }
        }

        return (w, h, rMap, gMap, bMap, aMap);
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
