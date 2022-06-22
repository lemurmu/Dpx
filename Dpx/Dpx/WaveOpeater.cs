using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// 提供wav文件头的访问
/// 和文件写入相关操作
/// </summary>
namespace Dpx {
    public struct RIFF {
        public string Riff { set; get; }//4 'RIFF'
        public uint RiffSize { set; get; }//4 filesize-8
        public string WaveID { set; get; }
    }


    public class WaveOpeater : MarshalByRefObject, IDisposable {

        
        #region      
        //RIFF区
        private byte[] riff;  //4	    //文件标识 'RIFF'
        private byte[] riffSize; //4	//文件大小
        private byte[] waveID; //4	    //文件类型


        //format区
        private byte[] fmtID; //4	
        private byte[] formatSize; //4	          //数值为16或18，18则最后又附加信息
        private byte[] waveType;  //2	
        private byte[] channel;  //2	         //声道数目，1--单声道；2--双声道
        private byte[] SamplesPerSec;  //4	     //采样率
        private byte[] AvgBytesPerSec;   //4	 //每秒所需字节数 
        private byte[] BlockAlign;  //2	块大小   //数据块对齐单位(每个采样需要的字节数) 
        private byte[] BitsPerSample;  //2	    //每个采样需要的bit数
        private byte[] unknown; //2	     附加信息（可选，通过Size来判断有无）
                                /*
                                 * 以'fmt'作为标示。一般情况下Size为16，此时最后附加信息没有；
                                 * 如果为18则最后多了2个字节的附加信息。
                                 * 主要由一些软件制成的wav格式中含有该2个字节的附加信息
                                 */

        //data区块
        private byte[] dataID;  //4	'data'
        private byte[] dataLength;  //4	音频数据长度


        short[] data;
        private string longFileName = string.Empty;

        public string LongFileName {
            get { return longFileName; }
        }

        public string ShortFileName {
            get {
                int pos = LongFileName.LastIndexOf("\\");
                return LongFileName.Substring(pos + 1);
            }
        }

        public short[] Data {
            get { return data; }
            set { data = value; }
        }

        public string Riff {
            get { return Encoding.Default.GetString(riff); }
            set { riff = Encoding.Default.GetBytes(value); }
        }

        public uint RiffSize {
            get {
                return BitConverter.ToUInt32(riffSize, 0);
            }
            set {
                riffSize = BitConverter.GetBytes(value);
            }

        }

        public string WaveID {
            get { return Encoding.Default.GetString(waveID); }
            set { waveID = Encoding.Default.GetBytes(value); }
        }

        public string FmtID {
            get { return Encoding.Default.GetString(fmtID); }
            set { fmtID = Encoding.Default.GetBytes(value); }
        }

        public int FormSize {
            get {
                return BitConverter.ToInt32(formatSize, 0);
            }

            set { formatSize = BitConverter.GetBytes(value); }
        }

        public short WaveType {
            get {
                return BitConverter.ToInt16(waveType, 0);
            }
            set { waveType = BitConverter.GetBytes(value); }
        }


        public ushort Channel {
            get {
                return BitConverter.ToUInt16(channel, 0);
            }
            set { channel = BitConverter.GetBytes(value); }
        }

        public uint Sample {
            get {
                return BitConverter.ToUInt32(SamplesPerSec, 0);
            }
            set { SamplesPerSec = BitConverter.GetBytes(value); }
        }

        public uint Send {
            get { return BitConverter.ToUInt32(AvgBytesPerSec, 0); }
            set { AvgBytesPerSec = BitConverter.GetBytes(value); }
        }

        public ushort BlockAjust {
            get {
                return BitConverter.ToUInt16(BlockAlign, 0);
            }
            set { BlockAlign = BitConverter.GetBytes(value); }
        }

        public ushort BitNum {
            get {
                return BitConverter.ToUInt16(BitsPerSample, 0);
            }
            set { BitsPerSample = BitConverter.GetBytes(value); }
        }

        public ushort Unknown {
            get {
                if (unknown == null) {
                    return 1;
                }
                else
                    return BitConverter.ToUInt16(unknown, 0);
            }

            set { unknown = BitConverter.GetBytes(value); }
        }

