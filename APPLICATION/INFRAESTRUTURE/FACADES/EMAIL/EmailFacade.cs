﻿using APPLICATION.DOMAIN.DTOS.CONFIGURATION;
using APPLICATION.DOMAIN.DTOS.EMAIL;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;

namespace APPLICATION.INFRAESTRUTURE.FACADES.EMAIL;

/// <summary>
/// Classe responsável por envio de e-mails.
/// </summary>
public class EmailFacade
{
    private readonly IOptions<AppSettings> _appsettings;

    public EmailFacade(IOptions<AppSettings> appsettings)
    {
        _appsettings = appsettings;
    }

    /// <summary>
    /// Metodo que prepara o envio do e-mail.
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="subject"></param>
    /// <param name="userId"></param>
    /// <param name="activateCode"></param>
    /// <exception cref="Exception"></exception>
    public async Task Invite(List<string> receiver, string subject, string content)
    {
        try
        {
            var message = new Message(receiver, subject, content);

            var emailMessage = await CreateEmailBody(message);

            await Send(emailMessage);

        }
        catch (Exception exception)
        {
            throw new Exception(exception.Message);
        }
    }

    /// <summary>
    /// Método responsavel por criar o corpo do e-mail.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private Task<MimeMessage> CreateEmailBody(Message message)
    {
        try
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress("receiver", _appsettings.Value.Email.From));

            emailMessage.To.AddRange(message.Receiver);

            emailMessage.Subject = message.Subject;

            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Text)
            {
                Text = message.Content
            };

            return Task.FromResult(emailMessage);
        }
        catch (Exception exception)
        {
            throw new Exception(exception.Message);
        }
    }

    /// <summary>
    /// Método responsavel por fazer acesso ao client e enviar o e-mail.
    /// </summary>
    /// <param name="emailMessage"></param>
    /// <exception cref="Exception"></exception>
    private async Task Send(MimeMessage emailMessage)
    {
        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(_appsettings.Value.Email.SmtpServer, _appsettings.Value.Email.Port, true);

            client.AuthenticationMechanisms.Remove("XOAUTH2");

            await client.AuthenticateAsync(_appsettings.Value.Email.From, _appsettings.Value.Email.Password);

            await client.SendAsync(emailMessage);
        }
        catch (Exception exception)
        {
            throw new Exception(exception.Message);
        }
        finally
        {
            client.Disconnect(true);

            client.Dispose();
        }
    }
}
