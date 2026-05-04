namespace LinguaSpace.Application.Gamification.Common;

/// <summary>XP amounts awarded per game event.</summary>
public static class XpConstants
{
    public const int JoinTextRoom = 5;
    public const int JoinVoiceRoom = 10;

    public const int VoiceXpPerMinute = 1;
    public const int VoiceSessionMinXp = 5;
    public const int VoiceSessionMaxXp = 30;
}

/// <summary>Badge code constants — must match seeded Badge.Code values.</summary>
public static class BadgeCodes
{
    public const string FirstRoom = "FIRST_ROOM";
    public const string Streak3 = "STREAK_3";
    public const string Streak7 = "STREAK_7";
    public const string Streak30 = "STREAK_30";
    public const string Xp100 = "XP_100";
    public const string Xp500 = "XP_500";
    public const string Xp1000 = "XP_1000";
}