        public string DataID {
            get { return Encoding.Default.GetString(dataID); }
            set { dataID = Encoding.Default.GetBytes(value); }
        }

        public uint DataLength {
            get {
                return BitConverter.ToUInt32(dataLength, 0);
            }
            set { dataLength = BitConverter.GetBytes(value); }
        }

        public BinaryReader Bread { get => bread; set => bread = value; }
        #endregion
        String m_filepath;
        FileStream _fs;
        BinaryReader bread;
        BinaryWriter bwriter;
        public WaveOpeater() {
            riff = new byte[4];
            riffSize = new byte[4];
            waveID = new byte[4];
            fmtID = new byte[4];
            formatSize = new byte[4];
            waveType = new byte[2];
            channel = new byte[2];
            SamplesPerSec = new byte[4];
            AvgBytesPerSec = new byte[4];
            BlockAlign = new byte[2];
            BitsPerSample = new byte[2];
            unknown = new byte[2];
            dataID = new byte[4];  //52	
            dataLength = new byte[4];  //56个字节    
        }

        public void SeekBegin(long index) {
            _fs.Seek(index, SeekOrigin.Begin);
        }

        /// <summary>
        /// 读取wave
        /// </summary>
        /// <param name="filepath"></param>
        public bool OpenWave(string filepath) {
            m_filepath = filepath;
            try {
                if (File.Exists(filepath)) {
                    _fs = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                    bread = new BinaryReader(_fs);
                    bwriter = new BinaryWriter(_fs);
                    riff = bread.ReadBytes(4);
                    riffSize = bread.ReadBytes(4);
                    waveID = bread.ReadBytes(4);
                    fmtID = bread.ReadBytes(4);
                    formatSize = bread.ReadBytes(4);
                    waveType = bread.ReadBytes(2);
                    channel = bread.ReadBytes(2);
                    SamplesPerSec = bread.ReadBytes(4);
                    AvgBytesPerSec = bread.ReadBytes(4);
                    BlockAlign = bread.ReadBytes(2);
                    BitsPerSample = bread.ReadBytes(2);
                    if (formatSize.Length > 0 && BitConverter.ToUInt32(formatSize, 0) == 18) {
                        unknown = bread.ReadBytes(2);
                    }
                    dataID = bread.ReadBytes(4);
                    dataLength = bread.ReadBytes(4);
                    if (this.Riff.ToUpper() != "RIFF") {
                        DataLength = (uint)_fs.Length;
                    }
                }
                else {
                    _fs = new FileStream(filepath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                    bread = new BinaryReader(_fs);
                    bwriter = new BinaryWriter(_fs);
                }
                return true;
            }
            catch (System.Exception ex) {
               // Logger.Error("打开文件失败:" + ex.Message);
                return false;
            }
        }

        public int ReadWavData(int from, int bufferSize, byte[] buffer) {
            if (buffer.Length != bufferSize) {
                throw new Exception("读取Wav 文件, 读取大小与buffer 不匹配");
            }
            long offset = 0;
            offset = 44 + from;
            _fs.Seek(offset, SeekOrigin.Begin);
            int output = bread.Read(buffer, 0, bufferSize);
            return output;
        }

        public int ReadData(int from, int bufferSize, byte[] buffer) {
            if (buffer.Length != bufferSize) {
                throw new Exception("读取Wav 文件, 读取大小与buffer 不匹配");
            }
            long offset = 0;
            if (this.Riff.ToUpper() == "RIFF") {
                offset = 44 + from;
                if (this.Unknown != 0)
                    offset += 2;
            }
            else {
                offset = from;
            }
            try {
                _fs.Seek(offset, SeekOrigin.Begin);
                int output = bread.Read(buffer, 0, bufferSize);
                return output;
            }
            catch (Exception ex) {
                //Logger.Error(ex.Message);
            }
            return -1;
        }

        public int WriteData(int from, byte[] buffer, int bufferSize) {

            if (buffer.Length != bufferSize) {
                throw new Exception("写入数据长度不匹配");
            }
            long offset = 0;
            offset = 44 + from;
            _fs.Seek(offset, SeekOrigin.Begin);
            bwriter.Write(buffer, 0, bufferSize);
            bwriter.Flush();
            return 0;
        }

        public int WriteData(byte[] buffer, int bufferSize) {
            if (buffer.Length != bufferSize) {
                throw new Exception("写入数据长度不匹配");
            }
            long offset = 0;
            offset = 44;
            _fs.Seek(offset, SeekOrigin.Begin);
            bwriter.Write(buffer, 0, bufferSize);
            bwriter.Flush();
            DataLength += (uint)bufferSize;
            return 0;
        }

        public void Write(float[] arr, int length) {
            if (arr.Length != length) {
                throw new Exception("写入数据长度不匹配");
            }

            long offset = 0;
            offset = 44;
            _fs.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < arr.Length; i++) {
                bwriter.Write(arr[i]);
            }
            bwriter.Flush();
            DataLength += (uint)length;
        }

