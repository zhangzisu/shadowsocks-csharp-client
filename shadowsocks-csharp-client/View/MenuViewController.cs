﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

using ZXing;
using ZXing.Common;
using ZXing.QrCode;

using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Properties;
using Shadowsocks.Util;
using System.Linq;
using Shadowsocks.Controller.Service;

namespace Shadowsocks.View
{
    public class MenuViewController
    {
        // yes this is just a menu view controller
        // when config form is closed, it moves away from RAM
        // and it should just do anything related to the config form

        private ShadowsocksController controller;
        private UpdateChecker updateChecker;
        private FeedUpdater feedUpdater;

        private NotifyIcon _notifyIcon;
        private Bitmap icon_baseBitmap;
        private Icon icon_base, icon_in, icon_out, icon_both, targetIcon;
        private ContextMenu contextMenu1;

        private bool _isFirstRun;
        private bool _isStartupChecking;
        private MenuItem AutoStartupItem;
        private MenuItem ShareOverLANItem;
        private MenuItem SeperatorItem;
        private MenuItem ConfigItem;
        private MenuItem ServersItem;
        private MenuItem autoCheckUpdatesToggleItem;
        private MenuItem checkPreReleaseToggleItem;
        private MenuItem proxyItem;
        private MenuItem VerboseLoggingToggleItem;
        private MenuItem feedSettingsItem;
        private MenuItem updateFeedItem;
        private ConfigForm configForm;
        private FeedForm feedForm;
        private ProxyForm proxyForm;
        private LogForm logForm;
        private string _urlToOpen;

        public MenuViewController(ShadowsocksController controller)
        {
            this.controller = controller;

            LoadMenu();

            controller.ConfigChanged += controller_ConfigChanged;
            controller.ShareOverLANStatusChanged += controller_ShareOverLANStatusChanged;
            controller.VerboseLoggingStatusChanged += controller_VerboseLoggingStatusChanged;
            controller.Errored += controller_Errored;
            controller.ReloadServer += ReloadServer;

            _notifyIcon = new NotifyIcon();
            icon_baseBitmap = Resources.ss_id;
            icon_base = Icon.FromHandle(icon_baseBitmap.GetHicon());
            targetIcon = icon_base;
            icon_in = Icon.FromHandle(Resources.ss_i.GetHicon());
            icon_out = Icon.FromHandle(Resources.ss_o.GetHicon());
            icon_both = Icon.FromHandle(Resources.ss_io.GetHicon());
            UpdateTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.ContextMenu = contextMenu1;
            _notifyIcon.BalloonTipClicked += notifyIcon1_BalloonTipClicked;
            _notifyIcon.MouseClick += NotifyIcon1_Click;
            _notifyIcon.MouseDoubleClick += notifyIcon1_DoubleClick;
            _notifyIcon.BalloonTipClosed += _notifyIcon_BalloonTipClosed;
            controller.TrafficChanged += controller_TrafficChanged;

            updateChecker = new UpdateChecker();
            updateChecker.CheckUpdateCompleted += updateChecker_CheckUpdateCompleted;

            feedUpdater = new FeedUpdater(controller);
            feedUpdater.StartUpdate += fu_startUpdate;
            feedUpdater.FinishUpdate += fu_finishUpdate;

            LoadCurrentConfiguration();

            Configuration config = controller.GetConfigurationCopy();

            if (config.isDefault)
            {
                _isFirstRun = true;
                ShowConfigForm();
            }
            else if (config.autoCheckUpdate)
            {
                _isStartupChecking = true;
                updateChecker.CheckUpdate(config, 3000);
            }
            if (config.autoUpdateFeeds)
            {
                feedUpdater.UpdateFeed();
            }
        }

        private void ShowFeedSettings(object sender, EventArgs e)
        {
            ShowFeedForm();
        }

