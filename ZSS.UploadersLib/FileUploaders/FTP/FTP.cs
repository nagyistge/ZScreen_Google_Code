﻿#region License Information (GPL v2)
/*
    ZScreen - A program that allows you to upload screenshots in one keystroke.
    Copyright (C) 2008-2009  Brandon Zimmerman

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
    
    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using Starksoft.Net.Ftp;
using Starksoft.Net.Proxy;

namespace UploadersLib
{
    public sealed class FTP : IDisposable
    {
        public delegate void FTPProgressEventHandler(float percentage);

        public event FTPProgressEventHandler ProgressChanged;

        public FTPAccount Account { get; set; }

        public FtpClient Client { get; set; }

        public bool AutoReconnect { get; set; }

        public FTP(FTPAccount account)
        {
            this.Account = account;
            this.Client = new FtpClient();

            Client.Host = account.Server;
            Client.Port = account.Port;
            Client.DataTransferMode = account.IsActive ? TransferMode.Active : TransferMode.Passive;

            if (Uploader.ProxySettings != null)
            {
                IProxyClient proxy = Uploader.ProxySettings.GetProxyClient;
                if (proxy != null)
                {
                    Client.Proxy = proxy;
                }
            }

            Client.TransferProgress += new EventHandler<TransferProgressEventArgs>(OnTransferProgressChanged);
            Client.ConnectionClosed += new EventHandler<ConnectionClosedEventArgs>(Client_ConnectionClosed);
        }

        private void OnTransferProgressChanged(object sender, TransferProgressEventArgs e)
        {
            if (ProgressChanged != null)
            {
                /*Console.WriteLine("{0}/{1} - {2}% - {3} - {4}", e.TotalBytesTransferred / 1024, e.TotalBytes / 1024, e.Percentage,
                   e.EstimatedCompleteTime.TotalMilliseconds, e.ElapsedTime.TotalMilliseconds);*/
                ProgressChanged(e.Percentage);
            }
        }

        private void Client_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
        {
            if (AutoReconnect)
            {
                Connect();
            }
        }

        public void Connect(string username, string password)
        {
            if (!Client.IsConnected)
            {
                Client.Open(username, password);
            }
        }

        public void Connect()
        {
            Connect(Account.Username, Account.Password);
        }

        public void Disconnect()
        {
            Client.Close();
        }

        public void UploadData(Stream stream, string remotePath)
        {
            Connect();
            Client.PutFile(stream, remotePath, FileAction.Create);
        }

        public void UploadData(byte[] data, string remotePath)
        {
            using (MemoryStream stream = new MemoryStream(data, false))
            {
                UploadData(stream, remotePath);
            }
        }

        public void UploadFile(string localPath, string remotePath)
        {
            using (FileStream stream = new FileStream(localPath, FileMode.Open))
            {
                UploadData(stream, remotePath);
            }
        }

        public void UploadImage(Image image, string remotePath)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                image.Save(stream, image.RawFormat);
                UploadData(stream, remotePath);
            }
        }

        public void UploadText(string text, string remotePath)
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(text), false))
            {
                UploadData(stream, remotePath);
            }
        }

        public void DownloadFile(string remotePath, string localPath)
        {
            Connect();
            Client.GetFile(remotePath, localPath);
        }

        public void DownloadFile(string remotePath, Stream outStream)
        {
            Connect();
            Client.GetFile(remotePath, outStream, false);
        }

        public FtpItemCollection GetDirList(string remotePath)
        {
            Connect();
            return Client.GetDirList(remotePath);
        }

        public void Test(string remotePath)
        {
            Connect();
            remotePath = FTPHelpers.AddSlash(remotePath, FTPHelpers.SlashType.Prefix);
            Client.ChangeDirectory(remotePath);
        }

        public void MakeDirectory(string remotePath)
        {
            Connect();
            Client.MakeDirectory(remotePath);
        }

        public void MakeMultiDirectory(string remotePath)
        {
            List<string> paths = FTPHelpers.GetPaths(remotePath);

            foreach (string path in paths)
            {
                try
                {
                    MakeDirectory(path);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public void Rename(string fromRemotePath, string toRemotePath)
        {
            Connect();
            Client.Rename(fromRemotePath, toRemotePath);
        }

        public void DeleteFile(string remotePath)
        {
            Connect();
            Client.DeleteFile(remotePath);
        }

        public void DeleteDirectory(string remotePath)
        {
            Connect();

            string filename = FTPHelpers.GetFileName(remotePath);
            if (filename == "." || filename == "..")
            {
                return;
            }

            FtpItemCollection files = GetDirList(remotePath);

            foreach (FtpItem file in files)
            {
                if (file.ItemType == FtpItemType.Directory)
                {
                    DeleteDirectory(file.FullPath);
                }
                else
                {
                    DeleteFile(file.FullPath);
                }
            }

            Client.DeleteDirectory(remotePath);
        }

        public bool SendCommand(string command)
        {
            try
            {
                Client.Quote(command);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            Disconnect();
            Client.Dispose();
        }
    }
}