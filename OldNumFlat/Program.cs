using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using NumFlat;
using NumFlat.SignalProcessing;

public static class Program
{
    private const int HighSampleRate = 48000;
    private const int LowSampleRate = 16000;
    private const int DurationSeconds = 60;
    private const int FilterOrder = 50;
    private const int MeasurementCount = 5;

    public static void Main(string[] args)
    {
        var outputDirectory = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDirectory);

        var downsampleSource = CreateSignal(DurationSeconds * HighSampleRate, 42);
        var upsampleSource = CreateSignal(DurationSeconds * LowSampleRate, 43);

        WarmUp();

        var (downsampled, downsampleMilliseconds) = Measure(downsampleSource, LowSampleRate, HighSampleRate);
        var (upsampled, upsampleMilliseconds) = Measure(upsampleSource, HighSampleRate, LowSampleRate);

        WriteSignal(Path.Combine(outputDirectory, "old_downsampled.bin"), downsampled);
        WriteSignal(Path.Combine(outputDirectory, "old_upsampled.bin"), upsampled);

        Console.WriteLine("NumFlat 1.3.2");
        Console.WriteLine($"  Downsampling (48 kHz -> 16 kHz): {downsampleMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} ms");
        Console.WriteLine($"  Upsampling   (16 kHz -> 48 kHz): {upsampleMilliseconds.ToString("F3", CultureInfo.InvariantCulture)} ms");
    }

    private static Vec<double> CreateSignal(int length, int seed)
    {
        var random = new Random(seed);
        var signal = new Vec<double>(length);
        foreach (ref var value in signal)
        {
            value = random.NextDouble() - 0.5;
        }

        return signal;
    }

    private static void WarmUp()
    {
        var signal = CreateSignal(480, 0);
        _ = signal.Resample(LowSampleRate, HighSampleRate, FilterOrder);
        _ = signal.Resample(HighSampleRate, LowSampleRate, FilterOrder);
    }

    private static (Vec<double> Result, double MedianMilliseconds) Measure(
        Vec<double> source,
        int outputSampleRate,
        int inputSampleRate)
    {
        var elapsedMilliseconds = new double[MeasurementCount];
        Vec<double> result = default;

        for (var i = 0; i < MeasurementCount; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var stopwatch = Stopwatch.StartNew();
            result = source.Resample(outputSampleRate, inputSampleRate, FilterOrder);
            stopwatch.Stop();
            elapsedMilliseconds[i] = stopwatch.Elapsed.TotalMilliseconds;
        }

        Array.Sort(elapsedMilliseconds);
        return (result, elapsedMilliseconds[MeasurementCount / 2]);
    }

    private static void WriteSignal(string path, Vec<double> signal)
    {
        using var writer = new BinaryWriter(File.Create(path));
        writer.Write(signal.Count);
        foreach (var value in signal)
        {
            writer.Write(value);
        }
    }
}
