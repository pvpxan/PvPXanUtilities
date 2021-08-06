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
    public class SMTP
    {
        private SMTPSettings smtpSettings = null;
        public SMTP(SMTPSettings smtpSettings)
        {
            this.smtpSettings = smtpSettings;
        }

        // Checks if credentials are in a valid format.
        public bool ValidateCredentials()
        {
            try
            {
                if (smtpSettings == null ||
                    string.IsNullOrEmpty(smtpSettings.Credentials.UserName) ||
                    string.IsNullOrEmpty(smtpSettings.Credentials.Password))
                {
                    return false;
                }

                return true;
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("SMTP Exception: Error in email credential check function.", Ex);
                return false;
            }
        }

        private class AddressCheck
        {
            public List<string> Valid { get; set; } = new List<string>();
            public List<string> Invalid { get; set; } = new List<string>();
        }

        private AddressCheck checkAddressList(string[] addressList)
        {
            AddressCheck addressCheckResult = new AddressCheck();
            if (addressList == null || addressList.Length < 1)
            {
                return addressCheckResult;
            }

            foreach (string address in addressList)
            {
                if (ValidateEmailAddress(address))
                {
                    addressCheckResult.Valid.Add(address);
                }
                else
                {
                    addressCheckResult.Invalid.Add(address);
                }
            }

            return addressCheckResult;
        }

        private class AttachmentCheck
        {
            public List<Attachment> Valid { get; set; } = new List<Attachment>();
            public List<string> Invalid { get; set; } = new List<string>();
        }

        private AttachmentCheck checkAttachmentList(string[] attachmentList)
        {
            AttachmentCheck attachmentCheck = new AttachmentCheck();

            foreach (string file in attachmentList)
            {
                if (File.Exists(file))
                {
                    Attachment attachment = generateAttachment(file);
                    if (attachment != null)
                    {
                        attachmentCheck.Valid.Add(attachment);
                    }
                    else
                    {
                        attachmentCheck.Invalid.Add(file);
                    }
                }
                else
                {
                    attachmentCheck.Invalid.Add(file);
                }
            }

            return attachmentCheck;
        }

        private Attachment generateAttachment(string file)
        {
            Attachment attachment = null;
            try
            {
                attachment = new Attachment(file, MediaTypeNames.Application.Octet);
                ContentDisposition contentDisposition = attachment.ContentDisposition;
                contentDisposition.CreationDate = File.GetCreationTime(file);
                contentDisposition.ModificationDate = File.GetLastWriteTime(file);
                contentDisposition.ReadDate = File.GetLastAccessTime(file);
                contentDisposition.FileName = Path.GetFileName(file);
                contentDisposition.Size = new FileInfo(file).Length;
                contentDisposition.DispositionType = DispositionTypeNames.Attachment;
                
                return attachment;
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("SMTP Exception: Error generating message attachment.", Ex);
                return null;
            }
        }

        // Sends an emails.
        public EmailSendResult Send(EmailMessage emailMessageData)
        {
            EmailSendResult emailSendResult = new EmailSendResult();

            // Makes sure the class has credentials that are not null or empty.
            if (ValidateCredentials() == false)
            {
                emailSendResult.Status = EmailSendStatus.Failed;
                emailSendResult.MessageAdd("Provided email account credentials are not valid.");
                return emailSendResult;
            }

            // To send an email, we need at least 1 receiver.
            if (emailMessageData.Receiver.Length < 1)
            {
                emailSendResult.Status = EmailSendStatus.Failed;
                emailSendResult.MessageAdd("Receiver email address list is empty");
                return emailSendResult;
            }

            // Checks to make sure there is at least 1 valid receiver address.
            List<string> receiverAddresses = new List<string>();
            foreach (string receiverData in emailMessageData.Receiver)
            {
                if (ValidateEmailAddress(receiverData))
                {
                    receiverAddresses.Add(receiverData);
                }
                else
                {
                    emailSendResult.MessageAdd("Email address is not valid. Address: " + receiverData);
                }
            }

            if (receiverAddresses.Count == 0)
            {
                emailSendResult.Status = EmailSendStatus.Failed;
                emailSendResult.MessageAdd("Unable to process email. All submitted Receiver addresses are not valid.");
                return emailSendResult;
            }

            AddressCheck addressCheckReplyTo = checkAddressList(emailMessageData.SenderData.ReplyTo);
            foreach (string address in addressCheckReplyTo.Invalid)
            {
                emailSendResult.MessageAdd("Invalid ReplyTo address: " + address);
            }

            AddressCheck addressCheckCC = checkAddressList(emailMessageData.CC);
            foreach (string address in addressCheckCC.Invalid)
            {
                emailSendResult.MessageAdd("Invalid CC address: " + address);
            }

            AddressCheck addressCheckBCC = checkAddressList(emailMessageData.BCC);
            foreach (string address in addressCheckCC.Invalid)
            {
                emailSendResult.MessageAdd("Invalid BCC address: " + address);
            }

            AttachmentCheck attachmentCheck = checkAttachmentList(emailMessageData.Attachment);
            foreach (string file in attachmentCheck.Invalid)
            {
                emailSendResult.MessageAdd("Invalid attachment file: " + file);
            }

            MailMessage mailMessage = null;
            try
            {
                mailMessage = new MailMessage();

                if (string.IsNullOrEmpty(emailMessageData.SenderData.Address) == false)
                {
                    mailMessage.From = new MailAddress(emailMessageData.SenderData.Address, emailMessageData.SenderData.DisplayName);
                }

                // Adds receiver addresses.
                foreach (string address in receiverAddresses)
                {
                    mailMessage.To.Add(address);
                }

                // Adds ReplyTo addresses.
                if (addressCheckReplyTo.Valid.Count > 0)
                {
                    foreach (string address in addressCheckReplyTo.Valid)
                    {
                        mailMessage.ReplyToList.Add(address);
                    }
                }

                // Adds CC addresses.
                if (addressCheckCC.Valid.Count > 0)
                {
                    foreach (string address in addressCheckCC.Valid)
                    {
                        mailMessage.CC.Add(address);
                    }
                }

                // Adds BCC addresses.
                if (addressCheckBCC.Valid.Count > 0)
                {
                    foreach (string address in addressCheckBCC.Valid)
                    {
                        mailMessage.Bcc.Add(address);
                    }
                }

                mailMessage.Subject = emailMessageData.Subject;
                mailMessage.Body = emailMessageData.Body;
                mailMessage.IsBodyHtml = emailMessageData.IsBodyHtml;

                foreach (Attachment attachment in attachmentCheck.Valid)
                {
                    mailMessage.Attachments.Add(attachment);
                }

                if (processSMTP(mailMessage) == "success")
                {
                    emailSendResult.Status = EmailSendStatus.Complete;
                }
            }
            catch (Exception Ex)
            {
                string error = "SMTP Exception: Error when building MailMessage.";
                emailSendResult.Status = EmailSendStatus.Failed;
                emailSendResult.MessageAdd(error);
                LogUtilities.Exception(error, Ex);
            }
            finally
            {
                if (mailMessage != null)
                {
                    mailMessage.Dispose();
                }
            }

            return emailSendResult;
        }

        public string processSMTP(MailMessage mailMessage)
        {
            SmtpClient smtpClient = null;
            try
            {
                smtpClient = new SmtpClient();
                smtpClient.Host = smtpSettings.Host;
                smtpClient.Port = smtpSettings.Port;
                smtpClient.EnableSsl = smtpSettings.EnableSsl;
                smtpClient.DeliveryMethod = smtpSettings.DeliveryMethod;
                smtpClient.UseDefaultCredentials = smtpSettings.UseDefaultCredentials;
                smtpClient.Credentials = smtpSettings.Credentials;
                smtpClient.Timeout = smtpSettings.Timeout;

                smtpClient.Send(mailMessage);
                
                return "success";
            }
            catch (Exception Ex)
            {
                string error = "SMTP Exception: Error with SMTP processing of MailMessage.";
                try
                {
                    if (Ex.Message != null && Ex.Message.ToLower().Contains("timed out"))
                    {
                        error = "SMTP Exception: SMTP client send operation has timed out.";
                    }
                }
                catch
                {

                }

                LogUtilities.Exception(error, Ex);
                return error;
            }
            finally
            {
                if (smtpClient != null)
                {
                    smtpClient.Dispose();
                }
            }
        }

        // Email check function using Regex.
        public bool ValidateEmailAddress(string emailAddress)
        {
            if (string.IsNullOrEmpty(emailAddress))
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(emailAddress, @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("SMTP Exception: Error in email address format validation function. Address: " + emailAddress, Ex);
                return false;
            }
        }

        // Checks if the input has valid characters for an email message.
        public bool ValidCharacter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return false;
            }
            
            try
            {
                return !Regex.IsMatch(input, @"[^\w\.@-]");
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("SMTP Exception: Error processing regex match check of string: " + input, Ex);
                return false;
            }
        }

        public string CleanEmailAddress(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                return "";
            }

            try
            {
                string firstPass = Regex.Replace(address, @"[^\w\.@-]", "");
                List<string> secondPass = new List<string>();
                string thirdPass = firstPass;
                string finalPass = "";

                if (firstPass.Contains("@"))
                {
                    secondPass = firstPass.Split('@').ToList();

                    if (secondPass.Count >= 2)
                    {
                        string secondPass0 = secondPass[0];
                        string secondPass1 = secondPass[1];

                        thirdPass = secondPass0 + "@" + secondPass1;

                        secondPass.Remove(secondPass0);
                        secondPass.Remove(secondPass1);
                    }
                }

                if (secondPass.Count > 0)
                {
                    finalPass = thirdPass + string.Join("", secondPass.ToArray());
                }
                else
                {
                    finalPass = thirdPass;
                }

                return finalPass;
            }
            catch (Exception Ex)
            {
                LogUtilities.Exception("SMTP Exception: Error processing address: " + address, Ex);
                return "";
            }
        }
    }
}
