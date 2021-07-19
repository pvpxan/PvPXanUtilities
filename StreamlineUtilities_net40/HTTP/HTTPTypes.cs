using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StreamlineUtilities
{
    public class HTTPRequest
    {
        public string URL { get; set; } = "";
        public string ContentType { get; set; } = "";
        public string RequestString { get; set; } = "";
        public RequestMethod Method { get; set; } = RequestMethod.GET;
        public HTTPHeader[] Headers { get; set; } = new HTTPHeader[0];
        public bool Authenticate { get; set; } = false;
        public bool AuthMask { get; set; } = true;
        public bool AcceptAllCerts { get; set; } = false;
        public bool RequestMask { get; set; } = false;
    }
    
    public class HTTPHeader
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }

    public class HTTPResponse : EventArgs
    {
        public string Response { get; set; } = "";
        public int StatusCode { get; set; } = -1;
        public bool Error { get; set; } = false;
    }

    public class HTTPReadComplete : EventArgs
    {
        public string[] FileLines { get; set; } = null;
        public bool Error { get; set; } = false;
    }

    public class HTTPProgress : EventArgs
    {
        public long BytesReceived { get; set; } = 0;
        public long PayLoadSize { get; set; } = 0;
        public int PercentageComplete { get; set; } = 0;
    }

    public class HTTPComplete : EventArgs
    {
        public long PayLoadSize { get; set; } = 0;
        public int PercentageComplete { get; set; } = 0;
        public long BytesReceived { get; set; } = 0;
        public bool Cancelled { get; set; } = false;
        public bool Success { get; set; } = false;
        public bool Error { get; set; } = false;
    }

    public enum ReportProgressType
    {
        Percentage,
        Bytes,
    }

    public enum RequestMethod
    {
        GET,
        POST,
        PUT,
        HEAD,
        CONNECT,
        DELETE,
        PATCH,
        OPTIONS,
        TRACE,
    }
}
