<template>
    <div class="flex items-center justify-center min-h-screen bg-gray-100">
        <div class="w-full max-w-md p-8 bg-white rounded-lg shadow-md">
            <h2 class="text-2xl font-bold text-center text-gray-800 mb-6">Asset Management Login</h2>

            <div v-if="errorMessage" class="mb-4 p-3 text-sm text-red-700 bg-red-100 rounded-lg">
                {{ errorMessage }}
            </div>

            <form @submit.prevent="handleLogin">
                <div class="mb-4">
                    <label class="block text-sm font-medium text-gray-700 mb-2">Email Address</label>
                    <input type="email" v-model="email" required
                        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="name@example.com" />
                </div>

                <div class="mb-6">
                    <label class="block text-sm font-medium text-gray-700 mb-2">Password</label>
                    <div class="relative">
                        <input :type="showPassword ? 'text' : 'password'" v-model="password" required
                            class="w-full px-3 py-2 pr-10 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                            placeholder="••••••••" />
                        <button type="button" @click="showPassword = !showPassword"
                            class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-500 hover:text-gray-700 focus:outline-none">
                            <img v-if="showPassword" width="20" height="20"
                                src="https://img.icons8.com/material-outlined/24/visible--v1.png" alt="visible--v1" />
                            <img v-else width="20" height="20"
                                src="https://img.icons8.com/material-outlined/24/invisible.png" alt="invisible" />
                        </button>
                    </div>
                </div>

                <button type="submit"
                    class="w-full py-2 px-4 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-md transition duration-200 mb-4">
                    Sign In
                </button>
            </form>

            <div class="text-center mt-4">
                <p class="text-sm text-gray-600">
                    Don't have an account?
                    <router-link to="/register" class="text-blue-600 hover:underline font-medium">
                        Sign up
                    </router-link>
                </p>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import authService from '../services/authService';
import { useAuthStore } from '../stores/auth';

const email = ref('');
const password = ref('');
const showPassword = ref(false);
const errorMessage = ref('');
const router = useRouter();
const authStore = useAuthStore();

const handleLogin = async () => {
    errorMessage.value = '';
    try {
        const res = await authService.login({
            email: email.value,
            password: password.value
        });

        const userData = res.user || res.User || res;

        authStore.setUser(userData);

        router.push('/');
    } catch (error) {
        errorMessage.value = error.message || 'Invalid email or password.';
    }
};

onMounted(() => {
    document.documentElement.classList.remove('dark');
});
</script>