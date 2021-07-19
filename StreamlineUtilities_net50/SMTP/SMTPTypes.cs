using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamlineUtilities
{
    public class SMTPSettings
    {
        // Gmail uses the following:
        // Host = "smtp.gmail.com";
        // Port = 587;
        // All default values work with gmail.

        private string _Host = "";
        public string Host
        {
            get { return _Host; }
            set
            {
                if (value == null)
                {
                    _Host = "";
                }
                else
                {
                    _Host = value;
                }
            }
        }

        public int Port { get; set; } = 0;
        public bool EnableSsl { get; set; } = true;
        public SmtpDeliveryMethod DeliveryMethod { get; set; } = SmtpDeliveryMethod.Network;
        public bool UseDefaultCredentials { get; set; } = false;
        public int Timeout { get; set; } = 20000;
        public NetworkCredential Credentials { get; set; } = null;
    }
    
    public class EmailMessage
    {
        public EmailAddress SenderData { get; set; } = new EmailAddress();

        private List<string> _Receiver = new List<string>();
        public string[] Receiver { get { return _Receiver.ToArray(); } }
        public void ReceiverAdd(string address)
        {
            _Receiver.Add(address);
        }

        private List<string> _CC = new List<string>();
        public string[] CC { get { return _CC.ToArray(); } }
        public void CCAdd(string address)
        {
            _CC.Add(address);
        }

        private List<string> _BCC = new List<string>();
        public string[] BCC { get { return _BCC.ToArray(); } }
        public void BCCAdd(string address)
        {
            _BCC.Add(address);
        }

        private string _Subject = "";
        public string Subject
        {
            get { return _Subject; }
            set
            { 
                if (value == null)
                {
                    _Subject = "";
                }
                else
                {
                    _Subject = value;
                }
            }
        }

        private string _Body = "";
        public string Body
        {
            get { return _Body; }
            set
            {
                if (value == null)
                {
                    _Body = "";
                }
                else
                {
                    _Body = value;
                }
            }
        }

        public bool IsBodyHtml { get; set; } = false;

        private List<string> _Attachment = new List<string>();
        public string[] Attachment { get { return _Attachment.ToArray(); } }
        public void AttachmentAdd(string address)
        {
            _Attachment.Add(address);
        }
    }

    public class EmailAddress
    {
        public string Address { get; set; } = "";
        public string First { get; set; } = "";
        public string Last { get; set; } = "";
        public string FullName
        {
            get 
            {
                if (string.IsNullOrEmpty(First) == false && string.IsNullOrEmpty(Last) == false)
                {
                    return string.Format("{0} {1}", First, Last);
                }
                else if(string.IsNullOrEmpty(First)== false && string.IsNullOrEmpty(Last))
                {
                    return First;
                }
                else if (string.IsNullOrEmpty(First) && string.IsNullOrEmpty(Last) == false)
                {
                    return Last;
                }
                else
                {
                    return "";
                }
            }
        }

        private List<string> _ReplyTo = new List<string>();
        public string[] ReplyTo { get { return _ReplyTo.ToArray(); } }
        public void ReplyToAdd(string address)
        {
            _ReplyTo.Add(address);
        }

        public string DisplayName { get; set; } = "";
        public string Signature { get; set; } = "";
    }

    public class EmailSendResult
    {
        public EmailSendStatus Status { get; set; } = EmailSendStatus.Undefined;

        private List<string> _Messages = new List<string>();
        public string[] Messages { get { return _Messages.ToArray(); } }
        public void MessageAdd(string address)
        {
            _Messages.Add(address);
        }
    }

    public enum EmailSendStatus
    {
        Undefined,
        Complete,
        Failed,
    }
}
