using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamlineUtilities
{
    public partial class HTTP : IDisposable
    {
        private class HttpConnection
        {
            public HttpWebRequest WebRequest { get; set; } = null;
            public HttpWebResponse WebResponse { get; set; } = null;
            public Stream ResponseStream { get; set; } = null;
            public long PayloadSize { get; set; } = 0;
            public int StatusCode { get; set; } = -1;
            public string ResponseBody { get; set; } = "";
            public bool Error { get; set; } = false;
        }

        private enum HTTPType
        {
            DataRequest,
            ReadWebTextFile,
            Download,
        }

        private Task httpTask = null;
        private CancellationTokenSource source = null;

        public event EventHandler<HTTPResponse> DataRequestComplete = null;

        public event EventHandler<HTTPReadComplete> FileReadComplete = null;

        public event EventHandler<HTTPProgress> DownloadProgress = null;
        public event EventHandler<HTTPComplete> DownloadComplete = null;

        private bool _CancellationRequested = false;
        public bool CancellationRequested
        {
            get
            {
                return _CancellationRequested;
            }
        }

        private bool _IsBusy = false;
        public bool IsBusy
        {
            get
            {
                return _IsBusy;
            }
        }

        public ReportProgressType ReportType { get; set; } = ReportProgressType.Percentage;

        private int _ProgessPercentage = 1;
        public int ProgessPercentage
        {
            get
            {
                return _ProgessPercentage;
            }
            set
            {
                if (value > 0 && value < 101)
                {
                    _ProgessPercentage = value;
                }
            }
        }

        private int _Timeout = 120;
        public int Timeout
        {
            get
            {
                return _Timeout;
            }
            set
            {
                if (value > 0 && value < 72001)
                {
                    _Timeout = value;
                }
            }
        }

        private int _BufferSize = 4096;
        public int BufferSize
        {
            get
            {
                return _BufferSize;
            }
            set
            {
                if (value > 255 && value < 8193)
                {
                    _BufferSize = value;
                }
            }
        }
    }
}
