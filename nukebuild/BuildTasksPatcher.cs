using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Nuke.Common.Tooling;

public class BuildTasksPatcher
{
    /// <summary>
    /// This helper class, avoid argument null exception
    /// when cecil write AssemblyNameDefinition on MemoryStream.
    /// </summary>
    private class Wrapper : ISymbolWriterProvider
    {
        readonly ISymbolWriterProvider _provider;
        readonly string _filename;

        public Wrapper(ISymbolWriterProvider provider, string filename)
        {
            _provider = provider;
            _filename = filename;
        }

        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName) =>
            _provider.GetSymbolWriter(module, string.IsNullOrWhiteSpace(fileName) ? _filename : fileName);

        public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream) =>
            _provider.GetSymbolWriter(module, symbolStream);
    }

    private static string GetSourceLinkInfo(string path)
    {
        try
        {
            using (var asm = AssemblyDefinition.ReadAssembly(path,
                new ReaderParameters
                {
                    ReadWrite = true,
                    InMemory = true,
                    ReadSymbols = true,
                    SymbolReaderProvider = new DefaultSymbolReaderProvider(false),
                }))
            {
                if (asm.MainModule.CustomDebugInformations?.OfType<SourceLinkDebugInformation>()?.FirstOrDefault() is { } sli)
                {
                    return sli.Content;
                }
            }
        }
        catch
        {

        }
        return null;
    }

    public static void PatchBuildTasksInPackage(string packagePath, Tool ilRepackTool)
    {
        using (var archive = new ZipArchive(File.Open(packagePath, FileMode.Open, FileAccess.ReadWrite),
            ZipArchiveMode.Update))
        {

            foreach (var entry in archive.Entries.ToList())
            {
                if (entry.Name == "Avalonia.Build.Tasks.dll")
                {
                    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempDir);
                    var temp = Path.Combine(tempDir, entry.Name);
                    var output = temp + ".output";
                    File.Copy(GetAssemblyPath(typeof(Microsoft.Build.Framework.ITask)),
                        Path.Combine(tempDir, "Microsoft.Build.Framework.dll"));
                    var patched = new MemoryStream();
                    try
                    {
                        entry.ExtractToFile(temp, true);
                        // Get Original SourceLinkInfo Content
                        var sourceLinkInfoContent = GetSourceLinkInfo(temp);

                        var cecilAsm = GetAssemblyPath(typeof(Mono.Cecil.AssemblyDefinition));
                        var cecilRocksAsm = GetAssemblyPath(typeof(Mono.Cecil.Rocks.MethodBodyRocks));
                        var cecilPdbAsm = GetAssemblyPath(typeof(Mono.Cecil.Pdb.PdbReaderProvider));
                        var cecilMdbAsm = GetAssemblyPath(typeof(Mono.Cecil.Mdb.MdbReaderProvider));

                        ilRepackTool.Invoke(
                            $"/internalize /out:\"{output}\" \"{temp}\" \"{cecilAsm}\" \"{cecilRocksAsm}\" \"{cecilPdbAsm}\" \"{cecilMdbAsm}\"",
                            tempDir);

                        // 'hurr-durr assembly with the same name is already loaded' prevention
                        using (var asm = AssemblyDefinition.ReadAssembly(output,
                            new ReaderParameters
                            {
                                ReadWrite = true,
                                InMemory = true,
                                ReadSymbols = true,
                                SymbolReaderProvider = new DefaultSymbolReaderProvider(false),
                            }))
                        {
                            asm.Name = new AssemblyNameDefinition(
                                "Avalonia.Build.Tasks."
                                + Guid.NewGuid().ToString().Replace("-", ""),
                                new Version(0, 0, 0));

                            var mainModule = asm.MainModule;

                            // If we have SourceLink info copy to patched assembly.
                            if (!string.IsNullOrEmpty(sourceLinkInfoContent))
                            {
                                mainModule.CustomDebugInformations.Add(new SourceLinkDebugInformation(sourceLinkInfoContent));
                            }

                            // Try to get SymbolWriter if it has it
                            var reader = mainModule.SymbolReader;
                            var hasDebugInfo = reader is not null;
                            var proivder = reader?.GetWriterProvider() is ISymbolWriterProvider p
                                ? new Wrapper(p, "Avalonia.Build.Tasks.dll")
                                : default(ISymbolWriterProvider);

                            var parameters = new WriterParameters
                            {
#if ISNETFULLFRAMEWORK
                                StrongNameKeyPair = signingStep.KeyPair,
#endif
                                WriteSymbols = hasDebugInfo,
                                SymbolWriterProvider = proivder,
                                DeterministicMvid = hasDebugInfo,
                            };
                            asm.Write(patched, parameters);
                            patched.Position = 0;
                        }

                    }
                    finally
                    {
                        try
                        {
                            if (Directory.Exists(tempDir))
                                Directory.Delete(tempDir, true);
                        }
                        catch
                        {
                            //ignore
                        }
                    }

                    var fn = entry.FullName;
                    entry.Delete();
                    var newEntry = archive.CreateEntry(fn, CompressionLevel.Optimal);
                    using (var s = newEntry.Open())
                        patched.CopyTo(s);
                }
            }
        }
    }

    private static string GetAssemblyPath(Type typeInAssembly)
        => typeInAssembly.Assembly.GetModules()[0].FullyQualifiedName;
}
