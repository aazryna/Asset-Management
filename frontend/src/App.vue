<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router';
import { useAuthStore } from './stores/auth';
import authService from './services/authService';
import sophicLogo from './assets/sophic.png';
import { ROLES } from './constants/roles';
import axios from 'axios';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const notifications = ref([]);
const unreadCount = ref(0);

const hideNavbar = computed(() => {
  return ['/login', '/register'].includes(route.path);
});

const currentUser = computed(() => authStore.user);
const hasAdminAccess = computed(() => currentUser.value?.role === ROLES.ADMIN);

const isDropdownOpen = ref(false);

const closeDropdown = (e) => {
  if (!e.target.closest('#profile-menu')) {
    isDropdownOpen.value = false;
  }
};

const isDarkMode = ref(false);

const toggleDarkMode = () => {
  isDarkMode.value = !isDarkMode.value;
  if (isDarkMode.value) {
    document.documentElement.classList.add('dark');
    localStorage.setItem('theme', 'dark');
  } else {
    document.documentElement.classList.remove('dark');
    localStorage.setItem('theme', 'light');
  }
};

let pollInterval = null;

const fetchNotifications = async () => {
  if (!hasAdminAccess.value) return;
  try {
    const token = authStore.token || localStorage.getItem('token');
    const response = await axios.get('http://localhost:5090/api/tickets', {
      headers: { Authorization: `Bearer ${token}` }
    });

    const allTickets = response.data;
    notifications.value = [...allTickets].reverse().slice(0, 5);

    const openTickets = allTickets.filter(ticket => ticket.status === 'Open');
    unreadCount.value = openTickets.length;
  } catch (error) {
    console.error('Failed to fetch notifications', error);
  }
};

const toggleDropdown = async () => {
  isDropdownOpen.value = !isDropdownOpen.value;
  if (isDropdownOpen.value) {
    await fetchNotifications();
  }
};

onMounted(() => {
  window.addEventListener('click', closeDropdown);
  const savedTheme = localStorage.getItem('theme');
  if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
    isDarkMode.value = true;
    document.documentElement.classList.add('dark');
  } else {
    isDarkMode.value = false;
    document.documentElement.classList.remove('dark');
  }

  if (hasAdminAccess.value) {
    fetchNotifications();
    pollInterval = setInterval(fetchNotifications, 15000);
  }
});

onUnmounted(() => {
  window.removeEventListener('click', closeDropdown);
  if (pollInterval) clearInterval(pollInterval);
});

const syncThemeState = () => {
  isDarkMode.value = document.documentElement.classList.contains('dark');
};

watch(() => route.path, () => { syncThemeState(); });



const handleLogout = () => {
  authStore.logout();
  router.push('/login');
};
</script>

