## 7zip mounter
Mounts 7zip/ZIP/RAR files.

Usage: `Shaman.Dokan.Archive.exe <path-to-archive> [--open]`

The archive is mounted in `C:\DokanFs\path-to-archive`. When `--open` is specified, the mounted folder is opened in Explorer.

Supports all the formats supported by SevenZipSharp.

Requires [Dokan](http://dokan-dev.github.io/).
