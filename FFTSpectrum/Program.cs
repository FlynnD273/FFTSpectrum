using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NAudio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FFTSpectrum
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            string path;
            //string path = @"C:\Users\Flynn\Downloads\Milky Ways.wav";

            if (args.Length > 0 && File.Exists(args[0]))
            {
                path = args[0];
            }
            else
            {
                Console.WriteLine("Enter filepath to convert");
                path = Console.ReadLine();

                while (!File.Exists(path))
                {
                    Console.WriteLine("Invalid filepath");
                    path = Console.ReadLine();
                }
            }

            try
            {
                PerformFFT(path);
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred.\n{e.Message}\nPress any key to exit");
                Console.ReadLine();
            }
        }

        private static void PerformFFT(string path)
        {
            WaveFileReader wav = new WaveFileReader(path);

            double[] data = new double[wav.SampleCount];

            for (int i = 0; i < data.Length; i++)
            {
                float[] frame = wav.ReadNextSampleFrame();

                if (frame.Length > 1)
                {
                    data[i] = (double)(frame[0] + frame[1]) * 0.5;
                }
                else
                {
                    data[i] = (double)frame[0];
                }
            }

            Console.WriteLine("Read File Successfully");

            int windowSize = 13;
            Console.Write($"Enter window size power (def = {windowSize}): ");

            string response = Console.ReadLine();
            while (response != "" && !int.TryParse(response, out windowSize))
            {
                Console.WriteLine("Invalid input");
                response = Console.ReadLine();
            }

            windowSize = (int)Math.Pow(2, windowSize);

            int stepSize = 30;
            Console.Write($"Enter framerate (def = {stepSize}): ");

            response = Console.ReadLine();
            while (response != "" && !int.TryParse(response, out stepSize))
            {
                Console.WriteLine("Invalid input");
                response = Console.ReadLine();
            }

            stepSize = (int)Math.Ceiling((double)wav.WaveFormat.SampleRate / stepSize);

            int skip = 10;
            Console.Write($"Enter frequency bin size (def = {skip}): ");
            

            response = Console.ReadLine();
            while (response != "" && !int.TryParse(response, out skip))
            {
                Console.WriteLine("Invalid input");
                response = Console.ReadLine();
            }

            Console.WriteLine("Calculating samples\n");

            double[][] FFTSlices = RunFFT(data, windowSize, stepSize);

            string[] lines = new string[FFTSlices.Length + 1];
            lines[0] = wav.TotalTime.TotalSeconds.ToString();

            Console.WriteLine();

            for (int i = 0; i < FFTSlices.Length; i++)
            {
                lines[i + 1] = DoubleArrayToString(skip, FFTSlices[i]);

                if ((i + 1) % 100 == 0)
                {
                    Console.Write($"\rCopied sample {i + 1} of {FFTSlices.Length}  ");
                }
            }

            Console.Write($"\rCopied sample {FFTSlices.Length} of {FFTSlices.Length}  ");


            File.WriteAllLines(Path.Combine(Path.GetDirectoryName(path), "_" + Path.GetFileNameWithoutExtension(path) + " - Converted.txt"), lines);

            Console.WriteLine("\n\nDone! Press Enter to continue");
            Console.ReadLine();
        }

        private static string DoubleArrayToString (int skip, params double[] vals)
        {
            if (vals.Length == 0)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();

            if (Math.Round(vals[0], 1) != 0)
            {
                sb.Append(Math.Round(vals[0], 1));
            }

            for (int i = 1; i < vals.Length - skip; i += skip)
            {
                double val = 0;
                for (int j = i; j < skip + i; j++)
                {
                    val = Math.Max(val, vals[i]);
                }

                sb.Append("\\");

                if (Math.Round(val, 1) != 0)
                {
                    sb.Append(Math.Round(val, 1));
                }
            }

            return sb.ToString().TrimEnd('\\');
        }

        private static double[][] RunFFT(double[] data, int window, int step)
        {
            List<double[]> FFTSlices = new List<double[]>();

            int windowSize = window;
            double[] windowCoefficients = Window.BartlettHann(windowSize); ;

            for (int i = 0; i < data.Length + windowSize; i += step)
            {
                double[] windowedVals = new double[windowSize];
                for (int j = 0; j < windowSize; j++)
                {
                    if (j + i < data.Length)
                    {
                        windowedVals[j] = data[j + i] * windowCoefficients[j];
                    }
                    else
                    {
                        windowedVals[j] = 0;
                    }
                }

                FFTSlices.Add(FFTWindow(windowedVals));
                if ((i / step) % 200 == 0)
                {
                    Console.Write($"\rCalculated sample {i / step} of {(data.Length + windowSize) / step}  ");
                }
            }
            Console.Write($"\rCalculated sample {(data.Length + windowSize) / step} of {(data.Length + windowSize) / step}  ");

            return FFTSlices.ToArray();
        }

        private static double[] FFTWindow(double[] samples)
        {
            Complex[] comps = new Complex[samples.Length];

            for (int i = 0; i < samples.Length; i++)
            {
                comps[i] = new Complex(samples[i], 0);
            }

            Fourier.Forward(comps);

            double[] values = new double[comps.Length / 2];

            for (int i = 0; i < values.Length; i++)
            {
                values[i] = comps[i].Magnitude;
            }

            return values;
        }
    }
}
