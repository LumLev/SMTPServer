// See https://aka.ms/new-console-template for more information

using SMTPNET;
using SMTPNET.Models;
using System.Net.Mail;


SMTPServer SmtpServer = new();



SmtpServer.StartAcceptMail();



