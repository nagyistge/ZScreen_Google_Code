﻿//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Runtime.InteropServices;
using System.IO;

namespace Microsoft.WindowsAPICodePack.Shell.Taskbar
{
    /// <summary>
    /// Represents a jump list item.
    /// </summary>
    public class JumpListItem : ShellFile, IJumpListItem
    {
        /// <summary>
        /// Creates a jump list item with the specified path.
        /// </summary>
        /// <param name="path">The path to the jump list item.</param>
        /// <remarks>The file type should associate the given file  
        /// with the calling application.</remarks>
        public JumpListItem(string path)
            : base(path)
        {

        }

        #region IJumpListItem Members

        /// <summary>
        /// Gets or sets the target path for this jump list item.
        /// </summary>
        public new string Path
        {
            get
            {
                return base.Path;
            }
            set
            {
                base.Path = value;
            }
        }

        /// <summary>
        /// Returns an <b>IShellItem</b> representation of this object.
        /// </summary>
        /// <returns>An IShellItem object.</returns>
        object IJumpListItem.GetShellRepresentation()
        {
            return NativeShellItem;
        }

        #endregion
    }
}