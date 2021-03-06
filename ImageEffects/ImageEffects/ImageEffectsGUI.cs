﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using HelpersLib.GraphicsHelper;
using ImageEffects.IPlugin;

namespace ImageEffects
{
    public partial class ImageEffectsGUI : Form
    {
        private List<IPluginInterface> plugins;

        public Image DefaultImage { get; set; }

        public ImageEffectsGUI(string filePath)
            : this(Image.FromFile(filePath))
        {
            this.Text = string.Format("Image Effects - {0}", filePath);
        }

        public ImageEffectsGUI(Image img)
        {
            InitializeComponent();
            plugins = PluginManager.LoadPlugins<IPluginInterface>(Application.StartupPath);
            FillPluginsList();

            DefaultImage = img;

            pbDefault.Image = DefaultImage;
            pbDefaultZoom.Image = ImageEffectsHelper.Zoom(pbDefault.Image, 8, 12);
            lblDefault.Text = string.Format("Default image ({0}x{1})", pbDefault.Image.Width, pbDefault.Image.Height);
        }

        private void FillPluginsList()
        {
            TreeNode parentNode, childNode;
            foreach (IPluginInterface plugin in plugins)
            {
                parentNode = tvPlugins.Nodes.Add(plugin.Name);
                parentNode.Tag = plugin;

                foreach (IPluginItem item in plugin.PluginItems)
                {
                    childNode = parentNode.Nodes.Add(item.Name);
                    childNode.Tag = item;
                }
            }

            tvPlugins.ExpandAll();
        }

        private void UpdatePreview()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            pbPreview.Image = GetImageForExport();
            lblPreview.Text = string.Format("Preview image ({0}x{1}) - {2} ms", pbPreview.Image.Width, pbPreview.Image.Height, timer.ElapsedMilliseconds);
            pbPreviewZoom.Image = ImageEffectsHelper.Zoom(pbPreview.Image, 8, 12);
        }

        public Image GetImageForExport()
        {
            Image img = (Image)DefaultImage.Clone();
            IPluginItem[] plugins = lvEffects.Items.Cast<ListViewItem>().Where(x => x.Tag is IPluginItem).Select(x => (IPluginItem)x.Tag).ToArray();
            Image tempImage = PluginManager.ApplyEffects(plugins, img);
            return tempImage != null ? tempImage : DefaultImage;
        }

        private void lvEffects_SelectedIndexChanged(object sender, EventArgs e)
        {
            pgSettings.SelectedObject = null;

            if (lvEffects.SelectedItems.Count > 0)
            {
                ListViewItem lvi = lvEffects.SelectedItems[0];
                if (lvi.Tag is IPluginItem)
                {
                    pgSettings.SelectedObject = lvi.Tag;
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            TreeNode node = tvPlugins.SelectedNode;
            if (node.Tag is IPluginItem)
            {
                IPluginItem pluginItem = (IPluginItem)Activator.CreateInstance(node.Tag.GetType());
                ListViewItem lvi = new ListViewItem(pluginItem.Name);
                lvi.Tag = pluginItem;
                pluginItem.PreviewTextChanged += p => lvi.Text = string.Format("{0}: {1}", pluginItem.Name, p);

                if (lvEffects.SelectedIndices.Count > 0)
                {
                    lvEffects.Items.Insert(lvEffects.SelectedIndices[lvEffects.SelectedIndices.Count - 1] + 1, lvi);
                }
                else
                {
                    lvEffects.Items.Add(lvi);
                    lvEffects.Items[lvEffects.Items.Count - 1].Selected = true;
                }
            }

            UpdatePreview();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            foreach (ListViewItem lvi in lvEffects.SelectedItems)
            {
                lvi.Remove();
            }

            UpdatePreview();
        }

        private void pgSettings_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            UpdatePreview();
        }

        private void ImageEffectsGUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }
    }
}