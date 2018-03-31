using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace uf {
    public class MapBlock
    {
        public string name = string.Empty;

        public const int VERSION = 0;

        private int gridWidth;

        private int gridHeight;

        private byte[] blocksData;

        public int Width
        {
            get
            {
                return gridWidth;
            }
        }

        public int Height
        {
            get
            {
                return gridHeight;
            }
        }

        private static byte[] BITMASK = new byte[] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };


        public void Init(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
            Debug.AssertFormat(gridWidth > 0, "网格长度非法 {0}", gridWidth);
            Debug.AssertFormat(gridHeight > 0, "网格高度非法 {0}", gridHeight);

            int total = GetRowBytes(gridWidth) * gridHeight;
            blocksData = new byte[total];
        }

        public bool Load(BinaryReader reader)
        {
            try
            {
                int version = reader.ReadInt32();
                if (version != VERSION)
                {
                    Debug.LogErrorFormat("错误的版本号{0}", version);
                    return false;
                }
                gridWidth = reader.ReadInt32();
                gridHeight = reader.ReadInt32();
                int total = gridHeight * GetRowBytes(gridWidth);
                blocksData = reader.ReadBytes(total);
                if (blocksData == null || blocksData.Length != total)
                    return false;
                return true;
            }
            catch(System.Exception e)
            {
                Debug.LogErrorFormat("加载占位文件失败: {0}", e.Message);
                return false;
            }
        }

        public bool Save(BinaryWriter writer)
        {
            writer.Write((System.Int32)VERSION);
            writer.Write((System.Int32)gridWidth);
            writer.Write((System.Int32)gridHeight);
            writer.Write(blocksData);
            return true;
        }

        public bool IsValid()
        {
            return gridWidth > 0 && gridHeight > 0 && blocksData != null;
        }

        public bool IsBlocked(int x, int y)
        {
            if (x < 0 || x > gridWidth || y < 0 || y > gridHeight)
            {
                return true;
            }
            int bytePos = y * GetRowBytes(gridWidth) + x / 8;
            int bitPos = x % 8;

            if (bytePos >= blocksData.Length)
            {
                return true;
            }
            int val = (int)(blocksData[bytePos] & BITMASK[bitPos]);
            return val != 0 ? true : false;
        }

        public void SetBlocked(int x, int y, bool blocked)
        {
            if (x < 0 || x > gridWidth || y < 0 || y > gridHeight)
            {
                return;
            }
            int bytePos = y * GetRowBytes(gridWidth) + x / 8;
            int bitPos = x % 8;

            if (bytePos >= blocksData.Length)
            {
                return;
            }

            if (blocked)
            {
                blocksData[bytePos] = (byte)(blocksData[bytePos] | BITMASK[bitPos]);
            }
            else
            {
                blocksData[bytePos] = (byte)(blocksData[bytePos] & ~BITMASK[bitPos]);
            }
        }

        private int GetRowBytes(int width)
        {
            return width / 8 + 1;
        }
    }
}