using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace StreamlineUtilities
{
    public partial class HTTP : IDisposable
    {
        public HTTPResponse DataRequest(HTTPRequest httpRequest)
        {
            HttpConnection httpConnectionData = new HttpConnection();
            httpConnectionData = processHTTPRequest(httpRequest);
            HTTPResponse httpResponse = new HTTPResponse()
            {
                StatusCode = httpConnectionData.StatusCode,
                Response = httpConnectionData.ResponseBody,
            };

            if (httpConnectionData.ResponseStream != null && httpConnectionData.Error == false)
            {
                string characterSet = httpConnectionData.WebResponse.CharacterSet;
                Encoding encoding = null;
                if (string.IsNullOrEmpty(characterSet) == false)
                {
                    encoding = Encoding.GetEncoding(characterSet);
                }

                httpResponse.Response = readStream(httpConnectionData.ResponseStream, encoding);
                httpConnectionData.ResponseStream.Dispose();
            }

            cleanup(httpConnectionData, httpConnectionData.Error);

            return httpResponse;
        }

        public void DataRequestAsync(HTTPRequest httpRequest)
        {
            if (_IsBusy)
            {
                return;
            }

            refreshCancellationTokenSource();
            CancellationToken token = source.Token;
            HTTPResponse httpResponse = new HTTPResponse();
            _IsBusy = true;
            try
            {
                TaskScheduler currentSynchronizationContext = TaskScheduler.FromCurrentSynchronizationContext();

                httpTask = Task.Factory.
                    StartNew(() => httpResponse = DataRequest(httpRequest), token, TaskCreationOptions.None, TaskScheduler.Default).
                    ContinueWith((t) => httpAsyncOperationComplete(HTTPType.DataRequest, httpResponse), currentSynchronizationContext);

            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("HTTP Exception: Error running Data Request task.", Ex);
            }
        }
    }
}
