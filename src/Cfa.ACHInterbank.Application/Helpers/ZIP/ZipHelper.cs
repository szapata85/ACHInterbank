using System.IO.Compression;

namespace Cfa.ACHInterbank.Application.Helpers.ZIP;

public static class ZipHelper
{
    public static byte[] CoprimeContend(byte[] data, string EntryFileName = "data")
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (ZipArchive zip = new ZipArchive(output, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry entry = zip.CreateEntry(EntryFileName);
                using (Stream entryStream = entry.Open())
                {
                    entryStream.Write(data, 0, data.Length);
                }
            }
            return output.ToArray();
        }
    }
}
