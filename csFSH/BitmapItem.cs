using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static csDBPF.Entries.DBPFEntryFSH;

namespace csFSH {
    public class BitmapItem {
        /// <summary>
        /// Byte data associated with this bitmap.
        /// </summary>
        public byte[] RawData { get; set; }
        /// <summary>
        /// Color (base) bitmap.
        /// </summary>
        public Image Color { get; set; }
        /// <summary>
        /// Alpha (transparency) bitmap.
        /// </summary>
        public Image Alpha { get; set; }
        /// <summary>
        /// Defines the bitmap type of this item.
        /// </summary>
        public FSHBitmapType BitmapType { get; set; }
        public string[] Comments { get; set; } //TODO - what's this for???
        public bool IsCompressed { get; set; } //TODO - is this QFS or DXT compression reference?
        public Color[] Palette { get; set; }

        public BitmapItem(byte[] rawData) {
            RawData = rawData;
            //Color = new Image();
            //Alpha = new Image();
            //BitmapType = ...;
            //Palette = new Color[];
        }

        public Image Blend() {
            return FSHImage.Blend(this);
        }
    }
}
