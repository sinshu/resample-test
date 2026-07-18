using System.Diagnostics;

const double ComparisonTolerance = 1e-9;

var rootDirectory = FindRootDirectory();
var outputDirectory = Path.Combine(rootDirectory, "artifacts");
Directory.CreateDirectory(outputDirectory);

var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name
    ?? throw new InvalidOperationException("Could not determine the build configuration.");

Console.WriteLine("Resampling benchmark (60-second random signal, median of 5 runs)\n");
Console.WriteLine(await RunAsync("OldNumFlat", configuration, outputDirectory));
Console.WriteLine(await RunAsync("NewNumFlat", configuration, outputDirectory));

var downsampling = CompareSignals(
    Path.Combine(outputDirectory, "old_downsampled.bin"),
    Path.Combine(outputDirectory, "new_downsampled.bin"));
var upsampling = CompareSignals(
    Path.Combine(outputDirectory, "old_upsampled.bin"),
    Path.Combine(outputDirectory, "new_upsampled.bin"));

Console.WriteLine("Comparison");
PrintComparison("Downsampling", downsampling);
PrintComparison("Upsampling  ", upsampling);

if (!downsampling.IsWithinTolerance || !upsampling.IsWithinTolerance)
{
    Environment.ExitCode = 1;
}

string FindRootDirectory()
{
    for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
    {
        if (File.Exists(Path.Combine(directory.FullName, "ResampleTest.slnx")))
        {
            return directory.FullName;
        }
    }

    throw new DirectoryNotFoundException("Could not find the project root.");
}

async Task<string> RunAsync(string projectName, string buildConfiguration, string resultsDirectory)
{
    var extension = OperatingSystem.IsWindows() ? ".exe" : string.Empty;
    var executable = Path.Combine(
        rootDirectory,
        projectName,
        "bin",
        buildConfiguration,
        "net10.0",
        projectName + extension);

    if (!File.Exists(executable))
    {
        throw new FileNotFoundException($"Build output was not found: {executable}");
    }

    var startInfo = new ProcessStartInfo(executable)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    startInfo.ArgumentList.Add(resultsDirectory);

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Could not start {projectName}.");
    var standardOutput = process.StandardOutput.ReadToEndAsync();
    var standardError = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();

    if (process.ExitCode != 0)
    {
        throw new InvalidOperationException(
            $"{projectName} failed with exit code {process.ExitCode}: {await standardError}");
    }

    return (await standardOutput).TrimEnd();
}

static ComparisonResult CompareSignals(string oldPath, string newPath)
{
    using var oldReader = new BinaryReader(File.OpenRead(oldPath));
    using var newReader = new BinaryReader(File.OpenRead(newPath));

    var oldLength = oldReader.ReadInt32();
    var newLength = newReader.ReadInt32();
    if (oldLength != newLength)
    {
        return new ComparisonResult(false, oldLength, newLength, int.MaxValue, double.PositiveInfinity);
    }

    var bitwiseMismatchCount = 0;
    var maximumAbsoluteError = 0.0;
    for (var i = 0; i < oldLength; i++)
    {
        var oldValue = oldReader.ReadDouble();
        var newValue = newReader.ReadDouble();

        if (BitConverter.DoubleToInt64Bits(oldValue) != BitConverter.DoubleToInt64Bits(newValue))
        {
            bitwiseMismatchCount++;
        }

        maximumAbsoluteError = Math.Max(maximumAbsoluteError, Math.Abs(oldValue - newValue));
    }

    var filesEndedTogether = oldReader.BaseStream.Position == oldReader.BaseStream.Length
        && newReader.BaseStream.Position == newReader.BaseStream.Length;
    return new ComparisonResult(
        filesEndedTogether && maximumAbsoluteError <= ComparisonTolerance,
        oldLength,
        newLength,
        bitwiseMismatchCount,
        maximumAbsoluteError);
}

static void PrintComparison(string label, ComparisonResult result)
{
    var verdict = result.IsWithinTolerance
        ? result.BitwiseMismatchCount == 0 ? "EXACT MATCH" : $"MATCH (tolerance: {ComparisonTolerance:G})"
        : "MISMATCH";
    Console.WriteLine(
        $"  {label}: {verdict} " +
        $"(samples: {result.OldLength}/{result.NewLength}, " +
        $"bitwise mismatches: {result.BitwiseMismatchCount}, " +
        $"max abs error: {result.MaximumAbsoluteError:G17})");
}

readonly record struct ComparisonResult(
    bool IsWithinTolerance,
    int OldLength,
    int NewLength,
    int BitwiseMismatchCount,
    double MaximumAbsoluteError);
