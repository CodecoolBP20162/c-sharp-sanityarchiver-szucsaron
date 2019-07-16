﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using SanityArchiver.service;
using SanityArchiver.prompter;
using SanityArchiver.form;
using SanityArchiver.data;

namespace SanityArchiver.fileManager
{
    class FileManager
    {
        public FileSystemRequest OnArchiveRequested;
        public FileSystemRequest OnDecompressRequested;
        public RefreshRequest OnRefreshRequested;
        public RootChangeRequest OnRootChangeRequested;
        public FileSystemRequest OnCopyRequested;
        public FileSystemRequest OnMoveRequested;

        private static readonly string PREV_DIRECTORY_SYMBOL = "..";
        private static readonly string DIRECTORY_SEPARATOR_SYMBOL = "----------------------";
        private ListBox Window;
        private TextBox PathBar;

        private ArchiveService ArchiveService;
        private FileService FileService;
        private Prompter Prompter = Prompter.GetInstance();
        private FilePathContainer Root;
        private string[] LastSelectedItems;
        private string LastSelectedItem;
        private Dictionary<string, FileSystemInfo> Files = new Dictionary<string, FileSystemInfo>();

        public delegate void FileSystemRequest(ICollection<FileSystemInfo> sources);
        public delegate void RefreshRequest();
        public delegate void RootChangeRequest(DirectoryInfo dirInfo);

        public FileManager(ListBox listBox, ArchiveService archiver, FileService fileService)
        {
            Init(listBox, archiver, fileService);
        }
        public FileManager(ListBox listBox, ArchiveService archiver, FileService fileService, FileManager fileManager)
        {
            Init(listBox, archiver, fileService);
            OnArchiveRequested = new FileSystemRequest(fileManager.Archive);
            fileManager.OnArchiveRequested = new FileSystemRequest(Archive);

            OnDecompressRequested = new FileSystemRequest(fileManager.Decompress);
            fileManager.OnDecompressRequested = new FileSystemRequest(Decompress);

            OnCopyRequested = new FileSystemRequest(fileManager.Copy);
            fileManager.OnCopyRequested = new FileSystemRequest(Copy);

            OnMoveRequested = new FileSystemRequest(fileManager.Move);
            fileManager.OnMoveRequested = new FileSystemRequest(Move);

            OnRefreshRequested = new RefreshRequest(fileManager.Refresh);
            fileManager.OnRefreshRequested = new RefreshRequest(Refresh);

            OnRootChangeRequested = new RootChangeRequest(fileManager.ChangeRoot);
            fileManager.OnRootChangeRequested = new RootChangeRequest(ChangeRoot);
        }
        public void AttachPathBar(TextBox pathBarTextBox)
        {
            PathBar = pathBarTextBox;
        }
        private void Init(ListBox listBox, ArchiveService archiver, FileService fileService)
        {
            Window = listBox;
            ArchiveService = archiver;
            ArchiveService.OnResponse = RefreshBoth;
            FileService = fileService;
            FileService.OnResponse = RefreshBoth;
            Root = FileService.Root;
            LastSelectedItems = new string[0];
        }
        public void Refresh()
        {
            try
            {
                TryRefresh();
            } 
            catch (IOException e)
            {
                Prompter.HandleError(e);
            }
            catch (NullReferenceException)
            {
                Prompter.HandleError("You are already in root");
            }
            catch (UnauthorizedAccessException e)
            {
                Prompter.HandleError(e);
            }
        }
        private void TryRefresh()
        {
            FileSystemInfo[] dirs = Root.Path.GetDirectories();
            FileSystemInfo[] files = Root.Path.GetFiles();
            FileSystemInfo[] fileSystems = new FileSystemInfo[dirs.Length + files.Length];
            Array.Copy(dirs, 0, fileSystems, 0, dirs.Length);
            Array.Copy(files, 0, fileSystems, dirs.Length, files.Length);
            Files.Clear();
            Window.Items.Clear();
            Window.Items.Add(PREV_DIRECTORY_SYMBOL);
            foreach (FileSystemInfo dirInfo in dirs)
            {
                Window.Items.Add(dirInfo.Name);
                Files.Add(dirInfo.Name, dirInfo);
            }
            Window.Items.Add(DIRECTORY_SEPARATOR_SYMBOL);
            foreach (FileSystemInfo fileInfo in files)
            {
                Window.Items.Add(fileInfo.Name);
                Files.Add(fileInfo.Name, fileInfo);
            }
            if (PathBar != null)
            {
                PathBar.Text = Root.Path.FullName;
            }
        }
        public void RefreshBoth()
        {
            Refresh();
            OnRefreshRequested();
        }
        private List<FileSystemInfo> GetSelected()
        {
            List<FileSystemInfo> selected = new List<FileSystemInfo>();
            foreach (string item in Window.SelectedItems)
            {
                try
                {
                    selected.Add(Files[item]);
                }
                catch (KeyNotFoundException)
                {
                    throw new FileManagerException("Invalid Selection");
                }
            }
            return selected;
        }
        public void OnItemDoubleClick()
        {
            Window.SelectedItems.Clear();
            Window.SelectedItem = LastSelectedItem;
            NavigateTo(LastSelectedItem);
        }

