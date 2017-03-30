using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DokanNet;
using SevenZip;

namespace Shaman.Dokan
{
    public class SevenZipFs : ReadOnlyFs
    {

        public SevenZipExtractor extractor;
        private FsNode<ArchiveFileInfo> root;
        public SevenZipFs(string path)
        {
            zipfile = path;
            extractor = new SevenZipExtractor(path);
            root = CreateTree<ArchiveFileInfo>(extractor.ArchiveFileData, x => x.FileName, x => IsDirectory(x.Attributes));


            cache = new MemoryStreamCache<FsNode<ArchiveFileInfo>>((item, stream) =>
            {
                lock (readerLock)
                {
                    extractor.ExtractFile(item.Info.Index, stream);
                }
            });
        }
        private string zipfile;
        public override string SimpleMountName => "SevenZipFs-" + zipfile;

        private object readerLock = new object();

        public override NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {

            if (IsBadName(fileName)) return NtStatus.ObjectNameInvalid;
            if ((access & ModificationAttributes) != 0) return NtStatus.DiskFull;

            var item = GetFile(fileName);
            if (item == null) return DokanResult.FileNotFound;
            if (item.Info.FileName != null && !item.Info.IsDirectory)
            {
                if ((access & DokanNet.FileAccess.ReadData) != 0)
                {
                    Console.WriteLine("ReadData: " + fileName);
                    info.Context = cache.OpenStream(item, (long)item.Info.Size);

                }
                return NtStatus.Success;
            }
            else
            {
                info.IsDirectory = true;
                return NtStatus.Success;
            }
        }

        
        private MemoryStreamCache<FsNode<ArchiveFileInfo>> cache;
        public override NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            var item = GetFile(fileName);
            if (item == null)
            {
                fileInfo = default(FileInformation);
                return DokanResult.FileNotFound;
            }
            fileInfo = GetFileInformation(item);
            return NtStatus.Success;
        }

        public override NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info)
        {
            fileSystemName = volumeLabel = "SevenZipFs";
            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.ReadOnlyVolume | FileSystemFeatures.UnicodeOnDisk | FileSystemFeatures.VolumeIsCompressed;
            return NtStatus.Success;
        }

        private FsNode<ArchiveFileInfo> GetFile(string fileName)
        {
            return GetNode(root, fileName);
        }

        protected override IList<FileInformation> FindFilesHelper(string fileName, string searchPattern)
        {
            var item = GetFile(fileName);
            if (item == null) return null;

            if (item == root || IsDirectory(item.Info.Attributes))
            {
                if (item.Children == null) return new FileInformation[] { };
                var matcher = GetMatcher(searchPattern);
                return item.Children.Where(x => matcher(x.Name)).Select(x => GetFileInformation(x)).ToList();
            }
            return null;
        }

        private FileInformation GetFileInformation(FsNode<ArchiveFileInfo> item)
        {
            return new FileInformation()
            {
                Attributes = item == root ? FileAttributes.Directory : (FileAttributes)item.Info.Attributes,
                CreationTime = item.Info.CreationTime,
                FileName = item.Name,
                LastAccessTime = item.Info.LastAccessTime,
                LastWriteTime = item.Info.LastWriteTime,
                Length = (long)item.Info.Size
            };
        }

        public override void Cleanup(string fileName, DokanFileInfo info)
        {
        }
    }
}
