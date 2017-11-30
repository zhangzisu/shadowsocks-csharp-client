using Shadowsocks.Controller;
using Shadowsocks.Controller.Service;
using Shadowsocks.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Shadowsocks.View
{
    public partial class FeedForm : Form
    {
        private FeedUpdater updater;

        public FeedForm(FeedUpdater feedUpdater)
        {
            updater = feedUpdater;
            InitializeComponent();
            UpdateText();
            Icon = Icon.FromHandle(Resources.ssw128.GetHicon());
            LoadConfiguration();
        }

        private void UpdateText()
        {
            add.Text = I18N.GetString("&Add");
            delete.Text = I18N.GetString("&Delete");
            save.Text = I18N.GetString("&Save");
            import.Text = I18N.GetString("&Import");
            export.Text = I18N.GetString("&Export");
        }

        private void LoadConfiguration()
        {
            feedList.Items.Clear();
            foreach (var obj in updater.feeds)
            {
                feedList.Items.Add(obj);
            }
        }

        private bool isRightUrl(string s)
        {
            return s.StartsWith("https://") || s.StartsWith("http://");
        }

        private bool isRightInFree(string s)
        {
            foreach (var ss in updater.feeds)
            {
                if (ss.Equals(s)) return false;
            }
            return true;
        }

        private void add_Click(object sender, EventArgs e)
        {
            string text = textBox1.Text;
            if (!isRightUrl(text))
            {
                MessageBox.Show(I18N.GetString("Input is not a legal url."), I18N.GetString("Shadowsocks"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox1.Text = "";
                return;
            }
            if (!isRightInFree(text))
            {
                MessageBox.Show(I18N.GetString("Do not enter duplicate url"), I18N.GetString("Shadowsocks"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox1.Text = "";
                return;
            }
            updater.feeds.Add(text);
            feedList.Items.Add(text);
            feedList.SelectedIndex = feedList.Items.Count - 1;
            updater.saveFeed();
        }

        private void save_Click(object sender, EventArgs e)
        {
            int index = feedList.SelectedIndex;
            string text = textBox1.Text;
            if (index < 0) return;
            if (!isRightUrl(text))
            {
                MessageBox.Show(I18N.GetString("Input is not a legal url."), I18N.GetString("Shadowsocks"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox1.Text = updater.feeds[index];
                return;
            }
            if (!isRightInFree(text))
            {
                MessageBox.Show(I18N.GetString("Do not enter duplicate url"), I18N.GetString("Shadowsocks"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox1.Text = updater.feeds[index];
                return;
            }
            feedList.Items[index] = text;
            updater.feeds[index] = text;
            updater.saveFeed();
        }

        private void delete_Click(object sender, EventArgs e)
        {
            int index = feedList.SelectedIndex;
            if (index < 0) return;
            feedList.SelectedIndex = index - 1;
            feedList.Items.RemoveAt(index);
            updater.feeds.RemoveAt(index);
        }

        private void feedList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (feedList.SelectedIndex < 0) textBox1.Text = "";
            else textBox1.Text = updater.feeds[feedList.SelectedIndex];
        }

        private void export_Click(object sender, EventArgs e)
        {
            if (saveSSF.ShowDialog() == DialogResult.OK)
            {
                updater.exportFeed(saveSSF.FileName);
                MessageBox.Show(I18N.GetString("Exported feeds successfully"), I18N.GetString("Shadowsocks"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void import_Click(object sender, EventArgs e)
        {
            if (openSSF.ShowDialog() == DialogResult.OK)
            {
                updater.importFeed(openSSF.FileName);
                LoadConfiguration();
                MessageBox.Show(I18N.GetString("Imported feeds successfully"), I18N.GetString("Shadowsocks"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
