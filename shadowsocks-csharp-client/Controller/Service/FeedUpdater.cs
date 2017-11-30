using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Shadowsocks.Controller.Service
{
    public class FeedUpdater
    {
        private const string UserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36";
        private static string FEED_KEY = "feed" + Application.StartupPath.GetHashCode();

        private ShadowsocksController controller;
        public event EventHandler StartUpdate;
        public event EventHandler FinishUpdate;
        public List<string> feeds;
        public FeedUpdater(ShadowsocksController controller)
        {
            this.controller = controller;
            try
            {
                RegistryKey key = Util.Utils.OpenRegKey("shadowsocks", false);
                string configContent = (string)key.GetValue(FEED_KEY);
                if (configContent == null) configContent = "";
                feeds = JsonConvert.DeserializeObject<List<string>>(configContent);
                if (feeds == null) feeds = new List<string>();
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }
        public void saveFeed()
        {
            try
            {
                RegistryKey key = Util.Utils.OpenRegKey("shadowsocks", true);
                key.SetValue(FEED_KEY, JsonConvert.SerializeObject(feeds, Formatting.None));
            }
            catch (IOException e)
            {
                Logging.LogUsefulException(e);
            }
        }
        public void exportFeed(string path)
        {
            using(var fs = new FileStream(path, FileMode.Create))
            {
                using(var writer = new StreamWriter(fs))
                {
                    writer.Write(JsonConvert.SerializeObject(feeds, Formatting.Indented));
                    writer.Flush();
                }
            }
        }
        public void importFeed(string path)
        {
            string text = File.ReadAllText(path);
            var of = JsonConvert.DeserializeObject<List<string>>(text);
            foreach(var obj in of)
            {
                if (feeds.Contains(obj)) continue;
                feeds.Add(obj);
            }
            saveFeed();
        }
        public void UpdateFeed()
        {
            StartUpdate?.Invoke(this, new EventArgs());
            controller.DeleteFeed();
            new System.Threading.Thread(() =>
            {
                foreach (var uri in feeds)
                {
                    try
                    {
                        WebClient webClient = new WebClient();
                        webClient.Headers.Add("User-Agent", UserAgent);
                        string path = Util.Utils.GetTempPath() + "feed-qr.png";
                        webClient.DownloadFile(uri, path);
                        using (var source = new Bitmap(path))
                        {
                            var bitmap = new BinaryBitmap(new HybridBinarizer(new BitmapLuminanceSource(source)));
                            QRCodeReader reader = new QRCodeReader();
                            var result = reader.decode(bitmap);
                            if (result != null)
                            {
                                if (controller.AddServerBySSURL(result.Text, true))
                                    FinishUpdate?.Invoke(uri, new EventArgs());
                            }
                        }
                        File.Delete(path);
                    }
                    catch (Exception e)
                    {
                        Logging.Error(e);
                    }
                }
                FinishUpdate?.Invoke(null, new EventArgs());
            }).Start();
        }
    }
}
