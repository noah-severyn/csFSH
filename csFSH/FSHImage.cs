using csDBPF.Entries;
using static csDBPF.Entries.DBPFEntryFSH;

//based on FSHLib
namespace csFSH {
    public class FSHImage {
        //Explanation: https://www.fsdeveloper.com/wiki/index.php?title=DXT_compression_explained
        //Wikipedia  : https://en.wikipedia.org/wiki/S3_Texture_Compression



        // https://github.com/nominom/bcnencoder.net
        // https://github.com/BraveSirAndrew/ManagedSquish/blob/master/ManagedSquish/Squish.cs
        // https://sourceforge.net/projects/libsquish/



        // https://github.com/castano/nvidia-texture-tools/tree/master/src/nvimage
        //(Javascript) https://github.com/kchapelier/decode-dxt
        //! https://github.com/mafaca/Dxt/blob/master/Dxt/DxtDecoder.cs
        //! https://github.com/MaxxWyndham/LibSquishNet/blob/master/LibSquishNet/Squish.cs
        //! https://github.com/vpenades/TextureSquish/blob/master/Sources/Epsylon.TextureSquish/Bitmap.cs


        private List<BitmapItem> _images;



        public FSHImage(DBPFEntryFSH fshEntry) {
            _images = new List<BitmapItem>();
            for (int idx = 0; idx < 1; idx++) {
                
            }
        }

        public FSHImage() {

        }

        public FSHImage(Stream stream) {

        }



        


        /// <summary>
        /// Represents a 4x4 pixel block of a bitmap.
        /// </summary>
        private class Block {
            /// <summary>
            /// Stores 16x <see cref="Argb32"/> pixels making up a 4x4 block, numbered left to right, top to bottom. 
            /// </summary>
            public Argb32[] Pixels = new Argb32[16];
        }


        public static Argb32[] DXTDecompress(int width, int height, byte[] blob, FSHBitmapType bitmapType) {
            //partially based off of: https://github.com/Easimer/cs-dxt1/blob/master/DXT1Decompressor.cs
            //DXT1 aka Block Compression 1 aka BC1: Compressed block is 64bits. 16 bit for each color0 and color1. 32bits left: 16x 2-bit identifiers of each color
            //DXT2 aka Block Compression 2 aka BC2: Compressed block is 128bit. Contains 2x DXT1 blocks. First is alpha channel data, second is color data.

            int bytes = width * 3 * 4 * height;

            Argb32[] decompressed = new Argb32[bytes];
            byte[] byteBlock = new byte[8];

            //row and col represent the position of a 4x4 block in the greater image
            for (int row = 0; row < height / 4; row++) {
                for (int col = 0; col < width / 4; col++) {
                    Array.Copy(blob, row * 4 + col, byteBlock, 0, 8);
                    Block block = DecompressBlock(byteBlock, bitmapType);

                    Array.Copy(decompressed, row * 4 + col, block.Pixels, 0, 16);

                    //Image<Argb32> output2 = Image.LoadPixelData<Argb32>(pixels, width, height);
                }
            }
            return decompressed;
        }


