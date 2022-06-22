using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Dpx {
    public class IQFileReader {

        public IQFileReader() {
            readIQtimer.Tick += ReadIQtimer_Tick;
            readIQtimer.Interval = TimeSpan.FromMilliseconds(2);
        }

        private long fromIndex = 0;
        private int iqLength = 65536 * 2;//读取IQ
        private readonly DispatcherTimer readIQtimer = new DispatcherTimer(DispatcherPriority.Render);
        private int totalCount = 0;
        private int readCount = 0;
        public readonly WaveOpeater waveOpeater = new WaveOpeater();
        public Action<float[], int> IQCallBack { set; get; }
        public Action<int> ProgressCallBack { set; get; }
        public bool IsRunning { get { return this.readIQtimer.IsEnabled; } }
        private void ReadIQtimer_Tick(object sender, EventArgs e) {
            if (readCount == totalCount) {
                MessageBox.Info("读取完成!");
                StopReadIQ();
                return;
            }
            int bits = (int)waveOpeater.BitNum / 8;
            int readsize = iqLength * bits;
            byte[] iqBytes = new byte[readsize];

            waveOpeater.SeekBegin(fromIndex);
            waveOpeater.Bread.Read(iqBytes, 0, iqBytes.Length);
            IntPtr ptr = Marshal.AllocHGlobal(readsize);
            Marshal.Copy(iqBytes, 0, ptr, iqBytes.Length);

            float[] iq = null;
            switch (waveOpeater.BitNum) {
                case 8: {
                        byte[] iqBuffer = new byte[iqLength];
                        Marshal.Copy(ptr, iqBuffer, 0, iqLength);
                        iq = Array.ConvertAll(iqBuffer, (t) => (float)t);
                    }
                    break;
                case 16: {
                        Int16[] iqBuffer = new Int16[iqLength];
                        Marshal.Copy(ptr, iqBuffer, 0, iqLength);
                        iq = Array.ConvertAll(iqBuffer, (t) => (float)t);
                    }
                    break;
                case 32: {
                        float[] iqBuffer = new float[iqLength];
                        Marshal.Copy(ptr, iqBuffer, 0, iqLength);
                        iq = iqBuffer;
                    }
                    break;
                default:
                    break;
            }
            Marshal.FreeHGlobal(ptr);

            IQCallBack?.Invoke(iq, iqLength);

            fromIndex += readsize;
            readCount++;

            int progress = (int)(readCount * 1.0 / totalCount * 100);
            ProgressCallBack?.Invoke(progress);
        }

        public void StartReadIQ(string iqFileName, int iqLength) {
            bool opened = waveOpeater.OpenWave(iqFileName);
            if (!opened)
                return;
            long offset = 0;
            if (this.waveOpeater.Riff.ToUpper() == "RIFF") {
                offset = 44;
                if (this.waveOpeater.Unknown != 0)
                    offset += 2;
            }
            else {
                offset = 0;
            }
            this.fromIndex = offset;
            this.iqLength = iqLength;

            readCount = 0;
            int n = (int)waveOpeater.BitNum / 8;
            int readsize = iqLength * n;
            if (waveOpeater.DataLength < readsize) {
                this.iqLength = 2048;
                readsize = this.iqLength * n;
            }
            totalCount = (int)waveOpeater.DataLength / readsize;

            readIQtimer.Start();
        }
        public void StopReadIQ() {
            fromIndex = 0;
            readCount = 0;
            totalCount = 0;
            waveOpeater.Close();
            readIQtimer.Stop();
        }

        public void PuseRead() {
            readIQtimer.Stop();
        }

        public void ContinueRead() {
            readIQtimer.Start();
        }
    }
}
