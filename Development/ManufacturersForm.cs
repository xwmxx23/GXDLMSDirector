//
// --------------------------------------------------------------------------
//  Gurux Ltd
//
//
//
//
// Version:         $Revision: 12483 $,
//                  $Date: 2021-06-07 12:52:24 +0300 (ma, 07 kesä 2021) $
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

using System;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Gurux.DLMS.ManufacturerSettings;
using Gurux.DLMS.Enums;

namespace GXDLMSDirector
{
    public partial class ManufacturersForm : Form
    {
        System.Collections.Hashtable Items = new System.Collections.Hashtable();
        GXManufacturerCollection ManufacturersOriginal;
        GXManufacturerCollection Manufacturers;
        public ManufacturersForm(GXManufacturerCollection manufacturers, string selectedManufacturer)
        {
            InitializeComponent();
            NameCH.Width = -2;
            UpdateValues();
            NameTB.Enabled = ManufacturerIdTB.Enabled = UseLNCB.Enabled = false;
            ManufacturersOriginal = manufacturers;

            //Create clone from original items.
            if (manufacturers != null)
            {
                MemoryStream ms = new MemoryStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, manufacturers);
                ms.Position = 0;
                Manufacturers = (GXManufacturerCollection)bf.Deserialize(ms);
                ms.Close();
                bool bSelected = false;
                foreach (GXManufacturer it in Manufacturers)
                {
                    if (!it.Removed)
                    {
                        ListViewItem item = AddManufacturer(it);
                        if (it.Identification == selectedManufacturer)
                        {
                            bSelected = item.Selected = true;
                        }
                    }
                }
                //Select first item
                if (ManufacturersList.Items.Count != 0 && !bSelected)
                {
                    ManufacturersList.Items[0].Selected = true;
                }
            }
        }

        ListViewItem AddManufacturer(GXManufacturer manufacturer)
        {
            ListViewItem it = ManufacturersList.Items.Add(manufacturer.Name);
            it.Tag = manufacturer;
            Items.Add(manufacturer, it);
            return it;
        }

        /// <summary>
        /// Add new manufacturer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewBtn_Click(object sender, EventArgs e)
        {
            try
            {
                GXManufacturer m = new GXManufacturer();
                ManufacturerForm dlg = new ManufacturerForm(Manufacturers, m);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    Manufacturers.Add(m);
                    AddManufacturer(m).Selected = true;
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }

        }

        /// <summary>
        /// Edit selected manufacturer's settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EditBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (ManufacturersList.SelectedItems.Count == 1)
                {
                    GXManufacturer m = (GXManufacturer)ManufacturersList.SelectedItems[0].Tag;
                    ManufacturerForm dlg = new ManufacturerForm(Manufacturers, m);
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        ManufacturersList.SelectedItems[0].Text = m.Name;
                        UpdateValues();
                    }
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        /// <summary>
        /// Remove selected manufacturer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveBtn_Click(object sender, EventArgs e)
        {
            try
            {
                while (ManufacturersList.SelectedItems.Count != 0)
                {
                    GXManufacturer m = (GXManufacturer)ManufacturersList.SelectedItems[0].Tag;
                    ManufacturersList.SelectedItems[0].Remove();
                    Items.Remove(m);
                    m.Removed = true;
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }

        private void OKBtn_Click(object sender, EventArgs e)
        {
            try
            {
                ManufacturersOriginal.Clear();
                ManufacturersOriginal.AddRange(Manufacturers);
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
                DialogResult = DialogResult.None;
            }
        }

        private void UpdatateInterface(bool isSupported, InterfaceType type)
        {
            if (isSupported)
            {
                if (SupportedInterfacesTb.Text.Length != 0)
                {
                    SupportedInterfacesTb.AppendText(Environment.NewLine);
                }
                SupportedInterfacesTb.AppendText(type.ToString());
            }
        }

        void UpdateValues()
        {
            bool bEnabled = ManufacturersList.SelectedItems.Count == 1;
            RemoveBtn.Enabled = EditBtn.Enabled = bEnabled;
            SupportedInterfacesTb.Text = "";
            if (bEnabled)
            {
                GXManufacturer m = (GXManufacturer)ManufacturersList.SelectedItems[0].Tag;
                NameTB.Text = m.Name;
                ManufacturerIdTB.Text = m.Identification;
                UseLNCB.Checked = m.UseLogicalNameReferencing;
                if (m.SupporterdInterfaces != 0)
                {
                    UpdatateInterface((m.SupporterdInterfaces & (1 << (int)InterfaceType.HDLC)) != 0, InterfaceType.HDLC);
                    UpdatateInterface((m.SupporterdInterfaces & (1 << (int)InterfaceType.HdlcWithModeE)) != 0, InterfaceType.HdlcWithModeE);
                    UpdatateInterface((m.SupporterdInterfaces & (1 << (int)InterfaceType.WRAPPER)) != 0, InterfaceType.WRAPPER);
                    UpdatateInterface((m.SupporterdInterfaces & (1 << (int)InterfaceType.WirelessMBus)) != 0, InterfaceType.WirelessMBus);
                    UpdatateInterface((m.SupporterdInterfaces & (1 << (int)InterfaceType.Plc)) != 0, InterfaceType.Plc);
                    UpdatateInterface((m.SupporterdInterfaces & (1 << (int)InterfaceType.PlcHdlc)) != 0, InterfaceType.PlcHdlc);
                    UpdatateInterface((m.SupporterdInterfaces & (1 << (int)InterfaceType.LPWAN)) != 0, InterfaceType.LPWAN);
                    UpdatateInterface((m.SupporterdInterfaces & (1 << (int)InterfaceType.WiSUN)) != 0, InterfaceType.WiSUN);
                }
                else
                {
                    //Select default interfaces.
                    UpdatateInterface(true, InterfaceType.HDLC);
                    UpdatateInterface(true, InterfaceType.HdlcWithModeE);
                    UpdatateInterface(true, InterfaceType.WRAPPER);
                }
            }
            else
            {
                NameTB.Text = ManufacturerIdTB.Text = "";
                UseLNCB.Checked = false;
            }
        }

        private void ManufacturersList_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateValues();
        }

        /// <summary>
        /// Download latest versions from Gurux www-page.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadLatestBtn_Click(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    if (MessageBox.Show(this, Properties.Resources.UpdateManufacturersOnlineTxt,
                        Properties.Resources.GXDLMSDirectorTxt, MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
                    {
                        GXManufacturerCollection.UpdateManufactureSettings();
                        Manufacturers = new GXManufacturerCollection();
                        GXManufacturerCollection.ReadManufacturerSettings(Manufacturers);
                    }
                }
                catch (Exception Ex)
                {
                    GXDLMS.Common.Error.ShowError(this, Ex);
                }
            }
            catch (Exception Ex)
            {
                GXDLMS.Common.Error.ShowError(this, Ex);
            }
        }
    }
}
