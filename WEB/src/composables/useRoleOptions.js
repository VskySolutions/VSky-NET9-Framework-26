import { ref, computed } from "vue";
import { roleApi } from "services/api";
import { useAuthStore } from "stores/auth";

// WO-123 multi-role picker.
export const SYSTEM_ROLE_NAMES = Object.freeze(["SuperAdmin", "TenantAdmin"]);

export const RoleCategory = Object.freeze({
  System: "System Roles",
  Custom: "Custom"
});

// The picker lists categories in this order (System → Custom).
const CATEGORY_ORDER = [RoleCategory.System, RoleCategory.Custom];

// The category a role name belongs to (drives the picker grouping and the display chips).
export function categoryForRoleName (name) {
  if (SYSTEM_ROLE_NAMES.includes(name)) return RoleCategory.System;
  return RoleCategory.Custom;
}

// Chip colours so each category is visually distinguishable in the role displays (AC-ADM-006.6).
export function roleCategoryChip (name) {
  const category = categoryForRoleName(name);
  if (category === RoleCategory.System) return { category, color: "blue-grey", textColor: "white" };
  return { category, color: "teal", textColor: "white" };
}

// Loads the roles assignable within a tenant and exposes them as a grouped option list for a single
// AppSelect (multiple).
export function useRoleOptions () {
  const authStore = useAuthStore();
  const roles = ref([]);
  const loading = ref(false);

  // AC-ADM-009.2: a caller who is not a Super Admin can never grant SuperAdmin, so it is excluded
  // from the options entirely (the backend also rejects it — belt and suspenders).
  const isSuperAdmin = computed(() => authStore.roles.includes("SuperAdmin"));

  const roleOptions = computed(() => {
    const assignable = roles.value.filter((r) => isSuperAdmin.value || r.name !== "SuperAdmin");
    const grouped = new Map(CATEGORY_ORDER.map((c) => [c, []]));
    for (const r of assignable) {
      grouped.get(categoryForRoleName(r.name)).push(r);
    }
    const options = [];
    for (const category of CATEGORY_ORDER) {
      const group = grouped.get(category);
      if (!group.length) continue;
      // Disabled header row → labels the category but cannot be selected.
      options.push({ label: category, value: `__cat_${category}`, disable: true, header: true });
      for (const r of [...group].sort((a, b) => a.name.localeCompare(b.name))) {
        options.push({ label: r.name, value: r.id });
      }
    }
    return options;
  });

  // Throws on failure so callers can surface the message via getApiErrorMessage (matches the
  // existing role-load error handling).
  const loadForTenant = async (tenantId) => {
    roles.value = [];
    if (!tenantId) return;
    loading.value = true;
    try {
      roles.value = (await roleApi.tenantRoles(tenantId)) || [];
    } finally {
      loading.value = false;
    }
  };

  return { roles, roleOptions, loading, isSuperAdmin, loadForTenant };
}
