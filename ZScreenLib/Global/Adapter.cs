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
using System.IO;
using System.Net;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Taskbar;
using UploadersLib;
using UploadersLib.FileUploaders;
using UploadersLib.Helpers;
using UploadersLib.ImageUploaders;
using UploadersLib.TextUploaders;
using UploadersLib.URLShorteners;
using ZScreenLib.Properties;
using ZSS;

namespace ZScreenLib
{
    /// <summary>
    /// Class for public static methods for use in ZScreen
    /// </summary>
    public static class Adapter
    {
    	public static bool Windows7TaskbarIntegrationEnabled(){
    		return Program.conf.ShowInTaskbar && Program.conf.MinimizeOnClose;
    	}
    	
        public static void AddToClipboardByDoubleClick(Control tp)
        {
            Control ctl = tp.GetNextControl(tp, true);
            while (ctl != null)
            {
                if (ctl.GetType() == typeof(TextBox))
                {
                    ctl.DoubleClick += TextBox_DoubleClick;
                }
                ctl = tp.GetNextControl(ctl, true);
            }
        }

        public static void TextBox_DoubleClick(object sender, EventArgs e)
        {
            TextBox tb = ((TextBox)sender);
            if (!string.IsNullOrEmpty(tb.Text))
            {
                Clipboard.SetText(tb.Text);
            }
        }

        public static string GetTinyPicShuk()
        {
            UserPassBox ub = new UserPassBox("Enter TinyPic Email Address and Password", string.IsNullOrEmpty(Program.conf.TinyPicUserName) ? "someone@gmail.com" : Program.conf.TinyPicUserName, Program.conf.TinyPicPassword) { Icon = Resources.zss_main };
            ub.ShowDialog();
            if (ub.DialogResult == DialogResult.OK)
            {
                TinyPicUploader tpu = new TinyPicUploader(Program.TINYPIC_ID, Program.TINYPIC_KEY, UploadMode.API);
                tpu.ProxySettings = Adapter.GetProxySettings();
                if (Program.conf.RememberTinyPicUserPass)
                {
                    Program.conf.TinyPicUserName = ub.UserName;
                    Program.conf.TinyPicPassword = ub.Password;
                }
                return tpu.UserAuth(ub.UserName, ub.Password);
            }
            return string.Empty;
        }

        #region ImageBam Methods

        public static string CreateImageBamGallery()
        {
            ImageBamUploader ibu = new ImageBamUploader(new ImageBamUploaderOptions(Program.conf.ImageBamApiKey, Program.conf.ImageBamSecret));
            string galleryId = ibu.CreateGalleryID();
            Program.conf.ImageBamGallery.Add(galleryId);
            return galleryId;
        }

        public static string GetImageBamGalleryActive()
        {
            string galleryId = string.Empty;
            if (CheckImageBamGallery())
            {
                galleryId = Program.conf.ImageBamGallery[Program.conf.ImageBamGalleryActive];
            }
            return galleryId;
        }

        #endregion

