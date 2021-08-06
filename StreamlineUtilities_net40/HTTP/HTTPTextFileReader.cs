using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamlineUtilities
{
    public partial class HTTP : IDisposable
    {
        // This function will only work on .txt files.
        // If the URL contains some sort of Content-Disposition header and is not directly to the file, this fuction will not work.
        public string[] ReadWebTextFile(string url)
        {
            return webTextFileReader(url).FileLines;
        }

        public void ReadWebTextFileAsync(string url)
        {
            if (_IsBusy)
            {
                return;
            }

            refreshCancellationTokenSource();
            CancellationToken token = source.Token;
            HTTPReadComplete httpReadComplete = new HTTPReadComplete();
            _IsBusy = true;
            try
            {
                TaskScheduler currentSynchronizationContext = TaskScheduler.FromCurrentSynchronizationContext();

                httpTask = Task.Factory.
                    StartNew(() => httpReadComplete = webTextFileReader(url), token, TaskCreationOptions.None, TaskScheduler.Default).
                    ContinueWith((t) => httpAsyncOperationComplete(HTTPType.ReadWebTextFile, httpReadComplete), currentSynchronizationContext);
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("HTTP Exception: Error running Text File Reader task.", Ex);
            }
        }

        private HTTPReadComplete webTextFileReader(string url)
        {
            HTTPReadComplete httpReadComplete = new HTTPReadComplete();
            try
            {
                if (Path.GetExtension(url).ToLower() != ".txt")
                {
                    httpReadComplete.Error = true;
                    return httpReadComplete;
                }
            }
            catch (Exception Ex)
            {
                httpReadComplete.Error = true;
                LogUtilities.Exception("HTTP Exception: Text File Reader failed to get url extension.", Ex);
                return httpReadComplete;
            }

            string[] fileLines = null;
            HttpConnection httpConnectionData = null;
            try
            {
                // TODO (DB): I put this in here for a reason. Not sure why it is here but will figure it out in the future.
                // DateTime startTime = DateTime.UtcNow;
                
                httpConnectionData = processHTTPRequest(new HTTPRequest() { URL = url, });
                if (httpConnectionData.ResponseStream == null || httpConnectionData.Error == true)
                {
                    cleanup(httpConnectionData, httpConnectionData.Error);
                    httpReadComplete.Error = true;
                    return httpReadComplete;
                }

                if (httpConnectionData.ResponseStream != null)
                {
                    httpConnectionData.ResponseStream.ReadTimeout = 1500;
                    fileLines = readStreamToArray(httpConnectionData.ResponseStream, null);
                }

                if (fileLines == null)
                {
                    httpReadComplete.Error = true;
                    httpReadComplete.FileLines = new string[0];
                }
                else
                {
                    httpReadComplete.FileLines = fileLines;
                }

                return httpReadComplete;
            }
            catch (Exception Ex)
            {
                httpReadComplete.Error = true;
                LogUtilities.Exception("HTTP Exception: Error with Text File Reader stream reader. Url: " + url, Ex);
                return httpReadComplete;
            }
            finally
            {
                if (httpConnectionData.ResponseStream != null)
                {
                    httpConnectionData.ResponseStream.Dispose();
                }

                if (httpConnectionData.WebResponse != null)
                {
                    httpConnectionData.WebResponse.Close();
                }
            }
        }
    }
}
