// NotificationType enum mirror (backend EmsPortal.Domain.Enums.NotificationType).
export const NotificationType = Object.freeze({
  Mention: 1,
  ReminderDue: 2
  // 7-13, 15 and 16 were REMS types, retired with that module; the numbers stay reserved (see NotificationType.cs).
});

const META = {
  [NotificationType.Mention]: { label: "Mention", icon: "o_alternate_email", color: "primary" },
  [NotificationType.ReminderDue]: { label: "Reminder", icon: "o_alarm", color: "orange-8" }
};

const FALLBACK = { label: "Notification", icon: "o_notifications", color: "grey-7" };

export function useNotificationMeta () {
  const metaFor = (type) => META[Number(type)] || FALLBACK;
  // The full set of types for the preferences matrix.
  const allTypes = Object.values(NotificationType).map((value) => ({ value, ...(META[value] || FALLBACK) }));
  return { metaFor, allTypes, NotificationType };
}
