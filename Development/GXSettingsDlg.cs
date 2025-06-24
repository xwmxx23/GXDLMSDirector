﻿//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
//
// Version:         $Revision: 9442 $,
//                  $Date: 2017-05-23 15:21:03 +0300 (ti, 23 touko 2017) $
//                  $Author: gurux01 $
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU General Public License for more details.
//
// More information of Gurux DLMS/COSEM Director: https://www.gurux.org/GXDLMSDirector
//
// This code is licensed under the GNU General Public License v2.
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using Gurux.Common;
using Gurux.DLMS;
using Gurux.Net;
using System;
using System.Collections.Generic;

using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace GXDLMSDirector
{
    public partial class GXSettingsDlg : Form
    {
        GXDLMSTranslator _eventsTranslator;
        Form notifications;
        List<IGXSettingsPage> MediaPropertiesForm;
        Gurux.Common.GXAsyncWork checkUpdates;
        private void Move2(Control.ControlCollection source, Control.ControlCollection target, bool enabled)
        {
            while (source.Count != 0)
            {
                Control ctr = source[0];
                if (ctr is Panel)
                {
                    if (!ctr.Enabled)
                    {
                        source.RemoveAt(0);
                        continue;
                    }
                }
                target.Add(ctr);
                ctr.Visible = true;
                ctr.Enabled = enabled;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="media"></param>
        public GXSettingsDlg(GXNet media, GXDLMSTranslator eventsTranslator)
        {
            _eventsTranslator = eventsTranslator;
            InitializeComponent();
            CertificationServerTb.Text = Properties.Settings.Default.GeneratorAddress;
            try
            {
                //Show notification settings.
                notifications = media.PropertiesForm;
                (notifications as IGXPropertyPage).Initialize();
                Move2(notifications.Controls, NotificationsView.Controls, !media.IsOpen);
                //Show custom settings.
                MediaPropertiesForm = new List<IGXSettingsPage>();
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (Type type in a.GetTypes())
                        {
                            if (!type.IsAbstract && type.IsClass && typeof(IGXSettingsPage).IsAssignableFrom(type))
                            {
                                MediaPropertiesForm.Add(Activator.CreateInstance(type) as IGXSettingsPage);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //It's OK if this fails.
                    }
                }
                foreach (Form it in MediaPropertiesForm)
                {
                    TabPage page = new TabPage((it as IGXSettingsPage).Caption);
                    Tabs.TabPages.Add(page);
                    (it as IGXSettingsPage).Initialize();
                    Move2(it.Controls, page.Controls, true);
                }
                //Show external medias.
                foreach (string it in Properties.Settings.Default.ExternalMedias.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    AddMedia(it);
                }
                if (IsAscii(eventsTranslator.SystemTitle))
                {
                    SystemTitleAsciiCb.CheckedChanged -= SystemTitleAsciiCb_CheckedChanged;
                    SystemTitleAsciiCb.Checked = true;
                    SystemTitleAsciiCb.CheckedChanged += SystemTitleAsciiCb_CheckedChanged;
                    SystemTitleTB.Text = ASCIIEncoding.ASCII.GetString(eventsTranslator.SystemTitle);
                }
                else
                {
                    SystemTitleAsciiCb.CheckedChanged -= SystemTitleAsciiCb_CheckedChanged;
                    SystemTitleAsciiCb.Checked = false;
                    SystemTitleAsciiCb.CheckedChanged += SystemTitleAsciiCb_CheckedChanged;
                    SystemTitleTB.Text = GXCommon.ToHex(eventsTranslator.SystemTitle);
                }
                if (IsAscii(eventsTranslator.BlockCipherKey))
                {
                    BlockCipherKeyAsciiCb.CheckedChanged -= BlockCipherKeyAsciiCb_CheckedChanged;
                    BlockCipherKeyAsciiCb.Checked = true;
                    BlockCipherKeyAsciiCb.CheckedChanged += BlockCipherKeyAsciiCb_CheckedChanged;
                    BlockCipherKeyTB.Text = ASCIIEncoding.ASCII.GetString(eventsTranslator.BlockCipherKey);
                }
                else
                {
                    BlockCipherKeyAsciiCb.CheckedChanged -= BlockCipherKeyAsciiCb_CheckedChanged;
                    BlockCipherKeyAsciiCb.Checked = false;
                    BlockCipherKeyAsciiCb.CheckedChanged += BlockCipherKeyAsciiCb_CheckedChanged;
                    BlockCipherKeyTB.Text = GXCommon.ToHex(eventsTranslator.BlockCipherKey);
                }
                if (IsAscii(eventsTranslator.AuthenticationKey))
                {
                    AuthenticationKeyAsciiCb.CheckedChanged -= AuthenticationKeyAsciiCb_CheckedChanged;
                    AuthenticationKeyAsciiCb.Checked = true;
                    AuthenticationKeyAsciiCb.CheckedChanged += AuthenticationKeyAsciiCb_CheckedChanged;
                    AuthenticationKeyTB.Text = ASCIIEncoding.ASCII.GetString(eventsTranslator.AuthenticationKey);
                }
                else
                {
                    AuthenticationKeyAsciiCb.CheckedChanged -= AuthenticationKeyAsciiCb_CheckedChanged;
                    AuthenticationKeyAsciiCb.Checked = false;
                    AuthenticationKeyAsciiCb.CheckedChanged += AuthenticationKeyAsciiCb_CheckedChanged;
                    AuthenticationKeyTB.Text = GXCommon.ToHex(eventsTranslator.AuthenticationKey);
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void AddMedia(string it)
        {
            string initDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GXDLMSDirector");
            Assembly asm = null;
            ListViewItem li;
            if (GXExternalMediaForm.IsDownloaded(it))
            {
                //If file is downloaded from the web.
                asm = Assembly.GetExecutingAssembly();
                string path = Path.Combine(initDir, "Medias");
                path = Path.Combine(path, Path.GetFileName(it));
                li = MediaList.Items.Add(Path.GetFileName(it));
                asm = Assembly.LoadFile(path);
                li.SubItems.Add(asm.GetName().Version.ToString());
                li.SubItems.Add(it);
                li.Tag = asm;
                return;
            }
            if (File.Exists(it))
            {
                asm = Assembly.LoadFile(it);
                li = MediaList.Items.Add(asm.GetName().Name);
                li.SubItems.Add(asm.GetName().Version.ToString());
                li.SubItems.Add(asm.Location.ToString());
                li.Tag = asm;
            }
        }

        public static string GetAsHex(string value, bool ascii)
        {
            if (ascii)
            {
                return GXCommon.ToHex(ASCIIEncoding.ASCII.GetBytes(value), false);
            }
            return GXCommon.ToHex(GXCommon.HexToBytes(value), false);
        }


        /// <summary>
        /// Accept changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKBtn_Click(object sender, EventArgs e)
        {
            try
            {
                _eventsTranslator.SystemTitle = GXCommon.HexToBytes(GetAsHex(SystemTitleTB.Text, SystemTitleAsciiCb.Checked));
                _eventsTranslator.BlockCipherKey = GXCommon.HexToBytes(GetAsHex(BlockCipherKeyTB.Text, BlockCipherKeyAsciiCb.Checked));
                _eventsTranslator.AuthenticationKey = GXCommon.HexToBytes(GetAsHex(AuthenticationKeyTB.Text, AuthenticationKeyAsciiCb.Checked));
                (notifications as IGXPropertyPage).Apply();
                foreach (IGXSettingsPage it in MediaPropertiesForm)
                {
                    it.Apply();
                }
                //Handle external medias.
                Properties.Settings.Default.ExternalMedias = "";
                bool first = true;
                foreach (ListViewItem it in MediaList.Items)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        Properties.Settings.Default.ExternalMedias = ";";
                    }
                    Properties.Settings.Default.ExternalMedias += it.SubItems[2].Text;
                }
                if (checkUpdates != null)
                {
                    checkUpdates.Cancel();
                }
                Properties.Settings.Default.GeneratorAddress = CertificationServerTb.Text;
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
                this.DialogResult = DialogResult.None;
            }
        }

        /// <summary>
        /// Add new media.
        /// </summary>
        private void AddBtn_Click(object sender, EventArgs e)
        {
            try
            {
                GXExternalMediaForm dlg = new GXExternalMediaForm();
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (GXExternalMediaForm.IsDownloaded(dlg.FileName))
                    {
                        if (GXExternalMediaForm.DownLoadMedia(dlg.FileName))
                        {
                            if (MessageBox.Show(this, "You need to restart application. Do you want to do it now?", Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                AddMedia(dlg.FileName);
                                OKBtn_Click(null, null);
                                Application.Restart();
                            }
                        }
                    }
                    AddMedia(dlg.FileName);
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        void OnAsyncStateChange(object sender, GXAsyncWork work, object[] parameters, AsyncState state, string text)
        {
        }

        void CheckUpdates(object sender, GXAsyncWork work, object[] parameters)
        {
            IGXUpdater updater = (IGXUpdater)parameters[0];
            Assembly asm = (Assembly)parameters[1];
            if (!GXExternalMediaForm.CheckUpdates(updater, asm))
            {
                // There's nothing to update
                MessageBox.Show(Properties.Resources.ExternalMediaNoUpdates, Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.OK);
            }
            else
            {
                if (MessageBox.Show("You need to restart application. Do you want to do it now?", Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Application.Restart();
                }
            }
        }
        /// <summary>
        /// Show occurred errors.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="ex"></param>
        private void OnError(object sender, Exception ex)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Gurux.Common.ErrorEventHandler(OnError), sender, ex);
            }
            else
            {
                GXDLMS.Common.Error.ShowError(this, ex);
            }
        }

        private void CheckUpdatesBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (MediaList.SelectedItems.Count != 1)
                {
                    throw new Exception("Update check failed. Selected media to check updates.");
                }
                Assembly asm = (Assembly)MediaList.SelectedItems[0].Tag;
                IGXUpdater updater = null;
                foreach (Type type in asm.GetTypes())
                {
                    if (!type.IsAbstract && type.IsClass && typeof(IGXUpdater).IsAssignableFrom(type))
                    {
                        updater = Activator.CreateInstance(type) as IGXUpdater;
                        break;
                    }
                }
                if (updater == null)
                {
                    throw new Exception("Update check failed. Update checker is not supported.");
                }
                checkUpdates = new GXAsyncWork(this, OnAsyncStateChange, CheckUpdates, OnError, null, new object[] { updater, asm });
                checkUpdates.Start();
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void RemoveBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show(this, GXDLMSDirector.Properties.Resources.ExternalMediaRemove, "GXDLMSDirector", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    while (MediaList.SelectedItems.Count != 0)
                    {
                        MediaList.SelectedItems[0].Remove();
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void CancelBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (checkUpdates != null)
                {
                    checkUpdates.Cancel();
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Copy external media info to the clipboard.
        /// </summary>
        private void NotificationCopy_Click(object sender, EventArgs e)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                foreach (ListViewItem it in MediaList.SelectedItems)
                {
                    foreach(var i in it.SubItems)
                    {
                        sb.Append(i.ToString());
                        sb.Append(";");
                    }
                    if (sb.Length != 0)
                    {
                        --sb.Length;
                        sb.AppendLine("");
                        Clipboard.SetText(sb.ToString());
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        public static bool IsAscii(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return false;
            }
            foreach (byte it in value)
            {
                if (it < 0x21 || it > 0x7E)
                {
                    return false;
                }
            }
            return true;
        }

        private void SystemTitleAsciiCb_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (SystemTitleAsciiCb.Checked)
                {
                    if (!IsAscii(GXCommon.HexToBytes(SystemTitleTB.Text)))
                    {
                        SystemTitleAsciiCb.CheckedChanged -= SystemTitleAsciiCb_CheckedChanged;
                        SystemTitleAsciiCb.Checked = !SystemTitleAsciiCb.Checked;
                        SystemTitleAsciiCb.CheckedChanged += SystemTitleAsciiCb_CheckedChanged;
                        throw new ArgumentOutOfRangeException(Properties.Resources.InvalidASCII);
                    }
                    SystemTitleTB.Text = ASCIIEncoding.ASCII.GetString(GXCommon.HexToBytes(SystemTitleTB.Text));
                }
                else
                {
                    SystemTitleTB.Text = GXCommon.ToHex(ASCIIEncoding.ASCII.GetBytes(SystemTitleTB.Text), true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void BlockCipherKeyAsciiCb_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (BlockCipherKeyAsciiCb.Checked)
                {
                    if (!IsAscii(GXCommon.HexToBytes(BlockCipherKeyTB.Text)))
                    {
                        BlockCipherKeyAsciiCb.CheckedChanged -= BlockCipherKeyAsciiCb_CheckedChanged;
                        BlockCipherKeyAsciiCb.Checked = !BlockCipherKeyAsciiCb.Checked;
                        BlockCipherKeyAsciiCb.CheckedChanged += BlockCipherKeyAsciiCb_CheckedChanged;
                        throw new ArgumentOutOfRangeException(Properties.Resources.InvalidASCII);
                    }
                    BlockCipherKeyTB.Text = ASCIIEncoding.ASCII.GetString(GXCommon.HexToBytes(BlockCipherKeyTB.Text));
                }
                else
                {
                    BlockCipherKeyTB.Text = GXCommon.ToHex(ASCIIEncoding.ASCII.GetBytes(BlockCipherKeyTB.Text), true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }

        private void AuthenticationKeyAsciiCb_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (AuthenticationKeyAsciiCb.Checked)
                {
                    if (!IsAscii(GXCommon.HexToBytes(AuthenticationKeyTB.Text)))
                    {
                        AuthenticationKeyAsciiCb.CheckedChanged -= AuthenticationKeyAsciiCb_CheckedChanged;
                        AuthenticationKeyAsciiCb.Checked = !AuthenticationKeyAsciiCb.Checked;
                        AuthenticationKeyAsciiCb.CheckedChanged += AuthenticationKeyAsciiCb_CheckedChanged;
                        throw new ArgumentOutOfRangeException(Properties.Resources.InvalidASCII);
                    }
                    AuthenticationKeyTB.Text = ASCIIEncoding.ASCII.GetString(GXCommon.HexToBytes(AuthenticationKeyTB.Text));
                }
                else
                {
                    AuthenticationKeyTB.Text = GXCommon.ToHex(ASCIIEncoding.ASCII.GetBytes(AuthenticationKeyTB.Text), true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
            }
        }
    }
}
