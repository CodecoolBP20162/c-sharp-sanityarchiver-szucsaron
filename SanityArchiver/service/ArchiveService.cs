﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SanityArchiver.archiver;
using SanityArchiver.forms;


namespace SanityArchiver.service
{
    class ArchiveService
    {
        private ICollection<FileSystemInfo> SentSources;
        private IArchiver Archiver;
        public delegate void RefreshHandler();
        public RefreshHandler OnResponse;
        private DirectoryInfo RootDirInfo;

        public ArchiveService(IArchiver archiver)
        {
            Archiver = archiver;
        }
        public void Archive(ICollection<FileSystemInfo> sources, DirectoryInfo rootDirInfo)
        {
            RootDirInfo = rootDirInfo;
            SentSources = sources;
            ArchiverForm af = new ArchiverForm
                (OnArchiveNameInputResponse,
                 SentSources.ElementAt(0).Name + ".zip");
            af.Show();
        }
        public void OnArchiveNameInputResponse(string input, string password)
        {
            if (password != "")
            {
                Archiver.SetEncryption(password);
            }
            Archiver.CompressItems(SentSources, RootDirInfo + "\\" + input);
            Archiver.DisableEncryption();
            OnResponse();
        }
        public void Decompress(ICollection<FileSystemInfo> sources, DirectoryInfo rootDirInfo)
        {
            RootDirInfo = rootDirInfo;
            SentSources = sources;
            ArchiverForm af = new ArchiverForm
                (OnDecompressInputResponse,
                 SentSources.ElementAt(0).Name);
            af.Show();
        }
        public void OnDecompressInputResponse(string input, string password)
        {
            if (password != "")
            {
                Archiver.SetEncryption(password);
            }
            Archiver.DecompressItem(SentSources.ElementAt(0), RootDirInfo + "\\" + input);
            Archiver.DisableEncryption();
            OnResponse();
        }
    }
}