<template>
  <div
    class="min-h-screen bg-gray-50 dark:bg-gray-950 text-gray-900 dark:text-gray-100 flex flex-col transition-colors duration-300">

    <nav v-if="!hideNavbar"
      class="sticky top-0 bg-white/80 dark:bg-gray-900/80 backdrop-blur-md border-b border-gray-200/80 dark:border-gray-800 shadow-xs z-50 transition-all duration-300">

      <div class="max-w-7xl mx-auto px-6 h-20 flex items-center justify-between">

        <div class="flex items-center gap-3">
          <router-link to="/" class="group flex items-center gap-3 focus:outline-none">
            <div
              class="relative p-0.5 rounded-full ring-2 ring-transparent group-hover:ring-blue-500/50 transition-all duration-300">
              <img :src="sophicLogo" alt="Logo" class="w-10 h-10 rounded-full object-cover shadow-xs" />
            </div>
            <span class="font-bold tracking-tight text-gray-800 dark:text-gray-100 hidden sm:inline-block text-lg">
              Asset<span class="text-blue-600 dark:text-blue-400">Sys</span>
            </span>
          </router-link>
        </div>


        <div
          class="hidden md:flex items-center space-x-1 bg-gray-100/70 dark:bg-gray-800/60 p-1.5 rounded-full border border-gray-200/50 dark:border-gray-700/50">
          <RouterLink to="/"
            class="px-5 py-2 rounded-full text-sm font-medium text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-white transition-all duration-200"
            active-class="!bg-white dark:!bg-gray-700 !text-blue-600 dark:!text-blue-400 shadow-xs font-semibold">
            Assets
          </RouterLink>
          <RouterLink v-if="hasAdminAccess" to="/users"
            class="px-5 py-2 rounded-full text-sm font-medium text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-white transition-all duration-200"
            active-class="!bg-white dark:!bg-gray-700 !text-blue-600 dark:!text-blue-400 shadow-xs font-semibold">
            Users
          </RouterLink>
          <RouterLink to="/tickets"
            class="px-5 py-2 rounded-full text-sm font-medium text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-white transition-all duration-200"
            active-class="!bg-white dark:!bg-gray-700 !text-blue-600 dark:!text-blue-400 shadow-xs font-semibold">
            Tickets
          </RouterLink>
          <RouterLink v-if="hasAdminAccess" to="/activity-log"
            class="px-5 py-2 rounded-full text-sm font-medium text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-white transition-all duration-200"
            active-class="!bg-white dark:!bg-gray-700 !text-blue-600 dark:!text-blue-400 shadow-xs font-semibold">
            Activity Log
          </RouterLink>
        </div>

        <!-- Right Profile & Dropdown Section -->
        <div class="flex items-center">
          <div class="relative" id="profile-menu" v-if="currentUser">

            <!-- Right Profile Button -->
            <button @click="toggleDropdown"
              class="flex items-center space-x-3 focus:outline-none bg-gray-100/80 dark:bg-gray-800 hover:bg-gray-200/70 dark:hover:bg-gray-700/80 px-3.5 py-2 rounded-full border border-gray-200/80 dark:border-gray-700 transition-all duration-200 group">

              <!-- Avatar with Red Badge -->
              <div class="relative">
                <div
                  class="w-8 h-8 rounded-full bg-gradient-to-tr from-blue-600 to-indigo-600 text-white flex items-center justify-center font-bold text-xs shadow-xs">
                  {{ currentUser.name ? currentUser.name.charAt(0).toUpperCase() : 'U' }}
                </div>
                <!-- Notification Badge -->
                <span v-if="unreadCount > 0"
                  class="absolute -top-1 -right-1 bg-red-500 text-white text-[10px] font-extrabold w-4 h-4 rounded-full flex items-center justify-center shadow-xs animate-pulse">
                  {{ unreadCount }}
                </span>
              </div>

              <div class="text-left hidden md:block">
                <p
                  class="text-xs font-semibold text-gray-800 dark:text-gray-200 leading-tight group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                  {{ currentUser.name }}
                </p>
                <p class="text-[10px] text-gray-400 dark:text-gray-400 font-medium leading-tight">
                  {{ currentUser.role || 'User' }}
                </p>
              </div>

              <svg
                class="w-4 h-4 text-gray-400 group-hover:text-gray-600 dark:group-hover:text-gray-200 transition-transform duration-200"
                :class="{ 'rotate-180': isDropdownOpen }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
              </svg>
            </button>


            <transition enter-active-class="transition ease-out duration-150"
              enter-from-class="transform opacity-0 scale-95 translate-y-1"
              enter-to-class="transform opacity-100 scale-100 translate-y-0"
              leave-active-class="transition ease-in duration-100"
              leave-from-class="transform opacity-100 scale-100 translate-y-0"
              leave-to-class="transform opacity-0 scale-95 translate-y-1">
              <div v-if="isDropdownOpen"
                class="absolute right-0 mt-3 w-64 bg-white dark:bg-gray-900 border border-gray-200/80 dark:border-gray-800 rounded-2xl shadow-xl py-2 z-50 backdrop-blur-xl">

                <!-- Mobile User Info Header -->
                <div class="px-4 py-3 border-b border-gray-100 dark:border-gray-800 md:hidden">
                  <p class="text-xs font-bold text-gray-800 dark:text-gray-100">{{ currentUser.name }}</p>
                  <p class="text-[11px] text-gray-500 dark:text-gray-400">{{ currentUser.role || 'User' }}</p>
                </div>

                <!-- NOTIFICATIONS SECTION -->
                <div v-if="hasAdminAccess" class="py-1 border-b border-gray-100 dark:border-gray-800">
                  <div class="px-4 py-1.5 flex justify-between items-center">
                    <span class="text-[10px] font-bold tracking-wider text-gray-400 dark:text-gray-500 uppercase">Live
                      Tickets / Alerts</span>
                  </div>

                  <div class="max-h-52 overflow-y-auto px-1 space-y-0.5">
                    <div v-if="notifications.length === 0" class="px-3 py-3 text-xs text-gray-400 text-center">
                      No new notifications.
                    </div>
                    <router-link v-for="ticket in notifications" :key="ticket.id" to="/tickets"
                      @click="isDropdownOpen = false"
                      class="block px-3 py-2 rounded-xl hover:bg-gray-50 dark:hover:bg-gray-800/70 transition">
                      <p
                        class="text-xs font-medium text-gray-800 dark:text-gray-200 truncate flex items-center gap-1.5">
                        <span class="w-1.5 h-1.5 rounded-full bg-blue-500"></span>
                        {{ ticket.title || ticket.subject || 'New Ticket Created' }}
                      </p>
                      <p class="text-[10px] text-gray-400 pl-3">
                        By: {{ ticket.user?.name || 'Staff' }}
                      </p>
                    </router-link>
                  </div>
                </div>

                <div class="p-1 space-y-0.5">
                  <!-- Dark/Light Mode Toggle Button -->
                  <button @click="toggleDarkMode"
                    class="w-full text-left px-3.5 py-2.5 rounded-xl text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 flex items-center justify-between transition">
                    <span class="flex items-center space-x-2.5">
                      <svg v-if="isDarkMode" class="w-4 h-4 text-amber-400" fill="none" stroke="currentColor"
                        viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z">
                        </path>
                      </svg>
                      <svg v-else class="w-4 h-4 text-indigo-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                          d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z">
                        </path>
                      </svg>
                      <span>{{ isDarkMode ? 'Light Mode' : 'Dark Mode' }}</span>
                    </span>
                  </button>

                  <!-- Sign Out Button -->
                  <button @click="handleLogout"
                    class="w-full text-left px-3.5 py-2.5 rounded-xl text-xs font-medium text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-950/30 flex items-center space-x-2.5 transition">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1">
                      </path>
                    </svg>
                    <span>Sign Out</span>
                  </button>
                </div>

              </div>
            </transition>
          </div>
        </div>

      </div>
    </nav>

    <!-- Main Content View -->
    <main class="flex-grow">
      <RouterView />
    </main>

    <!-- Copyright Footer -->
    <div
      class="fixed bottom-3 left-1/2 -translate-x-1/2 z-40 bg-white/80 dark:bg-gray-900/80 backdrop-blur-md px-4 py-1 rounded-full shadow-lg border border-gray-200/80 dark:border-gray-800 text-[10px] text-gray-500 dark:text-gray-400 pointer-events-none">
      © 2026 AssetSys. All rights reserved. Developed by ANIS AZRINA.
    </div>
  </div>
</template>