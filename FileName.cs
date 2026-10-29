using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;
using Button = System.Windows.Forms.Button;
using CheckBox = System.Windows.Forms.CheckBox;
using ListViewItem = System.Windows.Forms.ListViewItem;
using MessageBox = System.Windows.Forms.MessageBox;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;

namespace FileMetadataEditor
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private const int V = 15;
        private System.Windows.Forms.TextBox txtPath;
        private Button btnBrowse, btnRefresh, btnApplyAttributes, btnApplyTimes;
        private System.Windows.Forms.ListView lvMetadata;
        private System.Windows.Forms.GroupBox grpAttributes, grpTimes;
        private System.Windows.Forms.CheckBox chkReadOnly, chkHidden, chkSystem, chkArchive, chkNormal, chkTemp, chkOffline, chkNotContentIndexed;
        private System.Windows.Forms.DateTimePicker dtCreated, dtModified, dtAccessed;
        private System.Windows.Forms.Label lblStatus;
        private string currentFile;

        public MainForm()
        {
            Text = "File Metadata Editor";
            Width = 820;
            Height = 680;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new System.Drawing.Size(700, 640);

            var lbl = new System.Windows.Forms.Label { Text = "File:", Left = 10, Top = V, Width = 40 };
            txtPath = new System.Windows.Forms.TextBox { Left = 55, Top = 12, Width = 620, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            btnBrowse = new System.Windows.Forms.Button { Text = "Browse...", Left = 685, Top = 10, Width = 110, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            btnBrowse.Click += (s, e) => BrowseFile();

            lvMetadata = new System.Windows.Forms.ListView
            {
                Left = 10,
                Top = 45,
                Width = 785,
                Height = 260,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            lvMetadata.Columns.Add("Property", 220);
            lvMetadata.Columns.Add("Value", 540);

            grpAttributes = new System.Windows.Forms.GroupBox
            {
                Text = "File Attributes",
                Left = 10,
                Top = 315,
                Width = 385,
                Height = 230,
                Anchor = AnchorStyles.Top | AnchorStyles.Left
            };

            chkReadOnly = MakeCheck("ReadOnly", V, 25);
            chkHidden = MakeCheck("Hidden", V, 50);
            chkSystem = MakeCheck("System", V, 75);
            chkArchive = MakeCheck("Archive", V, 100);
            chkNormal = MakeCheck("Normal", V, 125);
            chkTemp = MakeCheck("Temporary", V, 150);
            chkOffline = MakeCheck("Offline", V, 175);
            chkNotContentIndexed = MakeCheck("NotContentIndexed", 200, 25);

            btnApplyAttributes = new Button { Text = "Apply Attributes", Left = V, Top = 200, Width = 130 };
            btnApplyAttributes.Click += (s, e) => ApplyAttributes();

            grpAttributes.Controls.AddRange(new System.Windows.Forms.Control[] {
                chkReadOnly, chkHidden, chkSystem, chkArchive, chkNormal,
                chkTemp, chkOffline, chkNotContentIndexed, btnApplyAttributes
            });

            grpTimes = new System.Windows.Forms.GroupBox
            {
                Text = "Timestamps",
                Left = 405,
                Top = 315,
                Width = 390,
                Height = 230,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            var l1 = new System.Windows.Forms.Label { Text = "Created:", Left = V, Top = 27, Width = 70 };
            dtCreated = MakeDatePicker(25);
            var l2 = new System.Windows.Forms.Label { Text = "Modified:", Left = V, Top = 62, Width = 70 };
            dtModified = MakeDatePicker(60);
            var l3 = new System.Windows.Forms.Label { Text = "Accessed:", Left = V, Top = 97, Width = 70 };
            dtAccessed = MakeDatePicker(95); 

            btnApplyTimes = new Button { Text = "Apply Timestamps", Left = 90, Top = 135, Width = 140 };
            btnApplyTimes.Click += (s, e) => ApplyTimes();

            grpTimes.Controls.AddRange(new System.Windows.Forms.Control[] { l1, l2, l3, dtCreated, dtModified, dtAccessed, btnApplyTimes });

            btnRefresh = new Button { Text = "Refresh", Left = 10, Top = 555, Width = 110 };
            btnRefresh.Click += (s, e) => { if (!string.IsNullOrEmpty(currentFile) && File.Exists(currentFile)) LoadFileInfo(currentFile); };

            lblStatus = new System.Windows.Forms.Label
            {
                Left = 130,
                Top = 560,
                Width = 660,
                Text = "Select a file to begin.",
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            Controls.AddRange(new System.Windows.Forms.Control[] {
                lbl, txtPath, btnBrowse, lvMetadata, grpAttributes, grpTimes, btnRefresh, lblStatus
            });

            SetControlsEnabled(false);
        }

        private static CheckBox MakeCheck(string text, int left, int top) =>
            new CheckBox { Text = text, Left = left, Top = top, Width = 170 };

        private static DateTimePicker MakeDatePicker(int top) => new DateTimePicker
        {
            Left = 90,
            Top = top,
            Width = 280,
            Format = DateTimePickerFormat.Custom,
            CustomFormat = "yyyy-MM-dd HH:mm:ss"
        };

        private void SetControlsEnabled(bool enabled)
        {
            grpAttributes.Enabled = enabled;
            grpTimes.Enabled = enabled;
            btnRefresh.Enabled = enabled;
        }

        private void BrowseFile()
        {
            using (var ofd = new OpenFileDialog { Title = "Select any file", Filter = "All files (*.*)|*.*" })
            {
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    txtPath.Text = ofd.FileName;
                    LoadFileInfo(ofd.FileName);
                }
            }
        }

        private void LoadFileInfo(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show(this, "File not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                currentFile = path;
                lvMetadata.Items.Clear();
                SetControlsEnabled(true);

                var fi = new FileInfo(path);
                var attr = fi.Attributes;

                AddRow("Full Path", fi.FullName);
                AddRow("Name", fi.Name);
                AddRow("Extension", fi.Extension);
                AddRow("Directory", fi.DirectoryName);
                AddRow("Size (bytes)", fi.Length.ToString("N0"));
                AddRow("Size (human)", FormatSize(fi.Length));
                AddRow("Attributes (raw)", "0x" + ((int)attr).ToString("X8") + " (" + attr + ")");
                AddRow("Created (local)", fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                AddRow("Last Modified (local)", fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                AddRow("Last Accessed (local)", fi.LastAccessTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                AddRow("Created (UTC)", fi.CreationTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff") + " UTC");
                AddRow("Last Modified (UTC)", fi.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff") + " UTC");
                AddRow("Last Accessed (UTC)", fi.LastAccessTimeUtc.ToString("yyyy-MM-dd HH:mm:ss.fff") + " UTC");
                AddRow("ReadOnly", fi.IsReadOnly.ToString());
                AddRow("Is System", ((attr & FileAttributes.System) != 0).ToString());
                AddRow("Is Hidden", ((attr & FileAttributes.Hidden) != 0).ToString());

                chkReadOnly.Checked = (attr & FileAttributes.ReadOnly) != 0;
                chkHidden.Checked = (attr & FileAttributes.Hidden) != 0;
                chkSystem.Checked = (attr & FileAttributes.System) != 0;
                chkArchive.Checked = (attr & FileAttributes.Archive) != 0;
                chkNormal.Checked = (attr & FileAttributes.Normal) != 0;
                chkTemp.Checked = (attr & FileAttributes.Temporary) != 0;
                chkOffline.Checked = (attr & FileAttributes.Offline) != 0;
                chkNotContentIndexed.Checked = (attr & FileAttributes.NotContentIndexed) != 0;

                dtCreated.Value = SafeDate(fi.CreationTime);
                dtModified.Value = SafeDate(fi.LastWriteTime);
                dtAccessed.Value = SafeDate(fi.LastAccessTime);

                lblStatus.Text = "Loaded: " + fi.Name + "   (" + FormatSize(fi.Length) + ")";
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to load file info:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DateTime SafeDate(DateTime dt)
        {
            if (dt < dtCreated.MinDate) return dtCreated.MinDate;
            if (dt > dtCreated.MaxDate) return dtCreated.MaxDate;
            return dt;
        }

        private void AddRow(string key, string value)
        {
            var item = new ListViewItem(key);
            item.SubItems.Add(value ?? "");
            lvMetadata.Items.Add(item);
        }

        private static string FormatSize(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double v = bytes;
            int u = 0;
            while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
            return v.ToString("0.##") + " " + units[u];
        }

        private void ApplyAttributes()
        {
            if (string.IsNullOrEmpty(currentFile)) return;
            try
            {
                var attr = File.GetAttributes(currentFile);
                const FileAttributes mask =
                    FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System |
                    FileAttributes.Archive | FileAttributes.Normal | FileAttributes.Temporary |
                    FileAttributes.Offline | FileAttributes.NotContentIndexed;

                attr &= ~mask;

                if (chkReadOnly.Checked) attr |= FileAttributes.ReadOnly;
                if (chkHidden.Checked) attr |= FileAttributes.Hidden;
                if (chkSystem.Checked) attr |= FileAttributes.System;
                if (chkArchive.Checked) attr |= FileAttributes.Archive;
                if (chkNormal.Checked) attr |= FileAttributes.Normal;
                if (chkTemp.Checked) attr |= FileAttributes.Temporary;
                if (chkOffline.Checked) attr |= FileAttributes.Offline;
                if (chkNotContentIndexed.Checked) attr |= FileAttributes.NotContentIndexed;

                File.SetAttributes(currentFile, attr);
                lblStatus.Text = "Attributes applied at " + DateTime.Now.ToString("HH:mm:ss");
                LoadFileInfo(currentFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to set attributes:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyTimes()
        {
            if (string.IsNullOrEmpty(currentFile)) return;
            try
            {
                // Timestamp setters open the file for writing; lift ReadOnly temporarily.
                var original = File.GetAttributes(currentFile);
                bool wasReadOnly = (original & FileAttributes.ReadOnly) != 0;
                try
                {
                    if (wasReadOnly)
                    {
                        var writable = original & ~FileAttributes.ReadOnly;
                        File.SetAttributes(currentFile, writable == 0 ? FileAttributes.Normal : writable);
                    }

                    File.SetCreationTime(currentFile, dtCreated.Value);
                    File.SetLastWriteTime(currentFile, dtModified.Value);
                    File.SetLastAccessTime(currentFile, dtAccessed.Value);
                }
                finally
                {
                    if (wasReadOnly) File.SetAttributes(currentFile, original);
                }

                lblStatus.Text = "Timestamps applied at " + DateTime.Now.ToString("HH:mm:ss");
                LoadFileInfo(currentFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to set timestamps:\r\n" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}