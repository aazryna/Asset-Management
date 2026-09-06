<script setup>
import { ref, computed, onMounted } from 'vue'
import axios from 'axios'

const logs = ref([])
const loading = ref(true)
const currentPage = ref(1)
const pageSize = 50

const totalPages = computed(() => Math.ceil(logs.value.length / pageSize))
const paginatedLogs = computed(() => {
    const start = (currentPage.value - 1) * pageSize
    return logs.value.slice(start, start + pageSize)
})
const paginationStart = computed(() => logs.value.length === 0 ? 0 : (currentPage.value - 1) * pageSize + 1)
const paginationEnd = computed(() => Math.min(currentPage.value * pageSize, logs.value.length))

const fetchLogs = async () => {
    try {
        const response = await axios.get('http://localhost:5090/api/activity-logs')
        logs.value = response.data
        currentPage.value = 1
    } catch (error) {
        console.error('Failed to fetch activity logs:', error)
    } finally {
        loading.value = false
    }
}

const formatTimestamp = (timestamp) => {
    if (!timestamp) return '-'
    // Pastikan string UTC dibaca betul dengan tambah 'Z' jika tiada offset
    const dateStr = timestamp.endsWith('Z') || timestamp.includes('+') ? timestamp : timestamp + 'Z'
    const date = new Date(dateStr)

    return new Intl.DateTimeFormat('en-GB', {
        timeZone: 'Asia/Kuala_Lumpur',
        year: 'numeric',
        month: 'short',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: true
    }).format(date)
}

onMounted(() => {
    fetchLogs()
})
</script>

<template>
    <div class="min-h-screen bg-gray-100 dark:bg-gray-900 text-gray-900 dark:text-gray-100 p-8">
        <div class="max-w-6xl mx-auto">
            <div class="flex justify-between items-center mb-6">
                <div>
                    <h1 class="text-3xl font-bold text-gray-800 dark:text-gray-100">System Activity Logs</h1>
                    <p class="text-gray-600 dark:text-gray-400 mt-1">Monitor real-time system events and operation
                        trails.</p>
                </div>
                <button @click="fetchLogs"
                    class="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition shadow">
                    Refresh Logs
                </button>
            </div>

            <div
                class="bg-white dark:bg-gray-800 shadow-md rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700">
                <div v-if="loading" class="p-6 text-center text-gray-500 dark:text-gray-400">
                    Loading log records...
                </div>

                <div v-else-if="logs.length === 0" class="p-6 text-center text-gray-500 dark:text-gray-400">
                    No activity logs recorded yet.
                </div>

                <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead class="bg-gray-50 dark:bg-gray-700">
                        <tr>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Action</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Description</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Timestamp</th>
                        </tr>
                    </thead>
                    <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                        <tr v-for="log in paginatedLogs" :key="log.id"
                            class="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition">
                            <td class="px-6 py-4 whitespace-nowrap text-sm font-semibold">
                                <span :class="{
                                    'text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/50 px-2.5 py-1 rounded-full text-xs': log.action === 'CREATE',
                                    'text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/50 px-2.5 py-1 rounded-full text-xs': log.action === 'UPDATE',
                                    'text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/50 px-2.5 py-1 rounded-full text-xs': log.action === 'DELETE'
                                }">
                                    {{ log.action }}
                                </span>
                            </td>
                            <td class="px-6 py-4 text-sm text-gray-700 dark:text-gray-300">{{ log.description }}</td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                                {{ formatTimestamp(log.timestamp) }}
                            </td>
                        </tr>
                    </tbody>
                </table>

                <div v-if="logs.length > 0"
                    class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                    <span class="text-sm text-gray-600 dark:text-gray-400">
                        Showing {{ paginationStart }} to {{ paginationEnd }} of {{ logs.length }} entries
                    </span>
                    <div class="flex items-center space-x-2">
                        <button @click="currentPage--" :disabled="currentPage === 1"
                            class="px-3 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 transition">
                            Previous
                        </button>
                        <span class="text-sm font-medium text-gray-700 dark:text-gray-300">
                            Page {{ currentPage }} of {{ totalPages || 1 }}
                        </span>
                        <button @click="currentPage++" :disabled="currentPage >= totalPages"
                            class="px-3 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 transition">
                            Next
                        </button>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>