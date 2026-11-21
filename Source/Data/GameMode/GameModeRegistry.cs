using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Single place where game modes are registered. Adding a mode here and adding a
/// <see cref="GameModeCard"/> for it in <c>GameModePickScreen.tscn</c> is everything the code
/// needs; see "Game mode registry" in the README for the editor steps.
/// </summary>
public static class GameModeRegistry
{
    public const string BettingId = "betting";
    public const string SabotageId = "sabotage";
    public const string FraudId = "fraud";
    public const string UnknownDisplayNameKey = "GAME_MODE_UNKNOWN";

    private static readonly List<GameModeDefinition> registeredModes = new()
    {
        new GameModeDefinition(
            (int)GameManager.GameModeEnum.Betting,
            BettingId,
            "GAME_MODE_BETTING",
            "res://Source/Screens/Betting/BettingScreen.tscn",
            question => question.IsBettingGameQuestion && question.IsTargetedAt(BettingId),
            (question, enabled) => question.IsBettingGameQuestion = enabled),
        new GameModeDefinition(
            (int)GameManager.GameModeEnum.Sabotage,
            SabotageId,
            "GAME_MODE_SABOTAGE",
            "res://Source/Screens/Question/Sabotage/SabotageQuestionScreen.tscn",
            question => question.IsSabotageGameQuestion && question.IsTargetedAt(SabotageId),
            (question, enabled) => question.IsSabotageGameQuestion = enabled),
        new GameModeDefinition(
            (int)GameManager.GameModeEnum.Fraud,
            FraudId,
            "GAME_MODE_FRAUD",
            "res://Source/Screens/Question/Fraud/FraudQuestionScreen.tscn",
            question => question.IsFraudGameQuestion && question.IsTargetedAt(FraudId),
            (question, enabled) => question.IsFraudGameQuestion = enabled)
    };

    public static IReadOnlyList<GameModeDefinition> All => registeredModes;

    public static GameModeDefinition GetByIndex(int index) =>
        registeredModes.FirstOrDefault(mode => mode.Index == index);

    public static bool IsRegistered(GameManager.GameModeEnum modeEnum) =>
        GetByIndex((int)modeEnum) is not null;

    /// <summary>
    /// An unknown mode (no registration for the current index) accepts every question, which keeps
    /// the behaviour from before the registry existed.
    /// </summary>
    public static bool IsQuestionAllowed(Question question, GameModeDefinition mode) =>
        question is not null && (mode is null || mode.Allows(question));

    /// <summary>
    /// Rewrites the legacy per-mode flags from <see cref="Question.AllowedGameModeIds"/> so builds
    /// without the metadata still read the question correctly.
    /// </summary>
    public static void ApplyLegacyFlags(Question question)
    {
        if (question == null)
        {
            return;
        }

        foreach (var mode in registeredModes)
        {
            mode.LegacyFlagSetter?.Invoke(question, question.IsTargetedAt(mode.Id));
        }
    }
}
