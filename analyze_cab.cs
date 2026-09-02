using System.Runtime.InteropServices;
namespace analyze_cab {
    class analyze_cab {
        static byte[] mscf_magic_bytes = { 77, 83, 67, 70 }; // MSCF
        struct cfExtra
        {
            public ushort CBCFHeader; // size of abReserve field in the CFHeader in bytes (optional)
            public byte CBCFFolder;  // size of abReserve field in each CFFolder entry in bytes (optional)
            public byte CBCFData;  // size of abReserve field in each CFData entry in bytes (optional)
        }
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
        struct cfFolder {
            public uint COFFCabStart; // offset of the first CFDATA block in this folder
            public ushort CCFData; // number of CFDATA blocks in this folder
            public ushort TypeCompress; // compression type indicator
        }

        struct cfFile {
            public uint CBFile; // uncompressed size of this file in bytes
            public uint UOffFolderStart; // uncompressed offset of this file in the folder
            public ushort IFolder; // index into the CFFOLDER area
            public ushort _date; // date stamp for this file
            public ushort _time; // time stamp for this file
            public short attribs; // attribute flags for this file
        }

        // just used for internal data keeping
        struct folder_internal
        {
            public string compression_name;
            public cfFolder folder_data;
        }
        
        struct file_internal
        {
            public string file_name;
            public cfFile file_data;
        }
        public enum HeaderFlags : ushort
        {
            PrevCabinet = 1 << 0,
            NextCabinet = 1 << 1,
            ReservePresent = 1 << 2
        }
        public enum compTypes : ushort
        {
        compMask = 0xf,
        compNone = 0x0,
        compMSZIP = 0x1,
        compQuantum = 0x2,
        compLZX = 0x3,
        }
        struct cfData {
            public uint Checksum; // checksum of this CFDATA entry
            public ushort CBData; // number of compressed bytes in this block
            public ushort CBUncomp; // number of uncompressed bytes in this block
        }

        static int find_cab_offset(FileStream file_handle)
        {
            int chunk_size = 4096;
            int bytes_read;
            int curr_index = 0;
            byte[] curr_chunk = new byte[chunk_size];
            while((bytes_read = file_handle.Read(curr_chunk,0,chunk_size)) > 0)
            {
                int index_of = curr_chunk.AsSpan().IndexOf(mscf_magic_bytes);
                if (index_of != -1) 
                {
                    int location = curr_index + index_of;
                    return location;
                }
                curr_index += chunk_size;
            }
            return -1;
        }

        static ret_type deserialize_from_file<ret_type>(FileStream file_handle)
        {
            int size = Marshal.SizeOf<ret_type>();
            byte[] bytes_read = new byte[size];
            file_handle.ReadExactly(bytes_read, 0, size);

            IntPtr alloc_mem = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes_read, 0, alloc_mem, size);
            return Marshal.PtrToStructure<ret_type>(alloc_mem);
        }

        static List<folder_internal> get_folders(FileStream file_handle, cfHeader header)
        {
            List<folder_internal> folders = new List<folder_internal>();
            for (int i = 0; i < header.CFolders; i++)
            {
                cfFolder folder = deserialize_from_file<cfFolder>(file_handle);
                ushort compression_method = (ushort)(folder.TypeCompress & (ushort)compTypes.compMask); // masks out the upper bits
                string _comp_name = ((compTypes)compression_method).ToString();
                folders.Add(new folder_internal { compression_name = _comp_name, folder_data = folder });

            }
            return folders;
        }
        static List<file_internal> get_files(FileStream file_handle, cfHeader header)
        {
            List<file_internal> files = new List<file_internal>();
            file_internal file_single;

            for (int i = 0; i < header.CFiles; i++)
            {
                cfFile file = deserialize_from_file<cfFile>(file_handle);
                string file_name = "";
                int curr_char;
                while((curr_char = file_handle.ReadByte()) != 0x00)
                {
                    file_name += (char)curr_char;
                }
                file_single.file_name = file_name;
                file_single.file_data = file;
                files.Add(file_single);
            }
            return files;
            }
        static cfHeader read_header(FileStream file_handle)
        {
            cfHeader exe_header = deserialize_from_file<cfHeader>(file_handle);
            if ((exe_header.Flags & (ushort)HeaderFlags.ReservePresent) != 0)
            {
                cfExtra extra = deserialize_from_file<cfExtra>(file_handle);
                Console.WriteLine($"Skipping {extra.CBCFHeader} bytes...");
                file_handle.Position += extra.CBCFHeader;
            }
            return exe_header;
        }
        static int Main()
        {
            string exe_path = @"C:\path\to\cab.exe";
            FileStream file_handle = System.IO.File.OpenRead(exe_path);
            int mscf_offset = find_cab_offset(file_handle);
            if (mscf_offset == -1)
            {
                Console.WriteLine("Failed to find MSCF Bytes");
                return 1;
            }
            Console.WriteLine($"MSCF found at : 0x{mscf_offset:x}");
            file_handle.Position = mscf_offset;

            cfHeader exe_header = read_header(file_handle);

            Console.WriteLine($"Version : {exe_header.VersionMajor}.{exe_header.VersionMinor}");
            Console.WriteLine($"Number of files : {exe_header.CFiles}");
            Console.WriteLine($"Number of folders : {exe_header.CFolders}");
            List<folder_internal> folders = get_folders(file_handle,exe_header);
            List<file_internal> files = get_files(file_handle, exe_header);
            file_handle.Close();
            int i = 0;
            foreach(file_internal file in files)
            {
                string compression_name = folders[file.file_data.IFolder].compression_name;
                Console.WriteLine($"{file.file_name} (folder_index={file.file_data.IFolder}, compression={compression_name}, index={i}, size=0x{file.file_data.CBFile:x})");
                i++;
            }
            return 0;
        }
    }
}