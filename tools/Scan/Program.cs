using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

var folder = Path.GetFullPath(args[0]);
var manifest = XDocument.Load(Path.Combine(folder, "manifest.xml"));
var dlls = manifest.Root.Element("Assemblies").Elements("Assembly")
    .Select(e => Path.Combine(folder, e.Value)).ToArray();
var managed = Path.GetFullPath(args[1]);
var gameAssemblies = System.IO.Directory.EnumerateFiles(managed, "*.dll", SearchOption.AllDirectories)
    .Select(Path.GetFileNameWithoutExtension).ToArray();
var scan = ModScanner.Scan(dlls, allowedReferences: gameAssemblies, folderPath: folder, localFolder: false);
Console.WriteLine($"Methods: {scan.MethodsScanned}; Refused: {scan.Refused}; Capabilities: {string.Join(",", scan.Capabilities)}");
Console.WriteLine(scan.Error);
Console.WriteLine(scan.FormatFindings(100));
return scan.Refused || scan.Capabilities.Count != 0 ? 1 : 0;
