namespace UniKnowledge.DTOs.Auth;

public class GoogleLoginDto
{
    /// <summary>
    /// The id_token obtained from Google Identity Services on the Frontend.
    /// </summary>
    public string IdToken { get; set; } = string.Empty;
}
