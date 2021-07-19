﻿using System;
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
        private void refreshCancellationTokenSource()
        {
            try
            {
                if (source != null)
                {
                    source.Dispose();
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Error attempting to dispose of a used cancellation token source.", Ex);
            }

            source = new CancellationTokenSource();
        }

        private readonly Uri baseURI = new Uri("http://www.BaseURI.com");
        public string ParseURLFileName(string url)
        {
            string fileName = "";
            try
            {
                Uri uri;
                if (Uri.TryCreate(url, UriKind.Absolute, out uri) == false)
                {
                    uri = new Uri(baseURI, url);
                }

                fileName = Path.GetFileName(uri.LocalPath);
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Error parsing file name from URL: " + url, Ex);
            }

            return fileName;
        }

        private bool acceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        // NOTE: Request string can be null or empty since it is technically an optional field. HTTP requests may not always need to have data written to the request stream.
        private HttpConnection processHTTPRequest(HTTPRequest httpRequest)
        {
            HttpConnection httpConnectionData = new HttpConnection();

            // ------------------------------------------------------------------------------------------------------------------
            // Sets up the request headers.
            try
            {
                if (httpRequest.AcceptAllCerts)
                {
                    ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(acceptAllCertifications);
                }
                
                httpConnectionData.WebRequest = (HttpWebRequest)WebRequest.Create(httpRequest.URL);
                httpConnectionData.WebRequest.Timeout = Timeout * 1000;
                httpConnectionData.WebRequest.ReadWriteTimeout = Timeout * 1000;
                httpConnectionData.WebRequest.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
                httpConnectionData.WebRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

                foreach (HTTPHeader httpHeader in httpRequest.Headers)
                {
                    httpConnectionData.WebRequest.Headers.Add(httpHeader.Key, httpHeader.Value);
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Error trying to creating HTTP Web Request. Url: " + httpRequest.URL, Ex);
                return cleanup(httpConnectionData, true);
            }

            if (httpRequest.Authenticate)
            {
                httpConnectionData.WebRequest.PreAuthenticate = httpRequest.Authenticate;
            }

            if (string.IsNullOrEmpty(httpRequest.ContentType) == false)
            {
                httpConnectionData.WebRequest.ContentType = httpRequest.ContentType;
            }

            // ------------------------------------------------------------------------------------------------------------------
            // Sets the Request Method
            switch (httpRequest.Method)
            {
                case RequestMethod.GET:
                    httpConnectionData.WebRequest.Method = WebRequestMethods.Http.Get;
                    break;

                case RequestMethod.POST:
                    httpConnectionData.WebRequest.Method = WebRequestMethods.Http.Post;
                    break;

                case RequestMethod.PUT:
                    httpConnectionData.WebRequest.Method = WebRequestMethods.Http.Put;
                    break;

                case RequestMethod.HEAD:
                    httpConnectionData.WebRequest.Method = WebRequestMethods.Http.Head;
                    break;

                case RequestMethod.CONNECT:
                    httpConnectionData.WebRequest.Method = WebRequestMethods.Http.Connect;
                    break;

                case RequestMethod.DELETE:
                    httpConnectionData.WebRequest.Method = "DELETE";
                    break;

                case RequestMethod.PATCH:
                    httpConnectionData.WebRequest.Method = "PATCH";
                    break;

                case RequestMethod.OPTIONS:
                    httpConnectionData.WebRequest.Method = "OPTIONS";
                    break;

                case RequestMethod.TRACE:
                    httpConnectionData.WebRequest.Method = "TRACE";
                    break;
            }

            // ------------------------------------------------------------------------------------------------------------------
            // Sets up for writing to a request stream.
            if (string.IsNullOrEmpty(httpRequest.RequestString) == false)
            {
                bool requestWritten = true;

                Stream requestStream = null;
                try
                {
                    byte[] requestData = Encoding.UTF8.GetBytes(httpRequest.RequestString);
                    httpConnectionData.WebRequest.ContentLength = requestData.Length;
                    httpConnectionData.WebRequest.AllowWriteStreamBuffering = true;

                    requestStream = httpConnectionData.WebRequest.GetRequestStream();
                    requestStream.Write(requestData, 0, requestData.Length);
                }
                catch (Exception Ex)
                {
                    requestWritten = false;
                    LogStreamline.Exception("HTTP Exception: Error attempting to write to request stream. Url: " + httpRequest.URL, Ex);
                }
                finally
                {
                    if (requestStream != null)
                    {
                        requestStream.Dispose();
                    }
                }

                if (requestWritten == false)
                {
                    return cleanup(httpConnectionData, true);
                }
            }

            // ------------------------------------------------------------------------------------------------------------------
            // Performs an HTTP(s) Web Request.
            try
            {
                httpConnectionData.WebResponse = (HttpWebResponse)httpConnectionData.WebRequest.GetResponse();
                if (httpConnectionData.WebResponse == null)
                {
                    LogStreamline.LogEntry("HTTP Error: Failed to process HTTP Web Request. Response is null. Url: " + httpRequest.URL);
                    return cleanup(httpConnectionData, true);
                }
            }
            catch (WebException WebEx)
            {
                httpConnectionData.Error = true;
                httpConnectionData.ResponseBody = readStream(WebEx.Response.GetResponseStream(), null);

                if (WebEx.Status == WebExceptionStatus.ProtocolError)
                {
                    try
                    {
                        httpConnectionData.StatusCode = (int)((HttpWebResponse)WebEx.Response).StatusCode;
                    }
                    catch (Exception Ex)
                    {
                        LogStreamline.Exception("HTTP Exception: Error trying to read response status code from Web Exception. Url: " + httpRequest.URL, Ex);
                    }
                }

                HTTPHeader[] responseHeaders = httpHeaderCollection(WebEx.Response.Headers);
                string responseHeadersText = "";
                for (int i = 0; i < responseHeaders.Length; i++)
                {
                    responseHeadersText = responseHeadersText + "   " + responseHeaders[i].Key + ": " + responseHeaders[i].Value + Environment.NewLine;
                }

                HTTPHeader[] requestHeaders = httpHeaderCollection(httpConnectionData.WebRequest.Headers);
                string requestHeadersText = "";
                for (int i = 0; i < requestHeaders.Length; i++)
                {
                    if (requestHeaders[i].Key.ToLower() == "authorization" && httpRequest.AuthMask)
                    {
                        // This code is used to generate a partially mased auth token to show in a log file. We are just editing the string here a bit.
                        string maskedToken = "*****";
                        for (int c = ((requestHeaders[i].Value.Length / 4) * 3); c < requestHeaders[i].Value.Length; c++)
                        {
                            maskedToken = maskedToken + requestHeaders[i].Value[c];
                        }

                        requestHeadersText = requestHeadersText + "   " + requestHeaders[i].Key + ": " + maskedToken + Environment.NewLine;
                    }
                    else
                    {
                        requestHeadersText = requestHeadersText + "   " + requestHeaders[i].Key + ": " + requestHeaders[i].Value + Environment.NewLine;
                    }
                }

                requestHeadersText = requestHeadersText.TrimEnd();

                string logError =
                    "HTTP Web Exception: Error Message: " + WebEx.Message + Environment.NewLine +
                    "Response Headers:" + Environment.NewLine + responseHeadersText +
                    "Response Message:" + Environment.NewLine + "   " + httpConnectionData.ResponseBody +
                    "Request Url: " + Environment.NewLine + "   " + httpRequest.URL + Environment.NewLine +
                    "Request Headers:" + Environment.NewLine + requestHeadersText;

                if (httpRequest.RequestMask == false && string.IsNullOrEmpty(httpRequest.RequestString) == false)
                {
                    logError = logError + Environment.NewLine + "Request String:" + Environment.NewLine + "   " + httpRequest.RequestString;
                }

                LogStreamline.Exception(logError, WebEx);
                
                return cleanup(httpConnectionData, true);
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: General error when attempting to get response. Url: " + httpRequest.URL, Ex);
                return cleanup(httpConnectionData, true);
            }

            // ------------------------------------------------------------------------------------------------------------------
            // Sets the response code.
            try
            {
                if (httpConnectionData.WebResponse != null)
                {
                    httpConnectionData.StatusCode = (int)httpConnectionData.WebResponse.StatusCode;
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Error trying to read response status code. Url: " + httpRequest.URL, Ex);
            }

            if (httpConnectionData.StatusCode != 200)
            {
                LogStreamline.LogEntry("HTTP Error: Bad response: " + Convert.ToString(httpConnectionData.StatusCode) + ". URL: " + httpRequest.URL);
                return cleanup(httpConnectionData, true);
            }

            try
            {
                httpConnectionData.ResponseStream = httpConnectionData.WebResponse.GetResponseStream();
                if (httpConnectionData.ResponseStream == null)
                {
                    LogStreamline.LogEntry("HTTP Error: Null response stream. URL: " + httpRequest.URL);
                    return cleanup(httpConnectionData, true);
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception. Error trying to read response stream. Url: " + httpRequest.URL, Ex);
                return cleanup(httpConnectionData, true);
            }

            try
            {
                if (httpConnectionData.WebResponse != null)
                {
                    httpConnectionData.PayloadSize = Convert.ToInt64(httpConnectionData.WebResponse.Headers.Get("Content-Length"));
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Error trying to get Content-Length from header data. Url: " + httpRequest.URL, Ex);
            }

            return httpConnectionData;
        }

        private string[] readStreamToArray(Stream stream, Encoding encoding)
        {
            List<string> streamLines = new List<string>();
            StreamReader streamReader = null;
            try
            {
                if (encoding == null)
                {
                    streamReader = new StreamReader(stream);
                }
                else
                {
                    streamReader = new StreamReader(stream, encoding);
                }

                string line = "";
                while ((line = streamReader.ReadLine()) != null)
                {
                    streamLines.Add(line);
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Stream to array reader encountered an issue.", Ex);
            }
            finally
            {
                if (streamReader != null)
                {
                    streamReader.Dispose();
                }
            }

            return streamLines.ToArray();
        }

        private string readStream(Stream stream, Encoding encoding)
        {
            StreamReader streamReader = null;
            try
            {
                if (encoding == null)
                {
                    streamReader = new StreamReader(stream);
                }
                else
                {
                    streamReader = new StreamReader(stream, encoding);
                }

                return streamReader.ReadToEnd();
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Stream to string reader encountered an issue.", Ex);
                return "";
            }
            finally
            {
                if (streamReader != null)
                {
                    streamReader.Dispose();
                }
            }
        }

        private HTTPHeader[] httpHeaderCollection(WebHeaderCollection webHeaderCollection)
        {
            List<HTTPHeader> httpHeaderList = new List<HTTPHeader>();
            try
            {
                for (int i = 0; i < webHeaderCollection.Count; i++)
                {
                    httpHeaderList.Add(new HTTPHeader() { Key = webHeaderCollection.Keys[i], Value = webHeaderCollection[i] });
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Error parsing header collection.", Ex);
            }

            return httpHeaderList.ToArray();
        }

        // This was initially created to just determine the size of a file. Might not be needed but leaving it here since I already wrote it.
        private long getHTTPRequestSize(string url)
        {
            HttpWebResponse httpWebSizeResponse = null;
            long payloadSize = -1;
            try
            {
                HttpWebRequest httpWebSizeRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebSizeRequest.Timeout = Timeout * 1000;
                httpWebSizeRequest.ReadWriteTimeout = Timeout * 1000;
                httpWebSizeRequest.Method = WebRequestMethods.Http.Head;
                httpWebSizeResponse = (HttpWebResponse)httpWebSizeRequest.GetResponse();

                if (httpWebSizeResponse != null)
                {
                    payloadSize = Convert.ToInt64(httpWebSizeResponse.Headers.Get("Content-Length"));
                }
                else
                {
                    LogStreamline.LogEntry("HTTP Error: Size request failure. Web response is null. Url: " + url);
                }
            }
            catch (Exception Ex)
            {
                LogStreamline.Exception("HTTP Exception: Error processing size request. Url: " + url, Ex);
            }
            finally
            {
                if (httpWebSizeResponse != null)
                {
                    httpWebSizeResponse.Close();
                }
            }

            return payloadSize;
        }

        private void httpAsyncOperationComplete(HTTPType httpType, object httpCompleteData)
        {
            // If the IsBusy is false, we just want to fizzle out. This means the Task was cancelled already.
            if (IsBusy == false)
            {
                return;
            }
            _IsBusy = false;

            if (httpCompleteData == null)
            {
                return;
            }

            switch (httpType)
            {
                case HTTPType.DataRequest:
                    if (DataRequestComplete == null)
                    {
                        break;
                    }

                    HTTPResponse httpResponse = new HTTPResponse();
                    if (httpCompleteData is HTTPResponse)
                    {
                        httpResponse = httpCompleteData as HTTPResponse;
                    }

                    DataRequestComplete(this, new HTTPResponse()
                    {
                        Response = httpResponse.Response,
                        StatusCode = httpResponse.StatusCode,
                    });

                    break;

                case HTTPType.ReadWebTextFile:
                    if (FileReadComplete == null)
                    {
                        break;
                    }

                    HTTPReadComplete httpReadComplete = new HTTPReadComplete() { Error = true, };
                    if (httpCompleteData is HTTPReadComplete)
                    {
                        httpReadComplete = httpCompleteData as HTTPReadComplete;
                    }

                    FileReadComplete(this, new HTTPReadComplete()
                    {
                        FileLines = httpReadComplete.FileLines,
                        Error = httpReadComplete.Error,
                    });

                    break;

                case HTTPType.Download:
                    if (DownloadComplete == null)
                    {
                        break;
                    }

                    HTTPComplete httpComplete = new HTTPComplete() { Error = true, };
                    if (httpCompleteData is HTTPComplete)
                    {
                        httpComplete = httpCompleteData as HTTPComplete;
                    }

                    DownloadComplete(this, new HTTPComplete()
                    {
                        PayLoadSize = httpComplete.PayLoadSize,
                        PercentageComplete = httpComplete.PercentageComplete,
                        BytesReceived = httpComplete.BytesReceived,
                        Cancelled = httpComplete.Cancelled,
                        Error = httpComplete.Error,
                    });

                    break;
            }

            Dispose();
        }

        private HttpConnection cleanup(HttpConnection httpConnectionData, bool error)
        {
            httpConnectionData.Error = error;

            if (httpConnectionData == null)
            {
                return new HttpConnection();
            }

            if (httpConnectionData.WebResponse != null)
            {
                httpConnectionData.WebResponse.Close();
                httpConnectionData.WebResponse = null;
            }

            if (httpConnectionData.ResponseStream != null)
            {
                httpConnectionData.ResponseStream.Dispose();
            }

            return httpConnectionData;
        }

        private void disposeTask(Task task)
        {
            if (task != null && task.IsCompleted)
            {
                task.Dispose();
            }
        }

        // Although this class method internally cleans up it is still good to expose this method just in case.
        public void Dispose()
        {
            try
            {
                if (httpTask != null && httpTask.IsCompleted)
                {
                    httpTask.Dispose();
                    httpTask = null;
                }

                if (source != null)
                {
                    source.Dispose();
                    source = null;
                }
            }
            catch (Exception Ex)
            {
                // The above may fail is the task is somehow dead locked. Will look into this more.
                LogStreamline.Exception("HTTP Exception: Error with disposing resources.", Ex);
            }
        }
    }
}