        private void ShowFeedForm()
        {
            updateFeedItem.Enabled = false;
            if (feedForm != null)
            {
                feedForm.Activate();
            }
            else
            {
                feedForm = new FeedForm(feedUpdater);
                feedForm.Show();
                feedForm.Activate();
                feedForm.FormClosed += FeedForm_closed;
            }
        }

        private void FeedForm_closed(object sender, EventArgs e)
        {
            updateFeedItem.Enabled = true;
            feedForm.Dispose();
            feedForm = null;
            Utils.ReleaseMemory(true);
        }

        private void fu_startUpdate(object sender, EventArgs e)
        {
            ShowBalloonTip(I18N.GetString("Shadowsocks"), I18N.GetString("Started update feeds, please wait..."), ToolTipIcon.Info, 5000);
            feedSettingsItem.Enabled = false;
        }

        private void fu_finishUpdate(object sender, EventArgs e)
        {
            if (sender != null && sender.GetType() == typeof(string))
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks"), I18N.GetString("Successfully find a free server.") + "\n" + sender, ToolTipIcon.Info, 5000);
            } 
            else
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks"), I18N.GetString("Successfully updated all feeds."), ToolTipIcon.Info, 5000);
                feedSettingsItem.Enabled = true;
            }
        }

        private void controller_TrafficChanged(object sender, EventArgs e)
        {
            if (icon_baseBitmap == null)
                return;

            Icon newIcon;

            bool hasInbound = controller.trafficPerSecondQueue.Last().inboundIncreasement > 0;
            bool hasOutbound = controller.trafficPerSecondQueue.Last().outboundIncreasement > 0;

            if (hasInbound && hasOutbound)
                newIcon = icon_both;
            else if (hasInbound)
                newIcon = icon_in;
            else if (hasOutbound)
                newIcon = icon_out;
            else
                newIcon = icon_base;

            if (newIcon != targetIcon)
            {
                targetIcon = newIcon;
                _notifyIcon.Icon = newIcon;
            }
        }

        void controller_Errored(object sender, System.IO.ErrorEventArgs e)
        {
            MessageBox.Show(e.GetException().ToString(), String.Format(I18N.GetString("Shadowsocks Error: {0}"), e.GetException().Message));
        }

        #region Tray Icon

        private void UpdateTrayIcon()
        {
            Configuration config = controller.GetConfigurationCopy();

            _notifyIcon.Icon = targetIcon;

            string serverInfo = null;
            if (controller.GetCurrentStrategy() != null)
            {
                serverInfo = controller.GetCurrentStrategy().Name;
            }
            else
            {
                serverInfo = config.GetCurrentServer().FriendlyName();
            }
            // show more info by hacking the P/Invoke declaration for NOTIFYICONDATA inside Windows Forms
            string text = I18N.GetString("Shadowsocks") + " " + UpdateChecker.Version + "\n" +
                          String.Format(I18N.GetString("Running: Port {0}"), config.localPort)  // this feedback is very important because they need to know Shadowsocks is running
                          + "\n" + serverInfo;
            ViewUtils.SetNotifyIconText(_notifyIcon, text);
        }

        private Bitmap AddBitmapOverlay(Bitmap original, params Bitmap[] overlays)
        {
            Bitmap bitmap = new Bitmap(original);
            Graphics canvas = Graphics.FromImage(bitmap);
            canvas.DrawImage(original, new Point(0, 0));
            foreach (Bitmap overlay in overlays)
            {
                canvas.DrawImage(new Bitmap(overlay, original.Size), new Point(0, 0));
            }
            canvas.Save();
            return bitmap;
        }

        #endregion

        #region MenuItems and MenuGroups

        private MenuItem CreateMenuItem(string text, EventHandler click, bool check = false)
        {
            return new MenuItem(I18N.GetString(text), click)
            {
                Checked = check
            };
        }

        private MenuItem CreateMenuGroup(string text, MenuItem[] items)
        {
            return new MenuItem(I18N.GetString(text), items);
        }

        private void LoadMenu()
        {
            contextMenu1 = new ContextMenu(new MenuItem[] {
                ServersItem = CreateMenuGroup("Servers", new MenuItem[] {
                    SeperatorItem = new MenuItem("-"),
                    ConfigItem = CreateMenuItem("Edit Servers...", new EventHandler(Config_Click)),
                    new MenuItem("-"),
                    CreateMenuItem("Share Server Config...", new EventHandler(QRCodeItem_Click)),
                    CreateMenuItem("Scan QRCode from Screen...", new EventHandler(ScanQRCodeItem_Click)),
                    CreateMenuItem("Import URL from Clipboard...", new EventHandler(ImportURLItem_Click))
                }),
                proxyItem = CreateMenuItem("Forward Proxy...", new EventHandler(proxyItem_Click)),
                new MenuItem("-"),
                AutoStartupItem = CreateMenuItem("Start on Boot", new EventHandler(AutoStartupItem_Click)),
                ShareOverLANItem = CreateMenuItem("Allow Clients from LAN", new EventHandler(ShareOverLANItem_Click)),
                new MenuItem("-"),
                CreateMenuItem("Show Logs...", new EventHandler(ShowLogItem_Click)),
                VerboseLoggingToggleItem = CreateMenuItem("Verbose Logging", new EventHandler(VerboseLoggingToggleItem_Click) ),
                CreateMenuGroup("Updates...", new MenuItem[] {
                    CreateMenuItem("Check for Updates...", new EventHandler(checkUpdatesItem_Click)),
                    new MenuItem("-"),
                    autoCheckUpdatesToggleItem = CreateMenuItem("Check for Updates at Startup", new EventHandler(autoCheckUpdatesToggleItem_Click)),
                    checkPreReleaseToggleItem = CreateMenuItem("Check Pre-release Version", new EventHandler(checkPreReleaseToggleItem_Click)),
                }),
                CreateMenuGroup("Feeds...", new MenuItem[] {
                    feedSettingsItem = CreateMenuItem("Feed source settings...", new EventHandler(ShowFeedSettings)),
                    updateFeedItem = CreateMenuItem("Update all feeds...", new EventHandler(UpdateFeeds_Click)),
                    new MenuItem("-"),
                    CreateMenuItem("Update feeds at Startup", new EventHandler(autoCheckUpdates), controller.GetConfigurationCopy().autoUpdateFeeds),
                }),
                CreateMenuItem("About...", new EventHandler(AboutItem_Click)),
                new MenuItem("-"),
                CreateMenuItem("Quit", new EventHandler(Quit_Click))
            });
        }

        #endregion

        private void ReloadServer(object sender, EventArgs e)
        {
            if(sender != null && sender.GetType() == typeof(string))
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks"), I18N.GetString("Shadowsocks started at port") + " " + sender, ToolTipIcon.Info, 5000);
            }
        }

        private void UpdateFeeds_Click(object sender, EventArgs e)
        {
            feedUpdater.UpdateFeed();
        }

        private void autoCheckUpdates(object sender, EventArgs e)
        {
            Configuration configuration = controller.GetConfigurationCopy();
            ((MenuItem)sender).Checked = !configuration.autoUpdateFeeds;
            controller.ToggleFeedAutoUpdate(!configuration.autoUpdateFeeds);
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
            UpdateTrayIcon();
        }

        void controller_ShareOverLANStatusChanged(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = controller.GetConfigurationCopy().shareOverLan;
        }

        void controller_VerboseLoggingStatusChanged(object sender, EventArgs e)
        {
            VerboseLoggingToggleItem.Checked = controller.GetConfigurationCopy().isVerboseLogging;
        }

        void controller_FileReadyToOpen(object sender, ShadowsocksController.PathEventArgs e)
        {
            string argument = @"/select, " + e.Path;

            System.Diagnostics.Process.Start("explorer.exe", argument);
        }

        void ShowBalloonTip(string title, string content, ToolTipIcon icon, int timeout)
        {
            _notifyIcon.BalloonTipTitle = title;
            _notifyIcon.BalloonTipText = content;
            _notifyIcon.BalloonTipIcon = icon;
            _notifyIcon.ShowBalloonTip(timeout);
        }

        void updateChecker_CheckUpdateCompleted(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                ShowBalloonTip(String.Format(I18N.GetString("Shadowsocks {0} Update Found"), updateChecker.LatestVersionNumber + updateChecker.LatestVersionSuffix), I18N.GetString("Click here to update"), ToolTipIcon.Info, 5000);
            }
            else if (!_isStartupChecking)
            {
                ShowBalloonTip(I18N.GetString("Shadowsocks"), I18N.GetString("No update is available"), ToolTipIcon.Info, 5000);
            }
            _isStartupChecking = false;
        }

        void notifyIcon1_BalloonTipClicked(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                updateChecker.NewVersionFound = false; /* Reset the flag */
                if (System.IO.File.Exists(updateChecker.LatestVersionLocalName))
                {
                    string argument = "/select, \"" + updateChecker.LatestVersionLocalName + "\"";
                    System.Diagnostics.Process.Start("explorer.exe", argument);
                }
            }
        }

        private void _notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            if (updateChecker.NewVersionFound)
            {
                updateChecker.NewVersionFound = false; /* Reset the flag */
            }
        }

        private void LoadCurrentConfiguration()
        {
            Configuration config = controller.GetConfigurationCopy();
            UpdateServersMenu();
            ShareOverLANItem.Checked = config.shareOverLan;
            VerboseLoggingToggleItem.Checked = config.isVerboseLogging;
            AutoStartupItem.Checked = AutoStartup.Check();
            UpdateUpdateMenu();
        }

        private void UpdateServersMenu()
        {
            var items = ServersItem.MenuItems;
            while (items[0] != SeperatorItem)
            {
                items.RemoveAt(0);
            }
            int i = 0;
            foreach (var strategy in controller.GetStrategies())
            {
                MenuItem item = new MenuItem(strategy.Name)
                {
                    Tag = strategy.ID
                };
                item.Click += AStrategyItem_Click;
                items.Add(i, item);
                i++;
            }

            // user wants a seperator item between strategy and servers menugroup
            items.Add(i++, new MenuItem("-"));

            int strategyCount = i;
            Configuration configuration = controller.GetConfigurationCopy();
            foreach (var server in configuration.configs)
            {
                MenuItem item = new MenuItem(server.FriendlyName())
                {
                    Tag = i - strategyCount
                };
                item.Click += AServerItem_Click;
                items.Add(i, item);
                i++;
            }

            foreach (MenuItem item in items)
            {
                if (item.Tag != null && (item.Tag.ToString() == configuration.index.ToString() || item.Tag.ToString() == configuration.strategy))
                {
                    item.Checked = true;
                }
            }
        }

        private void ShowConfigForm()
        {
            if (configForm != null)
            {
                configForm.Activate();
            }
            else
            {
                configForm = new ConfigForm(controller);
                configForm.Show();
                configForm.Activate();
                configForm.FormClosed += configForm_FormClosed;
            }
        }

        private void ShowProxyForm()
        {
            if (proxyForm != null)
            {
                proxyForm.Activate();
            }
            else
            {
                proxyForm = new ProxyForm(controller);
                proxyForm.Show();
                proxyForm.Activate();
                proxyForm.FormClosed += proxyForm_FormClosed;
            }
        }

        private void ShowLogForm()
        {
            if (logForm != null)
            {
                logForm.Activate();
            }
            else
            {
                logForm = new LogForm(controller, Logging.LogFilePath);
                logForm.Show();
                logForm.Activate();
                logForm.FormClosed += logForm_FormClosed;
            }
        }

        void logForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            logForm.Dispose();
            logForm = null;
            Utils.ReleaseMemory(true);
        }

        void configForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            configForm.Dispose();
            configForm = null;
            Utils.ReleaseMemory(true);
            if (_isFirstRun)
            {
                CheckUpdateForFirstRun();
                ShowFirstTimeBalloon();
                _isFirstRun = false;
            }
        }

        void proxyForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            proxyForm.Dispose();
            proxyForm = null;
            Utils.ReleaseMemory(true);
        }

        private void Config_Click(object sender, EventArgs e)
        {
            ShowConfigForm();
        }

        private void Quit_Click(object sender, EventArgs e)
        {
            controller.Stop();
            _notifyIcon.Visible = false;
            Application.Exit();
        }

        private void CheckUpdateForFirstRun()
        {
            Configuration config = controller.GetConfigurationCopy();
            if (config.isDefault) return;
            _isStartupChecking = true;
            updateChecker.CheckUpdate(config, 3000);
        }

        private void ShowFirstTimeBalloon()
        {
            _notifyIcon.BalloonTipTitle = I18N.GetString("Shadowsocks is here");
            _notifyIcon.BalloonTipText = I18N.GetString("You can configure Shadowsocks in the context menu");
            _notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
            _notifyIcon.ShowBalloonTip(0);
        }

        private void AboutItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://blog.zhangzisu.cn/");
        }

        private void NotifyIcon1_Click(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                ShowLogForm();
            }
        }

        private void notifyIcon1_DoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowConfigForm();
            }
        }

        private void ShareOverLANItem_Click(object sender, EventArgs e)
        {
            ShareOverLANItem.Checked = !ShareOverLANItem.Checked;
            controller.ToggleShareOverLAN(ShareOverLANItem.Checked);
        }

        private void AServerItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.SelectServerIndex((int)item.Tag);
        }

        private void AStrategyItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;
            controller.SelectStrategy((string)item.Tag);
        }

        private void VerboseLoggingToggleItem_Click(object sender, EventArgs e)
        {
            VerboseLoggingToggleItem.Checked = !VerboseLoggingToggleItem.Checked;
            controller.ToggleVerboseLogging(VerboseLoggingToggleItem.Checked);
        }

        private void QRCodeItem_Click(object sender, EventArgs e)
        {
            QRCodeForm qrCodeForm = new QRCodeForm(controller.GetServerURLForCurrentServer());
            //qrCodeForm.Icon = this.Icon;
            // TODO
            qrCodeForm.Show();
        }

        private void ScanQRCodeItem_Click(object sender, EventArgs e)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                using (Bitmap fullImage = new Bitmap(screen.Bounds.Width,
                                                screen.Bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(fullImage))
                    {
                        g.CopyFromScreen(screen.Bounds.X,
                                         screen.Bounds.Y,
                                         0, 0,
                                         fullImage.Size,
                                         CopyPixelOperation.SourceCopy);
                    }
                    int maxTry = 10;
                    for (int i = 0; i < maxTry; i++)
                    {
                        int marginLeft = (int)((double)fullImage.Width * i / 2.5 / maxTry);
                        int marginTop = (int)((double)fullImage.Height * i / 2.5 / maxTry);
                        Rectangle cropRect = new Rectangle(marginLeft, marginTop, fullImage.Width - marginLeft * 2, fullImage.Height - marginTop * 2);
                        Bitmap target = new Bitmap(screen.Bounds.Width, screen.Bounds.Height);

                        double imageScale = (double)screen.Bounds.Width / (double)cropRect.Width;
                        using (Graphics g = Graphics.FromImage(target))
                        {
                            g.DrawImage(fullImage, new Rectangle(0, 0, target.Width, target.Height),
                                            cropRect,
                                            GraphicsUnit.Pixel);
                        }
                        var source = new BitmapLuminanceSource(target);
                        var bitmap = new BinaryBitmap(new HybridBinarizer(source));
                        QRCodeReader reader = new QRCodeReader();
                        var result = reader.decode(bitmap);
                        if (result != null)
                        {
                            var success = controller.AddServerBySSURL(result.Text);
                            QRCodeSplashForm splash = new QRCodeSplashForm();
                            if (success)
                            {
                                splash.FormClosed += splash_FormClosed;
                            }
                            else if (result.Text.StartsWith("http://") || result.Text.StartsWith("https://"))
                            {
                                _urlToOpen = result.Text;
                                splash.FormClosed += openURLFromQRCode;
                            }
                            else
                            {
                                MessageBox.Show(I18N.GetString("Failed to decode QRCode"));
                                return;
                            }
                            double minX = Int32.MaxValue, minY = Int32.MaxValue, maxX = 0, maxY = 0;
                            foreach (ResultPoint point in result.ResultPoints)
                            {
                                minX = Math.Min(minX, point.X);
                                minY = Math.Min(minY, point.Y);
                                maxX = Math.Max(maxX, point.X);
                                maxY = Math.Max(maxY, point.Y);
                            }
                            minX /= imageScale;
                            minY /= imageScale;
                            maxX /= imageScale;
                            maxY /= imageScale;
                            // make it 20% larger
                            double margin = (maxX - minX) * 0.20f;
                            minX += -margin + marginLeft;
                            maxX += margin + marginLeft;
                            minY += -margin + marginTop;
                            maxY += margin + marginTop;
                            splash.Location = new Point(screen.Bounds.X, screen.Bounds.Y);
                            // we need a panel because a window has a minimal size
                            // TODO: test on high DPI
                            splash.TargetRect = new Rectangle((int)minX + screen.Bounds.X, (int)minY + screen.Bounds.Y, (int)maxX - (int)minX, (int)maxY - (int)minY);
                            splash.Size = new Size(fullImage.Width, fullImage.Height);
                            splash.Show();
                            return;
                        }
                    }
                }
            }
            MessageBox.Show(I18N.GetString("No QRCode found. Try to zoom in or move it to the center of the screen."));
        }

        private void ImportURLItem_Click(object sender, EventArgs e)
        {
            var success = controller.AddServerBySSURL(Clipboard.GetText(TextDataFormat.Text));
            if (success)
            {
                ShowConfigForm();
            }
        }

        void splash_FormClosed(object sender, FormClosedEventArgs e)
        {
            ShowConfigForm();
        }

        void openURLFromQRCode(object sender, FormClosedEventArgs e)
        {
            Process.Start(_urlToOpen);
        }

        private void AutoStartupItem_Click(object sender, EventArgs e)
        {
            AutoStartupItem.Checked = !AutoStartupItem.Checked;
            if (!AutoStartup.Set(AutoStartupItem.Checked))
            {
                MessageBox.Show(I18N.GetString("Failed to update registry"));
            }
        }

        private void UpdateUpdateMenu()
        {
            Configuration configuration = controller.GetConfigurationCopy();
            autoCheckUpdatesToggleItem.Checked = configuration.autoCheckUpdate;
            checkPreReleaseToggleItem.Checked = configuration.checkPreRelease;
        }

        private void autoCheckUpdatesToggleItem_Click(object sender, EventArgs e)
        {
            Configuration configuration = controller.GetConfigurationCopy();
            controller.ToggleCheckingUpdate(!configuration.autoCheckUpdate);
            UpdateUpdateMenu();
        }

        private void checkPreReleaseToggleItem_Click(object sender, EventArgs e)
        {
            Configuration configuration = controller.GetConfigurationCopy();
            controller.ToggleCheckingPreRelease(!configuration.checkPreRelease);
            UpdateUpdateMenu();
        }

        private void checkUpdatesItem_Click(object sender, EventArgs e)
        {
            updateChecker.CheckUpdate(controller.GetConfigurationCopy());
        }

        private void proxyItem_Click(object sender, EventArgs e)
        {
            ShowProxyForm();
        }

        private void ShowLogItem_Click(object sender, EventArgs e)
        {
            ShowLogForm();
        }

        public void ShowLogForm_HotKey()
        {
            ShowLogForm();
        }
    }
}
