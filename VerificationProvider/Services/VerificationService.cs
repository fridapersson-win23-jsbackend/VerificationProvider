using Azure.Messaging.ServiceBus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VerificationProvider.Data.Contexts;
using VerificationProvider.Models;

namespace VerificationProvider.Services;

public class VerificationService(ILogger<VerificationService> logger, IServiceProvider serviceProvider) : IVerificationService
{
    private readonly ILogger<VerificationService> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;


    public VerificationRequest UnpackVerificationRequest(ServiceBusReceivedMessage message)
    {
        try
        {
            var verificationRequest = JsonConvert.DeserializeObject<VerificationRequest>(message.Body.ToString());
            if (verificationRequest != null && !string.IsNullOrEmpty(verificationRequest.Email))
            {
                return verificationRequest;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.UnpackVerificationRequest() :: {ex.Message} ");
        }
        return null!;
    }

    public string GenerateCode()
    {
        try
        {
            var rnd = new Random();
            var code = rnd.Next(100000, 999999);

            return code.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.GenerateCode() :: {ex.Message} ");
        }
        return null!;
    }

    public EmailRequest GenerateEmailRequest(VerificationRequest verificationRequest, string code)
    {
        try
        {
            if (!string.IsNullOrEmpty(verificationRequest.Email) && !string.IsNullOrEmpty(code))
            {
                var emailRequest = new EmailRequest()
                {
                    To = verificationRequest.Email,
                    Subject = $"Verification Code {code}",
                    HtmlBody = $@"
                        <html lang='eng'>
                            <head>
                                    <meta charset='utf-8' />
                                    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
                                    <title>Verification Code</title>
                            </head>
                            <body>
                                <p>We received a request to sign in to your account using email {verificationRequest.Email} PLease verify your account using the verification code</p>
                                <br><br>
                                <h3>Verification code: {code}</h3>
                            </body>
                        </html>
                    ",
                    PlainText = $"PLease verify your account using the verification code: {code}"
                };
                return emailRequest;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.GenerateEmailRequest() :: {ex.Message} ");
        }
        return null!;
    }

    public async Task<bool> SaveVerificationRequest(VerificationRequest verificationRequest, string code)
    {
        try
        {
            using var context = _serviceProvider.GetRequiredService<DataContext>();

            var existingRequest = await context.VerificationRequests.FirstOrDefaultAsync(MvcXmlMvcBuilderExtensions => MvcXmlMvcBuilderExtensions.Email == verificationRequest.Email);
            if (existingRequest != null)
            {
                existingRequest.Code = code;
                existingRequest.ExpiryDate = DateTime.Now.AddMinutes(5);
                context.Entry(existingRequest).State = EntityState.Modified;
            }
            else
            {
                context.VerificationRequests.Add(new Data.Entities.VerificationRequestEntity { Email = verificationRequest.Email, Code = code });
            }

            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.SaveVerificationRequest() :: {ex.Message} ");
        }
        return false;
    }


    public string GenerateServiceBusEmailRequest(EmailRequest emailRequest)
    {
        try
        {
            var payload = JsonConvert.SerializeObject(emailRequest); ;
            if (!string.IsNullOrEmpty(payload))
            {
                return payload;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"ERROR : GenerateVerificationCode.GenerateServiceBusEmailRequest() :: {ex.Message} ");
        }
        return null!;
    }
}
