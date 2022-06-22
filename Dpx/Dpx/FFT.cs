using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dpx
{

    public enum WindowType
    {
        //
        // 摘要:
        //     Bartlett window.
        Bartlett,
        //
        // 摘要:
        //     Bartlett-Hann window.
        BartlettHann,
        //
        // 摘要:
        //     Blackman window.
        Blackman,
        //
        // 摘要:
        //     Blackman-Harris window.
        BlackmanHarris,
        //
        // 摘要:
        //     Blackman-Nuttall window.
        BlackmanNuttall,
        //
        // 摘要:
        //     Cosine window. Symmetric version, useful e.g. for filter design purposes.
        Cosine,
        //
        // 摘要:
        //     Cosine window. Periodic version, useful e.g. for FFT purposes.
        CosinePeriodic,
        //
        // 摘要:
        //     Uniform rectangular (Dirichlet) window.
        Dirichlet,
        //
        // 摘要:
        //     Flat top window.
        FlatTop,
        //
        // 摘要:
        //     Gauss window.
        Gauss,
        //
        // 摘要:
        //     Hamming window. Named after Richard Hamming. Symmetric version, useful e.g. for
        //     filter design purposes.
        Hamming,
        //
        // 摘要:
        //     Hamming window. Named after Richard Hamming. Periodic version, useful e.g. for
        //     FFT purposes.
        HammingPeriodic,
        //
        // 摘要:
        //     Hann window. Named after Julius von Hann. Symmetric version, useful e.g. for
        //     filter design purposes.
        Hann,
        //
        // 摘要:
        //     Hann window. Named after Julius von Hann. Periodic version, useful e.g. for FFT
        //     purposes.
        HannPeriodic,
        //
        // 摘要:
        //     Lanczos window. Symmetric version, useful e.g. for filter design purposes.
        Lanczos,
        //
        // 摘要:
        //     Lanczos window. Periodic version, useful e.g. for FFT purposes.
        LanczosPeriodic,
        //
        // 摘要:
        //     Nuttall window.
        Nuttall,
        //
        // 摘要:
        //     Triangular window.
        Triangular,
        //
        // 摘要:
        //     Tukey tapering window. A rectangular window bounded by half a cosine window on
        //     each side.
        //
        // 参数:
        //   width:
        //     Width of the window
        //
        //   r:
        //     Fraction of the window occupied by the cosine parts
        Tukey,
    }

    /// <summary>
    /// 基于Math.Net的FFT
    /// </summary>
    public class FFT
    {
        /// <summary>
        /// 生成频率数组  前面的为正频率后面的为负频率
        /// </summary>
        /// <param name="length">样本长度</param>
        /// <param name="sampleRate">采样率</param>
        /// <returns></returns>
        public static double[] FrequencyScale(double sampleRate, int length) {
            return MathNet.Numerics.IntegralTransforms.Fourier.FrequencyScale(length, sampleRate);
        }

        /// <summary>
        /// FFT [0]DC
        /// </summary>
        /// <param name="samples"></param>
        public static void Fourier(MathNet.Numerics.Complex32[] samples, WindowType windowType) {
            double[] coffes = Window(windowType, samples.Length);
            for (int i = 0; i < samples.Length; i++) {
                float real = (float)(samples[i].Real * coffes[i]);
                float img = (float)(samples[i].Imaginary * coffes[i]);
                samples[i] = new MathNet.Numerics.Complex32(real, img);
            }
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(samples, FourierOptions.Matlab);
        }

        /// <summary>
        /// FFT [0]DC
        /// </summary>
        /// <param name="real"></param>
        /// <param name="imaginary"></param>
        public static void Fourier(double[] real, double[] imaginary, WindowType windowType) {
            if (real.Length != imaginary.Length)
                throw new ArgumentException("实部虚部长度不等!");
            double[] coffes = Window(windowType, real.Length);
            for (int i = 0; i < real.Length; i++) {
                real[i] *= coffes[i];
                imaginary[i] *= coffes[i];
            }
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(real, imaginary, FourierOptions.Matlab);
        }

        /// <summary>
        /// FFT [0]DC
        /// </summary>
        /// <param name="real"></param>
        /// <param name="imaginary"></param>
        public static void Fourier(float[] real, float[] imaginary, WindowType windowType) {
            if (real.Length != imaginary.Length)
                throw new ArgumentException("实部虚部长度不等!");
            double[] coffes = Window(windowType, real.Length);
            for (int i = 0; i < real.Length; i++) {
                real[i] *= (float)coffes[i];
                imaginary[i] *= (float)coffes[i];
            }
            MathNet.Numerics.IntegralTransforms.Fourier.Forward(real, imaginary, FourierOptions.Matlab);
        }

        /// <summary>
        /// 傅里叶实数变换 [0]DC
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="n">样本点数是偶数则data的长度为n+2,样本点数是奇数则data的长度是n+1</param>
        public static void Fourier(double[] samples, int n, WindowType windowType) {
            double[] coffes = Window(windowType, samples.Length);
            for (int i = 0; i < samples.Length; i++) {
                samples[i] *= coffes[i];
            }
            MathNet.Numerics.IntegralTransforms.Fourier.ForwardReal(samples, n);
        }

        /// <summary>
        /// 傅里叶实数变换 [0]DC
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="n">样本点数是偶数则data的长度为n+2,样本点数是奇数则data的长度是n+1</param>
        public static void Fourier(float[] samples, int n, WindowType windowType) {
            double[] coffes = Window(windowType, samples.Length);
            for (int i = 0; i < samples.Length; i++) {
                samples[i] *= (float)coffes[i];
            }
            MathNet.Numerics.IntegralTransforms.Fourier.ForwardReal(samples, n, FourierOptions.Matlab);
        }

        /// <summary>
        /// 加窗
        /// </summary>
        /// <param name="windowType"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static double[] Window(WindowType windowType, int width) {
            switch (windowType) {
                case WindowType.Bartlett:
                    return MathNet.Numerics.Window.Bartlett(width);
                case WindowType.BartlettHann:
                    return MathNet.Numerics.Window.BartlettHann(width);
                case WindowType.Blackman:
                    return MathNet.Numerics.Window.Blackman(width);
                case WindowType.BlackmanHarris:
                    return MathNet.Numerics.Window.BlackmanHarris(width);
                case WindowType.BlackmanNuttall:
                    return MathNet.Numerics.Window.BlackmanNuttall(width);
                case WindowType.Cosine:
                    return MathNet.Numerics.Window.Cosine(width);
                case WindowType.CosinePeriodic:
                    return MathNet.Numerics.Window.CosinePeriodic(width);
                case WindowType.Dirichlet:
                    return MathNet.Numerics.Window.Dirichlet(width);
                case WindowType.FlatTop:
                    return MathNet.Numerics.Window.FlatTop(width);
                case WindowType.Gauss:
                    return MathNet.Numerics.Window.Gauss(width, 1);//不用
                case WindowType.Hamming:
                    return MathNet.Numerics.Window.Hamming(width);
                case WindowType.HammingPeriodic:
                    return MathNet.Numerics.Window.HammingPeriodic(width);
                case WindowType.Hann:
                    return MathNet.Numerics.Window.Hann(width);
                case WindowType.HannPeriodic:
                    return MathNet.Numerics.Window.HannPeriodic(width);
                case WindowType.Lanczos:
                    return MathNet.Numerics.Window.Lanczos(width);
                case WindowType.LanczosPeriodic:
                    return MathNet.Numerics.Window.LanczosPeriodic(width);
                case WindowType.Nuttall:
                    return MathNet.Numerics.Window.Nuttall(width);
                case WindowType.Triangular:
                    return MathNet.Numerics.Window.Triangular(width);
                case WindowType.Tukey:
                    return MathNet.Numerics.Window.Tukey(width);
                default:
                    return null;
            }
        }

        public static void FourierInverse(double[] data, int n) {
            MathNet.Numerics.IntegralTransforms.Fourier.InverseReal(data, n);
        }

        public static void FourierInverse(float[] data, int n) {
            MathNet.Numerics.IntegralTransforms.Fourier.InverseReal(data, n);
        }


        public static void FourierInverse(MathNet.Numerics.Complex32[] samples) {
            MathNet.Numerics.IntegralTransforms.Fourier.Inverse(samples);
        }

        public static void FourierInverse(double[] real, double[] imaginary) {
            MathNet.Numerics.IntegralTransforms.Fourier.Inverse(real, imaginary);
        }

        public static void FourierInverse(float[] real, float[] imaginary) {
            MathNet.Numerics.IntegralTransforms.Fourier.Inverse(real, imaginary);
        }
    }
}
