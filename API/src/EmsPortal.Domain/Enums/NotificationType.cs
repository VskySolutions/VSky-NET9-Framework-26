namespace EmsPortal.Domain.Enums;

/// <summary>
/// Categories of system notification. Drives the per-user notification preference matrix
/// (in-app / email channels) and the notification list type filter.
/// <para>
/// EVERY MEMBER HERE IS DISPATCHED SOMEWHERE. The preference matrix is built from
/// <c>Enum.GetValues&lt;NotificationType&gt;()</c>, so a declared-but-never-sent type shows the user a switch
/// over something that cannot arrive. Adding a member is therefore part of writing the code that sends it,
/// not a step before it.
/// </para>
/// <para>
/// RETIRED NUMBERS — never reuse: <c>3</c> to <c>16</c> belonged to modules that no longer exist. Values
/// are persisted as ints on Notifications and NotificationPreferences; reusing one would silently relabel
/// whatever rows still carry it.
/// </para>
/// </summary>
public enum NotificationType
{
    /// <summary>The user was @mentioned in a note.</summary>
    Mention = 1,

    /// <summary>A reminder the user set has reached its due time.</summary>
    ReminderDue = 2,
}