        /// <summary>
        /// Decompress a 64 bit block of data into a block of pixels.
        /// </summary>
        /// <param name="blob">Raw data to process</param>
        /// <param name="bitmapType">Compression type of the blob: DXT1 or DXT3</param>
        /// <returns>A decompressed <see cref="Block"/> of pixels</returns>
        /// <exception cref="ArgumentException">If block is incorrect length</exception>
        /// /// <exception cref="ArgumentException">Bitmap type is not DXT1 or DXT3</exception>
        private static Block DecompressBlock(byte[] blob, FSHBitmapType bitmapType) {
            if (blob.Length != 8) {
                throw new ArgumentException("Block must be 64 bits (8 bytes) in length.");
            }
            if (bitmapType > FSHBitmapType.DXT3) {
                throw new ArgumentException("Only valid for DXT1 and DXT3 bitmap types.");
            }

            //Read color0 and color1 straight from the block. Colors stored in R5 G6 B5 format.
            ushort c0 = (ushort) (blob[1] << 8 | blob[0]);
            ushort c1 = (ushort) (blob[3] << 8 | blob[2]);
            byte c0_r = (byte) ((c0 & 0b1111100000000000) >> 11);
            byte c0_g = (byte) ((c0 & 0b0000011111100000) >> 5);
            byte c0_b = ((byte) (c1 & 0b0000000000011111));
            byte c1_r = (byte) ((c1 & 0b1111100000000000) >> 11);
            byte c1_g = (byte) ((c1 & 0b0000011111100000) >> 5);
            byte c1_b = ((byte) (c1 & 0b0000000000011111));

            //Interpolate color2 and color3. DXT3 always uses the 4 color pattern.
            byte c2_r, c2_g, c2_b;
            byte c3_r, c3_g, c3_b;
            if ((c0 >= c1) || (bitmapType == FSHBitmapType.DXT3)) {
                c2_r = (byte) ((2.0f * c0_r + c1_r) / 3.0f);
                c2_g = (byte) ((2.0f * c0_g + c1_g) / 3.0f);
                c2_b = (byte) ((2.0f * c0_b + c1_b) / 3.0f);
                c3_r = (byte) ((c0_r + 2.0f * c1_r) / 3.0f);
                c3_g = (byte) ((c0_g + 2.0f * c1_g) / 3.0f);
                c3_b = (byte) ((c0_b + 2.0f * c1_b) / 3.0f);
            } else {
                c2_r = (byte) ((c0_r + c1_r) / 2.0f);
                c2_g = (byte) ((c0_g + c1_g) / 2.0f);
                c2_b = (byte) ((c0_b + c1_b) / 2.0f);
                c3_r = c3_g = c3_b = 0; //c3 is black
            }

            Argb32[] colors = new Argb32[] {
                new Argb32(c0_r, c0_g, c0_b),
                new Argb32(c1_r, c1_g, c1_b),
                new Argb32(c2_r, c2_g, c2_b),
                new Argb32(c3_r, c3_g, c3_b)
            };

            //Build a pixel array based on the color codes in the remaining 32 bits.
            int offset = 32;
            Block result = new Block();
            for (int by = 0; by < 4; by++) {
                for (int bx = 0; bx < 4; bx++) {
                    byte code = (byte) ((blob[offset + by] << (bx * 2)) & 3);
                    result.Pixels[by * 4 + bx] = colors[code];
                }
            }
            return result;
        }



        public static Image Blend(BitmapItem bitmapItem) {
            return Blend(bitmapItem.Alpha, bitmapItem.Color);
        }

        /// <summary>
        /// Blends the color/base bitmap with the alpha bitmap to create a transparent bitmap.
        /// </summary>
        /// <returns></returns>
        ///<remarks>Originally decompiled from BlendBmp.dll from <see ref="https://community.simtropolis.com/files/file/35279-fsh-converter-tool/">FSH Converter Tool</see>. Updated for cross platform interoperability.</remarks>
        public static unsafe Image Blend(Image alphaBmp, Image baseBmp) {
            Image blendedBmp;
            if (baseBmp is null || alphaBmp is null) {
                return null;
            }
            blendedBmp = new Image<Argb32>(baseBmp.Width, baseBmp.Height);

            //BitmapData colorBmpData = colorBmp.LockBits(new Rectangle(0, 0, blendedBmp.Width, blendedBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //BitmapData alphaBmpData = alphaBmp.LockBits(new Rectangle(0, 0, blendedBmp.Width, blendedBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            //BitmapData blendedBmpData = blendedBmp.LockBits(new Rectangle(0, 0, blendedBmp.Width, blendedBmp.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            byte test = 0;
            test[0] = 1;

            IntPtr scan0_blend = blendedBmpData.Scan0; //Gets or sets the address of the first pixel data in the bitmap. This can also be thought of as the first scan line in the bitmap.
            byte* scan0_color = (byte*) (void*) colorBmpData.Scan0;
            byte* scan0_alpha = (byte*) (void*) alphaBmpData.Scan0;
            byte* numPtr = (byte*) (void*) scan0_blend;
            //int numBlended = blendedBmpData.Stride - blendedBmp.Width * 4; //stride = width of a single row of pixels (scan line), rounded up to a 4 byte boundary.
            //int numColor = colorBmpData.Stride - blendedBmp.Width * 4;
            //int numAlpha = alphaBmpData.Stride - blendedBmp.Width * 4;
            for (int y = 0; y < blendedBmp.Height; ++y) {
                for (int x = 0; x < blendedBmp.Width; ++x) {
                    numPtr[3] = *scan0_alpha;
                    *numPtr = *scan0_color;
                    numPtr[1] = scan0_color[1];
                    numPtr[2] = scan0_color[2];
                    numPtr += 4;
                    scan0_color += 4;
                    scan0_alpha += 4;
                    numPtr[0] += 1;
                }
                //numPtr += numBlended;
                //scan0_color += numColor;
                //scan0_alpha += numAlpha;
            }
            //colorBmp.UnlockBits(colorBmpData);
            //alphaBmp.UnlockBits(alphaBmpData);
            //blendedBmp.UnlockBits(blendedBmpData);
            return blendedBmp;
        }
    }
}