using HandyControl.Controls;
using Microsoft.Win32;
using SciChart.Charting.Model.DataSeries;
using SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dpx
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            reader.IQCallBack += new Action<float[], int>(ProcessFFT);
            reader.ProgressCallBack += new Action<int>((progress) =>
            {
                this.progressBar.Value = progress;
            });
        }

        private bool exit = true;
        private uint sampleRate = 0;
        private double scale = 0;
        private double maxNum = 200;//出现的最大次数
        private string audioFile = "SDRSharp_20150527_141931Z_146089kHz_IQ.wav";
        private IQFileReader reader = new IQFileReader();
        private ulong[,] spectrum = null;//信号打在bitmap格子上的次数
        private void openFileBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "wav文件|*.wav";
            if (dialog.ShowDialog() == true)
            {
                this.audioFile = dialog.FileName;
                this.filetxt.Text = dialog.FileName;
            }
        }

        private void startBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (reader.IsRunning)
            {
                MessageBox.Error("正在读取波形文件!");
                return;
            }
            if (!exit)
            {
                MessageBox.Error("正在运行Sinx模拟!");
                return;
            }

            spectrum = null;
            sampleRate = 0;
            scale = 0;
            reader.StartReadIQ(audioFile, 8192);
        }

        private void stopBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            exit = true;
            this.progressBar.Value = 0;
            reader.StopReadIQ();
        }

        private void ProcessFFT(float[] iqData, int length)
        {
            if (sampleRate == 0)
            {
                this.sampleRate = reader.waveOpeater.Sample;
                this.maxNum = 200;
                this.heatmapPalette.Maximum = maxNum;
                this.heatmapPalette.Minimum = 0;
                this.scale = GetFreqHzScale(sampleRate);
                this.xAxis.TextFormatting = $"0.0{GetFreqUnitName(sampleRate)}";
                this.yAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(0, maxNum);
                this.xAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(0, sampleRate / 2 / scale);
            }


            int fftLen = iqData.Length / 2;
            float[] real = new float[fftLen];
            float[] imag = new float[fftLen];
            for (int i = 0; i < fftLen; i++)
            {
                real[i] = iqData[2 * i];
                imag[i] = iqData[2 * i + 1];
            }

            Fft(real, imag, fftLen);
        }

        private void Fft(float[] real, float[] imag, int fftLen)
        {
            FFT.Fourier(real, imag, WindowType.Hamming);

            float[] powerSpectrum = new float[fftLen];
            for (int j = 0; j < fftLen; j++)
            {
                powerSpectrum[j] = (float)(10 * Math.Log10(real[j] * real[j] + imag[j] * imag[j]));//dB
            }

            powerSpectrum = powerSpectrum.Take(fftLen / 2).ToArray();//取半频数据,FFT后的频谱对称

            int freqLength = fftLen / 2;
            //double[] freqArr = FFT.FrequencyScale(sampleRate, fftLen).Take(fftLen / 2).ToArray();
            double freqScale = sampleRate / fftLen / scale;//频率间隔
            if (spectrum == null)
            {
                spectrum = new ulong[(int)maxNum, freqLength];
            }
            for (int x = 0; x < freqLength; x++)
            {
                int y = (int)Math.Round(powerSpectrum[x]) + 30;
                spectrum[y, x] += 1;
            }
            var dataSeries = new UniformHeatmapDataSeries<double, double, ulong>(spectrum, 0, freqScale, 0, 1);
            heatmapSeries.DataSeries = dataSeries;
        }

        /// <summary>
        /// sin函数信号模拟
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void sinModeBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (reader.IsRunning)
            {
                MessageBox.Error("正在读取波形文件!");
                return;
            }
            if (!exit)
            {
                MessageBox.Error("正在运行Sinx模拟!");
                return;
            }
            spectrum = null;
            scale = 0;


            double fs = 96e6;//采样率
            double fc = 20e6;//信号载频
            double t = 1 / fs;//时间间隔
            int A = 1400;//幅度
            int fftLen = 4096;//FFT点数
            this.sampleRate = (uint)fs;
            this.scale = GetFreqHzScale(sampleRate);

            this.maxNum = 200;
            this.heatmapPalette.Maximum = maxNum;
            this.heatmapPalette.Minimum = 0;
            this.xAxis.TextFormatting = "0.0MHz";
            this.yAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(0, maxNum);
            this.xAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(0, 48);

            exit = false;
            Task.Run(() =>
            {
                Random rd = new Random();
                while (!exit)
                {
                    double noise = rd.Next(0, A) * 1.0 / 50;//随机噪声

                    float[] real = new float[fftLen];
                    float[] imag = new float[fftLen];
                    for (int i = 0; i < fftLen; i++)
                    {
                        real[i] = (float)Math.Round(A * Math.Sin(2 * Math.PI * fc * (t * i)) + noise);
                        imag[i] = 0.0f;
                    }

                    Fft(real, imag, real.Length);
                    Thread.Sleep(20);
                }

            });
        }


        /// <summary>
        /// 频率转换
        /// </summary>
        /// <param name="sampleRate">hz</param>
        /// <returns></returns>
        public static string GetFreqUnitName(double sampleRate)
        {
            string name = "Hz";
            int len = Math.Floor(sampleRate).ToString().Length;
            if (len < 13 && len >= 10)
            {
                name = "GHz";
            }
            else if (len < 10 && len >= 7)
            {
                name = "MHz";
            }
            else if (len < 7 && len >= 4)
            {
                name = "KHz";
            }
            else if (len < 16 && len >= 13)
            {
                name = "Hz(E-12)";
            }
            else
            {
                name = "Hz";
            }
            return name;
        }

        /// <summary>
        /// 通过采样率获取频率转HZ单位的进制
        /// </summary>
        /// <param name="sampleRate"></param>
        /// <returns></returns>
        public static int GetFreqHzScale(double sampleRate)
        {
            double scale = 1;
            string name = GetFreqUnitName(sampleRate);
            switch (name)
            {
                case "GHz":
                    scale = Math.Pow(10, 9);
                    break;
                case "MHz":
                    scale = Math.Pow(10, 6);
                    break;
                case "KHz":
                    scale = Math.Pow(10, 3);
                    break;
                case "Hz(E-12)":
                    scale = Math.Pow(10, 12);
                    break;
                case "Hz":
                    scale = 1;
                    break;
                default:
                    break;
            }
            return (int)scale;
        }
    }
}
