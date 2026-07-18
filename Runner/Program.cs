using Zx;
using NumFlat;
using NumFlat.IO;

var config = Path.GetDirectoryName(AppContext.BaseDirectory);
config = Path.GetDirectoryName(config);
config = Path.GetFileName(config);

var root = Path.GetDirectoryName(AppContext.BaseDirectory);
root = Path.GetDirectoryName(root);
root = Path.GetDirectoryName(root);
root = Path.GetDirectoryName(root);
root = Path.GetDirectoryName(root);

var oldNumFlatDir = Path.Combine(root, "OldNumFlat", "bin", config, "net10.0");
var oldNumFlatExe = Path.Combine(oldNumFlatDir, "OldNumFlat.exe");
await oldNumFlatExe;

var newNumFlatDir = Path.Combine(root, "NewNumFlat", "bin", config, "net10.0");
var newNumFlatExe = Path.Combine(newNumFlatDir, "NewNumFlat.exe");
await newNumFlatExe;

var oldNumFlatSignal = WaveFile.ReadMono("old_resampled.wav").Data;
var newNumFlatSignal = WaveFile.ReadMono("new_resampled.wav").Data;
var error = (oldNumFlatSignal - newNumFlatSignal).Select(Math.Abs).Max();
Console.WriteLine(error);
