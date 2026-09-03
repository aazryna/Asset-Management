import { createRouter, createWebHistory } from "vue-router";
import AssetsView from "../views/AssetsView.vue";
import UsersView from "../views/UsersView.vue";
import ActivityLogView from "../views/ActivityLogView.vue";
import TicketsView from "../views/TicketsView.vue";
import LoginView from "../views/LoginView.vue";
import RegisterView from "../views/RegisterView.vue";
import authService from "../services/authService";
import { useAuthStore } from "../stores/auth";
import { ROLES } from "../constants/roles";

const routes = [
  {
    path: "/login",
    name: "Login",
    component: LoginView,
  },
  {
    path: "/register",
    name: "Register",
    component: RegisterView,
  },
  {
    path: "/",
    name: "Assets",
    component: AssetsView,
    meta: { requiresAuth: true },
  },
  {
    path: "/tickets",
    name: "Tickets",
    component: TicketsView,
    meta: { requiresAuth: true },
  },
  {
    path: "/users",
    name: "Users",
    component: UsersView,
    meta: { requiresAuth: true },
  },
  {
    path: "/activity-log",
    name: "ActivityLog",
    component: ActivityLogView,
    meta: { requiresAuth: true, requiresAdmin: true },
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

// Navigation guard for non-logged in user partition
router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  const isAuthenticated = authService.getToken();

  if (to.meta.requiresAuth && !isAuthenticated) {
    return next({ name: "Login" });
  }

  if (to.meta.requiresAdmin) {
    const userRole = authStore.user?.role;
    if (userRole !== ROLES.ADMIN) {
      return next({ name: "Tickets" });
    }
  }

  if ((to.name === "Login" || to.name === "Register") && isAuthenticated) {
    return next({ name: "Assets" });
  }

  next();
});

export default router;
