<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { RouterLink, RouterView, useRoute, useRouter } from 'vue-router';
import authService from './services/authService';
import sophicLogo from './assets/sophic.png';

const route = useRoute();
const router = useRouter();

const hideNavbar = computed(() => {
  return ['/login', '/register'].includes(route.path);
});

const currentUser = authService.getCurrentUser();

// State to control dropdown visibility
const isDropdownOpen = ref(false);

const toggleDropdown = () => {
  isDropdownOpen.value = !isDropdownOpen.value;
};

// Close dropdown when clicking outside
const closeDropdown = (e) => {
  if (!e.target.closest('#profile-menu')) {
    isDropdownOpen.value = false;
  }
};

// Dark Mode State & Logic
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

onMounted(() => {
  window.addEventListener('click', closeDropdown);

  // Check initial theme from localStorage
  const savedTheme = localStorage.getItem('theme');
  if (savedTheme === 'dark' || (!savedTheme && window.matchMedia('(prefers-color-scheme: dark)').matches)) {
    isDarkMode.value = true;
    document.documentElement.classList.add('dark');
  }
});

onUnmounted(() => {
  window.removeEventListener('click', closeDropdown);
});

const handleLogout = () => {
  authService.logout();
  router.push('/login');
};
</script>

<template>
  <!-- 1. UBAH SINI: Tambah 'dark:bg-gray-900 text-gray-900 dark:text-gray-100' pada kontena utama -->
  <div
    class="min-h-screen bg-gray-100 dark:bg-gray-900 text-gray-900 dark:text-gray-100 flex flex-col transition-colors duration-200">

    <!-- Navbar -->
    <!-- 2. UBAH SINI: Tambah 'dark:bg-gray-800' dan 'dark:border-gray-700' -->
    <nav v-if="!hideNavbar"
      class="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 shadow-sm relative z-50 transition-colors duration-200">
      <div class="max-w-6xl mx-auto px-8 py-4 flex justify-between items-center">
        <!-- Logo Image -->
        <div class="flex items-center">
          <router-link to="/">
            <!-- 3. UBAH SINI: Guna ':src="sophicLogo"' dan tambah 'dark:border-gray-700' -->
            <img :src="sophicLogo" alt="Logo"
              class="w-12 h-12 rounded-full object-cover border border-gray-200 dark:border-gray-700 shadow-sm" />
          </router-link>
        </div>

        <!-- Center Navigation Links -->
        <!-- 4. UBAH SINI: Tambah kelas 'dark:' untuk link navbar -->
        <div class="flex space-x-6">
          <RouterLink to="/"
            class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium transition"
            active-class="text-blue-600 dark:text-blue-400 font-bold">
            Assets
          </RouterLink>
          <RouterLink to="/users"
            class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium transition"
            active-class="text-blue-600 dark:text-blue-400 font-bold">
            User Management
          </RouterLink>
          <RouterLink to="/tickets"
            class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium transition"
            active-class="text-blue-600 dark:text-blue-400 font-bold">
            Tickets
          </RouterLink>
          <RouterLink to="/activity-log"
            class="text-gray-600 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400 font-medium transition"
            active-class="text-blue-600 dark:text-blue-400 font-bold">
            Activity Log
          </RouterLink>
        </div>

        <!-- Right Section: Profile Dropdown -->
        <div class="relative" id="profile-menu" v-if="currentUser">
          <!-- 5. UBAH SINI: Tambah gaya dark mode untuk butang profile -->
          <button @click="toggleDropdown"
            class="flex items-center space-x-3 focus:outline-none bg-gray-50 dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 px-3 py-1.5 rounded-full border border-gray-200 dark:border-gray-600 transition">
            <div class="w-8 h-8 rounded-full bg-blue-600 text-white flex items-center justify-center font-bold text-sm">
              {{ currentUser.name ? currentUser.name.charAt(0).toUpperCase() : 'U' }}
            </div>
            <div class="text-left hidden md:block">
              <!-- 6. UBAH SINI: Tambah 'dark:text-gray-100' & 'dark:text-gray-400' -->
              <p class="text-xs font-semibold text-gray-800 dark:text-gray-100 leading-tight">{{ currentUser.name }}</p>
              <p class="text-[10px] text-gray-500 dark:text-gray-400 leading-tight">{{ currentUser.role || 'User' }}</p>
            </div>
            <!-- Arrow Icon -->
            <svg class="w-4 h-4 text-gray-500 dark:text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7"></path>
            </svg>
          </button>

          <!-- Dropdown Menu Box -->
          <!-- 7. UBAH SINI: Tambah 'dark:bg-gray-800 dark:border-gray-700' -->
          <div v-if="isDropdownOpen"
            class="absolute right-0 mt-2 w-48 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg py-1 z-50">
            <div class="px-4 py-2 border-b border-gray-100 dark:border-gray-700 md:hidden">
              <p class="text-xs font-semibold text-gray-800 dark:text-gray-100">{{ currentUser.name }}</p>
              <p class="text-[10px] text-gray-500 dark:text-gray-400">{{ currentUser.role || 'User' }}</p>
            </div>

            <!-- Dark/Light Mode Toggle Button -->
            <button @click="toggleDarkMode"
              class="w-full text-left px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center justify-between transition">
              <span class="flex items-center space-x-2">
                <!-- Sun Icon for Dark Mode, Moon for Light Mode -->
                <svg v-if="isDarkMode" class="w-4 h-4 text-yellow-400" fill="none" stroke="currentColor"
                  viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z">
                  </path>
                </svg>
                <svg v-else class="w-4 h-4 text-gray-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"></path>
                </svg>
                <span>{{ isDarkMode ? 'Light Mode' : 'Dark Mode' }}</span>
              </span>
            </button>

            <!-- Sign Out Button -->
            <!-- 8. UBAH SINI: Tambah 'dark:text-red-400' & 'dark:hover:bg-gray-700' -->
            <button @click="handleLogout"
              class="w-full text-left px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-gray-700 flex items-center space-x-2 transition">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                  d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1"></path>
              </svg>
              <span>Sign Out</span>
            </button>
          </div>
        </div>
      </div>
    </nav>

    <!-- Main Content View -->
    <main class="flex-grow">
      <RouterView />
    </main>
  </div>
</template>