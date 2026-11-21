using Godot;
using System;
using System.Security.Cryptography;
using System.Text;

[GlobalClass]
public partial class Question : Resource
{
    [Export]
    public string Id { get; set; }

    [Export(PropertyHint.MultilineText)]
    public string QuestionText { get; set; }
    [Export]
    public bool IsClosedQuestion { get; set; } = true;
    [Export]
    public Godot.Collections.Array<string> AnswersText { get; set; }
    [Export]
    public bool IsBettingGameQuestion { get; set; } = true;
    [Export]
    public bool IsSabotageGameQuestion { get; set; } = true;
    [Export]
    public bool IsFraudGameQuestion { get; set; } = true;
    [Export]
    public Category Category { get; set; }
    [Export(PropertyHint.MultilineText)]
    public string FunFact { get; set; }
    [Export]
    public Texture2D Photo { get; set; }
    [Export]
    public AudioStream Audio { get; set; }
    [Export]
    public string PhotoPath { get; set; }
    [Export]
    public string AudioPath { get; set; }

    public string EnsureStableId()
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            Id = CreateLegacyId();
        }

        return Id;
    }

    public static string CreateNewId() => Guid.NewGuid().ToString("N");

    private string CreateLegacyId()
    {
        var answers = AnswersText == null ? string.Empty : string.Join("|", AnswersText);
        var payload = $"{Category?.CategoryName}|{QuestionText}|{answers}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
