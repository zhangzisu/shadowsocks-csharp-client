using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Shadowsocks.Controller.Service
{
    class FeedUpdater
    {
        private const string UserAgent = "Mozilla/5.0 (Windows NT 5.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/35.0.3319.102 Safari/537.36";

        private ShadowsocksController controller;
        public event EventHandler StartUpdate;
        public event EventHandler FinishUpdate;
        public FeedUpdater(ShadowsocksController controller)
        {
            this.controller = controller;
        }
        public void UpdateFeed()
        {
            StartUpdate?.Invoke(this, new EventArgs());
            controller.DeleteFeed();
            List<string> free = new List<string> {
                "https://freess.cx/images/servers/jp01.png",
                "https://freess.cx/images/servers/jp02.png",
                "https://freess.cx/images/servers/jp03.png",
                "https://freess.cx/images/servers/us01.png",
                "https://freess.cx/images/servers/us02.png",
                "https://freess.cx/images/servers/us03.png"
            };
            foreach(var uri in free)
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
                            controller.AddServerBySSURL(result.Text, true);
                        }
                    }
                    File.Delete(path);
                }catch(Exception e)
                {
                    Logging.Error(e);
                }
            }
            FinishUpdate?.Invoke(this, new EventArgs());
        }
    }
}
