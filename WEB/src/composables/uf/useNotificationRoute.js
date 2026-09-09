// Where a notification sends its reader.
import { useEntityMeta } from "composables/uf/useEntityMeta";

export function useNotificationRoute () {
  const { routeFor } = useEntityMeta();

  // The route to open for a notification row. Async so a module can resolve a better destination
  // (a task, a sub-record) without changing its callers.
  const routeForNotification = async (n) => routeFor(n.entityType, n.entityId);

  return { routeForNotification };
}
