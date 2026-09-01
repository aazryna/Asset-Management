<template>
    <div class="flex items-center justify-center min-h-screen bg-gray-100">
        <div class="w-full max-w-md p-8 bg-white rounded-lg shadow-md">
            <h2 class="text-2xl font-bold text-center text-gray-800 mb-6">Create New Account</h2>

            <div v-if="errorMessage" class="mb-4 p-3 text-sm text-red-700 bg-red-100 rounded-lg">
                {{ errorMessage }}
            </div>

            <div v-if="successMessage" class="mb-4 p-3 text-sm text-green-700 bg-green-100 rounded-lg">
                {{ successMessage }}
            </div>

            <form @submit.prevent="handleRegister">
                <div class="mb-4">
                    <label class="block text-sm font-medium text-gray-700 mb-2">Full Name</label>
                    <input type="text" v-model="name" required
                        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="John Doe" />
                </div>

                <div class="mb-4">
                    <label class="block text-sm font-medium text-gray-700 mb-2">Email Address</label>
                    <input type="email" v-model="email" required
                        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="name@example.com" />
                </div>

                <div class="mb-4">
                    <label class="block text-sm font-medium text-gray-700 mb-2">Password</label>
                    <input type="password" v-model="password" required
                        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                        placeholder="••••••••" />
                </div>

                <div class="mb-6">
                    <label class="block text-sm font-medium text-gray-700 mb-2">Role</label>
                    <select v-model="role"
                        class="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500">
                        <option value="Normal User">Normal User</option>
                        <option value="Admin">Admin</option>
                    </select>
                </div>

                <button type="submit"
                    class="w-full py-2 px-4 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-md transition duration-200">
                    Sign Up
                </button>
            </form>

            <div class="mt-4 text-center">
                <router-link to="/login" class="text-sm text-blue-600 hover:underline">
                    Already have an account? Sign in
                </router-link>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import axios from 'axios';

const name = ref('');
const email = ref('');
const password = ref('');
const role = ref('Normal User');
const errorMessage = ref('');
const successMessage = ref('');
const router = useRouter();

const handleRegister = async () => {
    errorMessage.value = '';
    successMessage.value = '';
    try {
        await axios.post('http://localhost:5090/api/register', {
            name: name.value,
            email: email.value,
            password: password.value,
            role: role.value
        });

        successMessage.value = 'Registration successful! Redirecting to login...';
        setTimeout(() => {
            router.push('/login');
        }, 1500);
    } catch (error) {
        errorMessage.value = error.response?.data?.message || 'Registration failed. Please try again.';
    }
};

onMounted(() => {
    document.documentElement.classList.remove('dark');
});
</script>