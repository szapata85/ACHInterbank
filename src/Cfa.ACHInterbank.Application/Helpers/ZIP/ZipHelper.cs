using System.IO.Compression;

namespace Cfa.ACHInterbank.Application.Helpers.ZIP;

public static class ZipHelper
{
    public static byte[] ZipContend(byte[] data, string EntryFileName = "data")
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

    public static (byte[], string) UnZipContend(byte[] compressedData)
    {
        using (MemoryStream input = new MemoryStream(compressedData))
        using (ZipArchive zip = new ZipArchive(input, ZipArchiveMode.Read))
        {
            ZipArchiveEntry entry = zip.Entries[0];
            using (Stream entryStream = entry.Open())
            using (MemoryStream output = new MemoryStream())
            {
                entryStream.CopyTo(output);
                return (output.ToArray(), entry.FullName);
            }
        }
    }
}
