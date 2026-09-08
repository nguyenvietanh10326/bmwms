namespace BMWMS.Business.Services;

public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed class EmailMessage
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public IReadOnlyCollection<EmailAttachment> Attachments { get; init; } = Array.Empty<EmailAttachment>();
}
