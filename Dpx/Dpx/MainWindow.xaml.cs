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
        private uint sampleRate = 1000;
        private string audioFile = "SDRSharp_20150527_141931Z_146089kHz_IQ.wav";
        private IQFileReader reader = new IQFileReader();
        private double[,] spectrum = null;//信号打在bitmap格子上的次数
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
            heatmapPalette.Maximum = 100;
            heatmapPalette.Minimum = 0;
            this.xAxis.TextFormatting = "0.0KHz";
            this.yAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(0, 100);
            this.xAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(0, 1200);
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
            this.sampleRate = reader.waveOpeater.Sample;

            int fftLen = iqData.Length / 2;
            float[] real = new float[fftLen];
            float[] imag = new float[fftLen];
            for (int i = 0; i < fftLen; i++)
            {
                real[i] = iqData[2 * i];
                imag[i] = iqData[2 * i + 1];
            }

            FFT.Fourier(real, imag, WindowType.Hamming);

            float[] powerSpectrum = new float[fftLen];
            for (int j = 0; j < fftLen; j++)
            {
                powerSpectrum[j] = (float)(10 * Math.Log10(real[j] * real[j] + imag[j] * imag[j]));//dB
            }

            powerSpectrum = powerSpectrum.Take(fftLen / 2).ToArray();//取半频数据,FFT后的频谱对称

            double[] freqArr = FFT.FrequencyScale(sampleRate, fftLen).Take(fftLen / 2).ToArray();
            double freqScale = Math.Abs(freqArr[1] - freqArr[0]) / 1e3;
            if (spectrum == null)
            {
                spectrum = new double[200, freqArr.Length];
            }
            for (int x = 0; x < freqArr.Length; x++)
            {
                int y = (int)Math.Round(powerSpectrum[x]);
                spectrum[y, x] += 1;
            }
            var dataSeries = new UniformHeatmapDataSeries<double, double, double>(spectrum, 0, freqScale, 0, 1);
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

            this.xAxis.TextFormatting = "0.0MHz";
            double fs = 96e6;//采样率
            double fc = 20e6;//信号载频
            double t = 1 / fs;//时间间隔
            int A = 1400;//幅度
            int fftLen = 4096;//FFT点数
            spectrum = null;
            heatmapPalette.Maximum = 200;
            heatmapPalette.Minimum = 0;
            this.yAxis.VisibleRange = new SciChart.Data.Model.DoubleRange(0, 200);
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

                    FFT.Fourier(real, imag, WindowType.Hamming);

                    float[] powerSpectrum = new float[fftLen];
                    for (int j = 0; j < fftLen; j++)
                    {
                        powerSpectrum[j] = (float)(10 * Math.Log10(real[j] * real[j] + imag[j] * imag[j]));//dB
                    }

                    powerSpectrum = powerSpectrum.Take(fftLen / 2).ToArray();//取半频数据,FFT后的频谱对称
                    double[] freqArr = FFT.FrequencyScale(fs, fftLen).Take(fftLen / 2).ToArray();
                    double freqScale = Math.Abs(freqArr[1] - freqArr[0]) / 1e6;
                    if (spectrum == null)
                    {
                        spectrum = new double[200, freqArr.Length];
                    }
                    for (int x = 0; x < freqArr.Length; x++)
                    {
                        int y = (int)Math.Round(powerSpectrum[x]);
                        spectrum[y, x] += 1;
                    }
                    var dataSeries = new UniformHeatmapDataSeries<double, double, double>(spectrum, 0, freqScale, 0, 1);
                    heatmapSeries.DataSeries = dataSeries;
                    Thread.Sleep(20);
                }

            });
        }
    }
}
