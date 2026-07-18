using Zx;

var config = Path.GetDirectoryName(AppContext.BaseDirectory);
config = Path.GetDirectoryName(config);
config = Path.GetFileName(config);

var root = Path.GetDirectoryName(AppContext.BaseDirectory);
root = Path.GetDirectoryName(root);
root = Path.GetDirectoryName(root);
root = Path.GetDirectoryName(root);
root = Path.GetDirectoryName(root);

var oldNumFlatPath = Path.Combine(root, "OldNumFlat", "bin", config, "net10.0", "OldNumFlat.exe");
await oldNumFlatPath;

var newNumFlatPath = Path.Combine(root, "NewNumFlat", "bin", config, "net10.0", "NewNumFlat.exe");
await newNumFlatPath;
