using System;
using System.Diagnostics;
using NumFlat;
using NumFlat.IO;
using NumFlat.SignalProcessing;

public static class Program
{
    private static readonly int highSampleRate = 48000;
    private static readonly int lowSampleRate = 16000;

    private static readonly int sourceSignalLength = 60 * highSampleRate;

    public static void Main(string[] args)
    {
        var random = new Random(42);
        var signal = new Vec<double>(sourceSignalLength);
        foreach (ref var value in signal)
        {
            value = random.NextDouble() - 0.5;
        }

        var sw = new Stopwatch();

        sw.Start();
        var resampled = signal.Resample(1, 3, 50);
        sw.Stop();

        WaveFile.Write("source.wav", signal, highSampleRate);
        WaveFile.Write("resampled.wav", resampled, lowSampleRate);

        Console.WriteLine(sw.ElapsedMilliseconds);
    }
}