        public static void TestFTPAccount(FTPAccount acc)
        {
            string msg, path = FTPHelpers.CombineURL(acc.FTPAddress, acc.Path);

            FTPOptions options = new FTPOptions { Account = acc, ProxySettings = GetProxySettings() };
            FTPAdapter ftpClient = new FTPAdapter(options);

            try
            {
                ftpClient.ListDirectory(path);
                msg = "Success!";
            }
            catch (WebException e)
            {
                string status = ((FtpWebResponse)e.Response).StatusDescription;
                if (status.StartsWith("550") && acc.AutoCreateFolder)
                {
                    try
                    {
                        ftpClient.MakeMultiDirectory(acc.Path);
                        ftpClient.ListDirectory(path);
                        msg = "Success!\nAuto created folders: " + acc.Path;
                    }
                    catch (WebException e2)
                    {
                        msg = GetWebExceptionMessage(e2);
                    }
                }
                else
                {
                    msg = GetWebExceptionMessage(e);
                }
            }

            if (!string.IsNullOrEmpty(msg))
            {
                MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static string GetWebExceptionMessage(WebException e)
        {
            string status = ((FtpWebResponse)e.Response).StatusDescription;
            return string.Format("Status description:\n{0}\nException message:\n{1}", status, e.Message);
        }

        public static bool CheckList<T>(List<T> list, int selected)
        {
            return list.Count > 0 && selected >= 0 && list.Count > selected;
        }

        public static bool CheckTextUploaders()
        {
            return CheckList(Program.conf.TextUploadersList, Program.conf.TextUploaderSelected);
        }

        public static bool CheckURLShorteners()
        {
            return CheckList(Program.conf.UrlShortenersList, Program.conf.UrlShortenerSelected);
        }

        public static bool CheckFTPAccounts()
        {
            return CheckList(Program.conf.FTPAccountList, Program.conf.FTPSelected);
        }

        public static bool CheckFTPAccounts(ref WorkerTask task)
        {
            bool result = CheckFTPAccounts();
            if (!result) task.Errors.Add("An FTP account does not exist or not selected properly.");
            return result;
        }

        public static bool CheckDekiWikiAccounts()
        {
            return CheckList(Program.conf.DekiWikiAccountList, Program.conf.DekiWikiSelected);
        }

        public static bool CheckDekiWikiAccounts(ref WorkerTask task)
        {
            bool result = CheckDekiWikiAccounts();
            if (!result) task.Errors.Add("A Mindtouch account does not exist or not selected properly.");
            return result;
        }

        public static bool CheckImageBamGallery()
        {
            return CheckList(Program.conf.ImageBamGallery, Program.conf.ImageBamGallerySelected);
        }

        public static void TestDekiWikiAccount(DekiWikiAccount acc)
        {
            string msg = "Success!";

            try
            {
                DekiWiki connector = new DekiWiki(new DekiWikiOptions(acc, GetProxySettings()));
                connector.Login();
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            if (!string.IsNullOrEmpty(msg))
            {
                MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Returns a WebProxy object based on active ProxyInfo and if Proxy is enabled, returns default system proxy otherwise
        /// </summary>
        public static IWebProxy GetProxySettings()
        {
            if (Program.conf.ProxyEnabled)
            {
                ProxyInfo acc = Program.conf.ProxyActive;
                if (acc != null)
                {
                    NetworkCredential cred = new NetworkCredential(acc.UserName, acc.Password);
                    return new WebProxy(acc.GetAddress(), true, null, cred);
                }
            }
            return WebRequest.DefaultWebProxy;
        }

        public static void TestProxyAccount(ProxyInfo acc)
        {
            string msg = "Success!";

            try
            {
                NetworkCredential cred = new NetworkCredential(acc.UserName, acc.Password);
                WebProxy wp = new WebProxy(acc.GetAddress(), true, null, cred);
                WebClient wc = new WebClient { Proxy = wp };
                wc.DownloadString("http://www.google.com");
            }
            catch (Exception ex)
            {
                msg = ex.Message;
            }

            if (!string.IsNullOrEmpty(msg))
            {
                MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Quick Method to shorten a URL
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string TryShortenURL(string url)
        {
            if (!string.IsNullOrEmpty(url) && (Program.conf.LimitLongURL == 0 || Program.conf.LimitLongURL > 0 && url.Length > Program.conf.LimitLongURL ||
                Program.conf.ClipboardUriMode == ClipboardUriType.FULL_TINYURL))
            {
                TextUploader tu = Program.conf.UrlShortenersList[Program.conf.UrlShortenerSelected];
                tu.ProxySettings = Adapter.GetProxySettings();
                if (tu != null)
                {
                    string temp = tu.UploadText(TextInfo.FromString(url));
                    if (!string.IsNullOrEmpty(temp))
                    {
                        url = temp;
                    }
                }
            }
            return url;
        }

        /// <summary>
        /// Method to update TinyPic Shuk; Run periodically
        /// </summary>
        public static void UpdateTinyPicShuk()
        {
            if (Program.conf.RememberTinyPicUserPass && !string.IsNullOrEmpty(Program.conf.TinyPicUserName) &&
                !string.IsNullOrEmpty(Program.conf.TinyPicPassword))
            {
                TinyPicUploader tpu = new TinyPicUploader(Program.TINYPIC_ID, Program.TINYPIC_KEY, UploadMode.API);
                tpu.ProxySettings = Adapter.GetProxySettings();
                string shuk = tpu.UserAuth(Program.conf.TinyPicUserName, Program.conf.TinyPicPassword);
                if (!string.IsNullOrEmpty(shuk))
                {
                    if (Program.conf.TinyPicShuk != shuk)
                    {
                        FileSystem.AppendDebug(string.Format("Updated TinyPic Shuk from {0} to {1}", Program.conf.TinyPicShuk, shuk));
                    }
                    Program.conf.TinyPicShuk = shuk;
                }
            }
        }

        public static UserPassBox SendSpaceRegister()
        {
            UserPassBox upb = new UserPassBox("SendSpace Registration...", "John Doe", "john.doe@gmail.com", "JohnDoe", "");
            upb.ShowDialog();
            if (upb.DialogResult == DialogResult.OK)
            {
                SendSpace sendSpace = new SendSpace();
                upb.Success = sendSpace.AuthRegister(upb.UserName, upb.FullName, upb.Email, upb.Password);
                if (!upb.Success && sendSpace.Errors.Count > 0)
                {
                    MessageBox.Show(sendSpace.ToErrorString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            return upb;
        }

        public static bool MakeTinyURL()
        {
            // LimitLongURL = 0 means make tinyURL always
            return Program.conf.MakeTinyURL || Program.conf.ClipboardUriMode == ClipboardUriType.FULL_TINYURL;
        }

        public static TextUploader FindTextUploader(string name)
        {
            if (name.Equals(PastebinUploader.Hostname))
            {
                return new PastebinUploader();
            }
            else if (name.Equals(PastebinCaUploader.Hostname))
            {
                return new PastebinCaUploader();
            }
            else if (name.Equals(Paste2Uploader.Hostname))
            {
                return new Paste2Uploader();
            }
            else if (name.Equals(SlexyUploader.Hostname))
            {
                return new SlexyUploader();
            }
            else if (name.Equals(SniptUploader.Hostname))
            {
                return new SniptUploader();
            }
            else
            {
                if (name == UploadersLib.FTPUploader.Hostname)
                {
                    if (Program.conf.FTPAccountList.Count > 0)
                    {
                        FTPAccount acc = new FTPAccount();
                        if (Program.conf.FTPSelected >= 0)
                        {
                            acc = Program.conf.FTPAccountList[Program.conf.FTPSelected];
                        }
                        else
                        {
                            acc = Program.conf.FTPAccountList[0];
                        }
                        return new UploadersLib.FTPUploader(acc);
                    }
                }
            }
            return null;
        }

        public static TextUploader FindUrlShortener(string name)
        {
            switch (name)
            {
                case BitlyUploader.Hostname:
                    return new BitlyUploader();
                case IsgdUploader.Hostname:
                    return new IsgdUploader();
                case TinyURLUploader.Hostname:
                    return new TinyURLUploader();
                case ThreelyUploader.Hostname:
                    return new ThreelyUploader();
                case KlamUploader.Hostname:
                    return new KlamUploader();
            }
            return null;
        }

        public static bool ImageSoftwareEnabled()
        {
            if (Program.conf.ImageEditor == null) return false;
            return Program.DISABLED_IMAGE_EDITOR != Program.conf.ImageEditor.Name;
        }

        public static void DeleteFile(string fp)
        {
            if (File.Exists(fp))
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(fp, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                                                                   Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }
        }

        #region "Windows 7 only"

        public static void TaskbarSetProgressState(TaskbarProgressBarState tbps)
        {
            if (TaskbarManager.IsPlatformSupported && Program.zWindowsTaskbar != null)
            {
                Program.zWindowsTaskbar.SetProgressState(tbps);
            }
        }

        public static void TaskbarSetProgressValue(int progress)
        {
            if (TaskbarManager.IsPlatformSupported && Program.zWindowsTaskbar != null)
            {
                Program.zWindowsTaskbar.SetProgressValue(progress, 100);
            }
        }

        public static void AddRecentItem(string filePath)
        {
            if (Program.zJumpList != null)
            {
                try
                {
                    Program.zJumpList.AddToRecent(filePath);
                    Program.zJumpList.KnownCategoryToDisplay = JumpListKnownCategoryType.Recent;
                    Program.zJumpList.Refresh();
                }
                catch (Exception ex)
                {
                    FileSystem.AppendDebug(ex);
                }
            }
        }

        #endregion

    }
}