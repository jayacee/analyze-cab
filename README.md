# analyze-cab
Minimal C# code to list data from a CAB file. Displays CAB version, file names and sizes, compression types, etc.

Based on [go-cabfile](https://github.com/google/go-cabfile).
# What is a CAB File?
A CAB file is a compressed file format to distribute software. It allows developers to bundle resources such as images and audio with an executable in a compressed and efficient format. Since CAB files are interpreted as EXEs and efficiently store resources in a single file, they are often used to make standalone EXEs for easy distribution.

# How are they structured?
## cfHeader (header)
CAB files begin with a **36 byte header**. This header contains:
- The MSCF signature
- The size of the CAB file
- Offset of the first file data entry
- Version of CAB used
- Number of folders and files in the CAB file
- Flags
- Cab ID
- Number of cabinet files in a set (if this CAB is bundled with others)
```
struct cfHeader {
    public uint Signature; // the MSCF bytes
    public uint Reserved1; 
    public uint CBCabinet; // size of cabinet file
    public uint Reserved2;
    public uint COFFFiles; // offset of the first CFFILE entry
    public uint Reserved3; // reserved
    public byte VersionMinor;  // cabinet file format version, minor
    public byte VersionMajor;  // cabinet file format version, major
    public ushort CFolders;    // number of CFFOLDER entries in this cabinet
    public ushort CFiles; // number of CFFILE entries in this cabinet
    public ushort Flags; // cabinet file option indicators
    public ushort SetID; // must be the same for all cabinets in a set
    public ushort ICabinet; // number of this cabinet file in a set
};
```
There are also 3 optional fields following this main header:
```
struct cfExtra
{
    public ushort CBCFHeader; // size of abReserve field in the CFHeader in bytes (optional)
    public byte CBCFFolder;  // size of abReserve field in each CFFolder entry in bytes (optional)
    public byte CBCFData;  // size of abReserve field in each CFData entry in bytes (optional)
}
```

## cfFolder (folders)
Following the header, there is a series of **8-byte folder structures**. These folder structures contain:
- The offset of the first data block in this folder
- Number of data blocks in this folder
- Compression type of this folder
```
struct cfFolder {
    public uint COFFCabStart; // offset of the first CFDATA block in this folder
    public ushort CCFData; // number of CFDATA blocks in this folder
    public ushort TypeCompress; // compression type indicator
}
```
## cfFile (file)
Following the folder entries, there is a series of **16-byte file structures**. These file structures contain:
- Uncompressed size of the file
- Uncompressed offset of this file in the folder
- Folder index
- Date & Time Stamp for the file
- Attribute flags for the file

```
struct cfFile {
    public uint CBFile; // uncompressed size of this file in bytes
    public uint UOffFolderStart; // uncompressed offset of this file in the folder
    public ushort IFolder; // index into the CFFOLDER area
    public ushort _date; // date stamp for this file
    public ushort _time; // time stamp for this file
    }
```
