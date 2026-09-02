import { defineStore } from "pinia";
import { ROLES } from "../constants/roles";
import authService from "../services/authService";

export const useAuthStore = defineStore("auth", {
  state: () => ({
    user: authService.getCurrentUser(),
  }),
  getters: {
    isAdmin: (state) => (state.user?.role ?? state.user?.Role) === ROLES.ADMIN,
    isStaff: (state) => (state.user?.role ?? state.user?.Role) === ROLES.STAFF,
  },
  actions: {
    setUser(userData) {
      this.user = userData;
    },
    logout() {
      authService.logout();
      this.user = null;
    },
  },
});