        public void Write(int from, float[] arr, int length) {
            if (arr.Length != length) {
                throw new Exception("写入数据长度不匹配");
            }

            long offset = 0;
            offset = 44 + from;
            _fs.Seek(offset, SeekOrigin.Begin);
            for (int i = 0; i < arr.Length; i++) {
                bwriter.Write(arr[i]);
            }
            bwriter.Flush();
            DataLength += (uint)length;
        }

        /// <summary>
        /// 生成wav文件到系统
        /// </summary>
        /// <param name="fileName">要保存的文件名</param>
        /// <returns></returns>
        public bool writeHeader() {
            try {
                bwriter.Seek(0, SeekOrigin.Begin);
                bwriter.Write(Encoding.Default.GetBytes(this.Riff));     //不可以直接写入string类型的字符串，字符串会有串结束符，比原来的bytes多一个字节
                bwriter.Write(this.RiffSize);
                bwriter.Write(Encoding.Default.GetBytes(this.WaveID));
                bwriter.Write(Encoding.Default.GetBytes(this.FmtID));
                bwriter.Write(this.FormSize);
                bwriter.Write(this.WaveType);
                bwriter.Write(this.Channel);
                bwriter.Write(this.Sample);
                bwriter.Write(this.Send);
                bwriter.Write(this.BlockAjust);
                bwriter.Write(this.BitNum);
                if (this.Unknown != 0)
                    bwriter.Write(this.Unknown);
                bwriter.Write(Encoding.Default.GetBytes(this.DataID));
                bwriter.Write(this.DataLength);
                bwriter.Flush();


                return true;
            }
            catch (System.Exception ex) {
                Console.Write(ex.Message);
                return false;
            }
        }

        public bool writeHeader(string Riff, uint RiffSize, string WaveID, string FmtID, int FormSize, short WaveType, ushort Channel, uint Sample, uint Send, ushort BlockAjust, ushort BitNum, string DataID, uint DataLength) {
            try {
                bwriter.Seek(0, SeekOrigin.Begin);
                bwriter.Write(Encoding.Default.GetBytes(Riff));     //不可以直接写入string类型的字符串，字符串会有串结束符，比原来的bytes多一个字节
                bwriter.Write(RiffSize);
                bwriter.Write(Encoding.Default.GetBytes(WaveID));
                bwriter.Write(Encoding.Default.GetBytes(FmtID));
                bwriter.Write(FormSize);//16
                bwriter.Write(WaveType);//0
                bwriter.Write(Channel);
                bwriter.Write(Sample);
                bwriter.Write(Send);
                bwriter.Write(BlockAjust);
                bwriter.Write(BitNum);
                bwriter.Write(Encoding.Default.GetBytes(DataID));
                bwriter.Write(DataLength);
                bwriter.Flush();

                long len = bwriter.BaseStream.Length;
                return true;
            }
            catch (System.Exception ex) {
                Console.Write(ex.Message);
                return false;
            }
        }


        public int Close() {
            Dispose();
            return 0;

        }

        public void Dispose() {
            Dispose(true);
        }
        ~WaveOpeater() {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                if (bread != null)
                    bread.Close();
                if (bwriter != null)
                    bwriter.Close();
                if (_fs != null)
                    _fs.Close();
            }
        }
    }
}

