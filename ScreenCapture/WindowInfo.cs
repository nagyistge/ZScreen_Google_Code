﻿#region License Information (GPL v2)

/*
    ZUploader - A program that allows you to upload images, texts or files
    Copyright (C) 2008-2011 ZScreen Developers

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

#endregion License Information (GPL v2)

using System;
using System.Drawing;
using HelpersLib;

namespace ScreenCapture
{
    public class WindowInfo
    {
        public IntPtr Handle { get; private set; }

        public string Text
        {
            get
            {
                return NativeMethods.GetWindowText(Handle);
            }
        }

        public string ClassName
        {
            get
            {
                return NativeMethods.GetClassName(Handle);
            }
        }

        public Rectangle Rectangle
        {
            get
            {
                return CaptureHelpers.GetWindowRectangle(Handle);
            }
        }

        public Rectangle Rectangle0Based
        {
            get
            {
                return CaptureHelpers.FixScreenCoordinates(Rectangle);
            }
        }

        public WindowStyles Styles
        {
            get
            {
                return (WindowStyles)NativeMethods.GetWindowLong(Handle, NativeMethods.GWL_STYLE);
            }
        }

        public bool IsMaximized
        {
            get
            {
                return NativeMethods.IsZoomed(Handle);
            }
        }

        public bool IsMinimized
        {
            get
            {
                return NativeMethods.IsIconic(Handle);
            }
        }

        public bool IsVisible
        {
            get
            {
                return NativeMethods.IsWindowVisible(Handle);
            }
        }

        public Icon Icon
        {
            get
            {
                return NativeMethods.GetApplicationIcon(Handle);
            }
        }

        public WindowInfo(IntPtr handle)
        {
            Handle = handle;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}