        public void OnArchiveClicked()
        {
            OnArchiveRequested(GetSelected());
        }
        public void OnDecompressClicked()
        {
            OnDecompressRequested(GetSelected());
        }
        public void OnCopyClicked()
        {
            OnCopyRequested(GetSelected());
        }
        public void OnMoveClicked()
        {
            OnMoveRequested(GetSelected());
        }
        public void OnSetAttributeClicked()
        {
            FileService.SetAttribute(GetSelected());
        }
        public void OnChangeDriveClicked()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            DriveForm df = new DriveForm(drives, OnChangeDriveResponse);
        }
        private void OnChangeDriveResponse(string drive)
        {
            Root.Path = new DirectoryInfo(drive);
            Refresh();
        }
        public void OnSelectionChanged()
        {
            foreach (string item in Window.SelectedItems)
            {
                if (!LastSelectedItems.Contains(item))
                {
                    LastSelectedItem = item;
                }
            }
            LastSelectedItems = new string[Window.SelectedItems.Count];
            int i = 0;
            foreach (string item in Window.SelectedItems)
            {
                LastSelectedItems[i] = item;
                i++;
            }
        }
        public void OnAlignRootClicked()
        {
            OnRootChangeRequested(Root.Path);
        }
        private void NavigateTo(String dirName)
        {
            if (dirName.Equals(PREV_DIRECTORY_SYMBOL))
            {
                Root.Path = Root.Path.Parent;
            } else
            {
                try
                {
                    DirectoryInfo dirInfo = (DirectoryInfo)Files[dirName];
                    Root.Path = new DirectoryInfo(dirInfo.FullName);
                }
                catch (InvalidCastException) {}
                catch (KeyNotFoundException) { }
            }
            Refresh();
        }
        public void ChangeRoot(DirectoryInfo dirInfo)
        {
            Root.Path = dirInfo;
            Refresh();
        }

        public void NavigateTo(DirectoryInfo dirInfo)
        {
            Root.Path = dirInfo;
            Refresh();
        }
        public void Archive(ICollection<FileSystemInfo> sources)
        {
             ArchiveService.Archive(sources, Root.Path);
        }
        public void Decompress(ICollection<FileSystemInfo> sources)
        {
             ArchiveService.Decompress(sources, Root.Path);
        }
        public void Move(ICollection<FileSystemInfo> items)
        {
            FileService.Move(items, Root.Path);
        }
        public void Copy(ICollection<FileSystemInfo> items)
        {
            
           FileService.Copy(items, Root.Path);
        }
        
    }
}
