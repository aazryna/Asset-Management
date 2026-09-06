<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import * as XLSX from 'xlsx'
import { userService } from '../services/userService'
import { activityLogService } from '../services/activityLogService'

// State management
const users = ref([])
const loading = ref(true)
const error = ref(null)
const deleteHistory = ref([])
const searchQuery = ref('')
const statusFilter = ref('')
const roleFilter = ref('')
const openMenuId = ref(null)

// Modal states
const showEditModal = ref(false)
const updating = ref(false)

// Pagination state
const currentPage = ref(1)
const pageSize = ref(10)
const deleteHistoryPage = ref(1)
const deleteHistoryPageSize = ref(10)

// State for edit modal
const editingUser = ref({
    id: null,
    name: '',
    email: '',
    role: 'Staff',
    status: 'Active'
})

// Toggle Dropdown Menu
const toggleMenu = (id) => {
    openMenuId.value = openMenuId.value === id ? null : id
}

// Fetch users from API
const fetchUsers = async () => {
    loading.value = true
    error.value = null
    try {
        const response = await userService.getUsers()
        users.value = response
    } catch (err) {
        error.value = err.message || 'Failed to load users'
        console.error('Fetch users error:', err)
    } finally {
        loading.value = false
    }
}

const fetchDeleteHistory = async () => {
    try {
        const logs = await activityLogService.getActivityLogs()
        deleteHistory.value = logs.filter(log =>
            log.action === 'DELETE' && log.description?.startsWith('Deleted user:')
        )
        deleteHistoryPage.value = 1
    } catch (err) {
        console.error('Fetch user delete history error:', err)
    }
}

const formatTimestamp = (timestamp) => new Date(timestamp).toLocaleString()


// Update User Action
const updateUser = async () => {
    updating.value = true
    try {
        await userService.updateUser(editingUser.value.id, editingUser.value)
        showEditModal.value = false
        await fetchUsers()
    } catch (err) {
        alert('Error: ' + (err.response?.data?.message || err.message))
    } finally {
        updating.value = false
    }
}

// Computed property for real-time searching
const filteredUsers = computed(() => {
    return users.value.filter(user => {
        const query = searchQuery.value.toLowerCase()

        const name = (user.name ?? user.Name ?? user.username ?? user.Username)?.toLowerCase() || ''
        const email = (user.email ?? user.Email)?.toLowerCase() || ''
        const role = (user.role ?? user.Role)?.toLowerCase() || ''
        const status = user.status ?? user.Status ?? 'Active'

        return (
            name.includes(query) ||
            email.includes(query) ||
            role.includes(query)
        ) &&
            (!statusFilter.value || status === statusFilter.value) &&
            (!roleFilter.value || (user.role ?? user.Role) === roleFilter.value)
    })
})

// Paginated users
const totalPages = computed(() => Math.ceil(filteredUsers.value.length / pageSize.value))

const paginatedUsers = computed(() => {
    const start = (currentPage.value - 1) * pageSize.value
    const end = start + pageSize.value
    return filteredUsers.value.slice(start, end)
})

const paginationStart = computed(() => {
    if (filteredUsers.value.length === 0) return 0
    return (currentPage.value - 1) * pageSize.value + 1
})

const paginationEnd = computed(() => {
    const end = currentPage.value * pageSize.value
    return end > filteredUsers.value.length ? filteredUsers.value.length : end
})

const deleteHistoryTotalPages = computed(() =>
    Math.ceil(deleteHistory.value.length / deleteHistoryPageSize.value)
)

const paginatedDeleteHistory = computed(() => {
    const start = (deleteHistoryPage.value - 1) * deleteHistoryPageSize.value
    return deleteHistory.value.slice(start, start + deleteHistoryPageSize.value)
})

const deleteHistoryPaginationStart = computed(() => {
    if (deleteHistory.value.length === 0) return 0
    return (deleteHistoryPage.value - 1) * deleteHistoryPageSize.value + 1
})

const deleteHistoryPaginationEnd = computed(() => Math.min(
    deleteHistoryPage.value * deleteHistoryPageSize.value,
    deleteHistory.value.length
))

// Reset to page 1 on search change
watch([searchQuery, statusFilter, roleFilter], () => {
    currentPage.value = 1
})

// Open Edit modal and Load Data
const openEditModal = (user) => {
    editingUser.value = {
        id: user.id ?? user.Id,
        name: user.name ?? user.Name ?? user.username ?? user.Username,
        email: user.email ?? user.Email,
        role: user.role ?? user.Role,
        status: user.status ?? user.Status
    }
    showEditModal.value = true
}


// Delete User Action
const removeUser = async (id) => {
    if (!confirm('Are you sure you want to delete this user?')) return
    try {
        await userService.deleteUser(id)
        await Promise.all([fetchUsers(), fetchDeleteHistory()])
    } catch (err) {
        alert('Error: ' + err.message)
    }
}

// Excel Export Utility for Users
const exportUsersToExcel = () => {
    if (users.value.length === 0) {
        alert('No user data available to export.')
        return
    }
    const dataToExport = users.value.map(user => ({
        ID: user.id ?? user.Id,
        'Full Name': user.name ?? user.Name ?? user.username ?? user.Username,
        Email: user.email ?? user.Email,
        Role: user.role ?? user.Role,
        Status: user.status ?? user.Status
    }))
    const worksheet = XLSX.utils.json_to_sheet(dataToExport)
    const workbook = XLSX.utils.book_new()
    XLSX.utils.book_append_sheet(workbook, worksheet, 'User List')
    XLSX.writeFile(workbook, 'user_management_list.xlsx')
}

const closeMenuOutside = (e) => {
    if (!e.target.closest('.relative')) {
        openMenuId.value = null
    }
}

onMounted(() => {
    fetchUsers()
    fetchDeleteHistory()
    window.addEventListener('click', closeMenuOutside)
})

onUnmounted(() => {
    window.removeEventListener('click', closeMenuOutside)
})
</script>

<template>
    <div class="min-h-screen bg-gray-100 dark:bg-gray-900 text-gray-900 dark:text-gray-100 p-8">
        <div class="max-w-6xl mx-auto">
            <header class="mb-8 flex justify-between items-center">
                <div>
                    <h1 class="text-3xl font-bold text-gray-800 dark:text-gray-100">User Management</h1>
                    <p class="text-gray-600 dark:text-gray-400">Manage system operators, assign access roles, and export
                        records.</p>
                </div>

            </header>

            <!-- Search Bar & Export Button -->
            <div class="mb-6 flex flex-col lg:flex-row lg:justify-between lg:items-center gap-3">
                <div class="w-full lg:max-w-md">
                    <input v-model="searchQuery" type="text" placeholder="Search by name, email, or role..."
                        class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 shadow-sm placeholder-gray-400 dark:placeholder-gray-500" />
                </div>

                <div class="flex flex-col sm:flex-row gap-3 w-full lg:w-auto">
                    <select v-model="statusFilter"
                        class="bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
                        <option value="">All Statuses</option>
                        <option value="Active">Active</option>
                        <option value="Inactive">Inactive</option>
                    </select>
                    <select v-model="roleFilter"
                        class="bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
                        <option value="">All Roles</option>
                        <option value="Admin">Admin</option>
                        <option value="Staff">Staff</option>
                    </select>
                    <button @click="exportUsersToExcel"
                        class="bg-emerald-600 hover:bg-emerald-700 text-white font-semibold px-4 py-2 rounded-lg shadow transition inline-flex items-center gap-2">
                        📊 Export Users
                    </button>
                </div>
            </div>

            <!-- Loading / Error State -->
            <div v-if="loading" class="text-blue-600 dark:text-blue-400 font-medium">Loading user records...</div>
            <div v-if="error" class="text-red-500 font-medium">Error: {{ error }}</div>

            <!-- User Table -->
            <div v-if="!loading && !error"
                class="bg-white dark:bg-gray-800 shadow-md rounded-lg border border-gray-200 dark:border-gray-700 overflow-visible">
                <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead class="bg-gray-50 dark:bg-gray-700">
                        <tr>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                ID</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Name</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Email</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Role</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Status</th>
                            <th
                                class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Actions</th>
                        </tr>
                    </thead>
                    <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                        <tr v-for="user in paginatedUsers" :key="user.id ?? user.Id"
                            class="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition">
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">{{ user.id
                                ??
                                user.Id }}</td>
                            <td
                                class="px-6 py-4 whitespace-nowrap text-sm font-semibold text-gray-800 dark:text-gray-100">
                                {{ user.name ?? user.Name ?? user.username ?? user.Username }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">{{
                                user.email
                                ?? user.Email }}</td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm">
                                <span class="px-2.5 py-1 inline-flex text-xs leading-5 font-semibold rounded-full"
                                    :class="(user.role ?? user.Role) === 'Admin' ? 'bg-purple-100 dark:bg-purple-900/50 text-purple-800 dark:text-purple-300' : 'bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-300'">
                                    {{ user.role ?? user.Role }}
                                </span>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm">
                                <span class="px-2.5 py-1 inline-flex text-xs leading-5 font-semibold rounded-full"
                                    :class="(user.status ?? user.Status) === 'Active' ? 'bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300' : 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-300'">
                                    {{ user.status ?? user.Status ?? 'Active' }}
                                </span>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium relative">
                                <button @click="toggleMenu(user.id)"
                                    class="text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 bg-gray-50 dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 p-2 rounded-md transition inline-flex items-center justify-center">
                                    <span>⋮</span>
                                </button>

                                <div v-if="openMenuId === user.id"
                                    class="absolute right-0 mt-2 w-36 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg z-50 py-1.5 text-left">
                                    <button @click="openEditModal(user); openMenuId = null"
                                        class="w-full px-4 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 flex items-center gap-2">
                                        ✏️ Edit
                                    </button>
                                    <button @click="removeUser(user.id); openMenuId = null"
                                        class="w-full px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 flex items-center gap-2">
                                        🗑️ Delete
                                    </button>
                                </div>
                            </td>
                        </tr>
                        <tr v-if="filteredUsers.length === 0">
                            <td colspan="6" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">No
                                user
                                records found.</td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <!-- Pagination Bar -->
            <div v-if="filteredUsers.length > 0"
                class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <span class="text-sm text-gray-600 dark:text-gray-400">
                    Showing {{ paginationStart }} to {{ paginationEnd }} of {{ filteredUsers.length }} entries
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
            <!-- User Delete Audit Trail -->
            <div v-if="filteredUsers.length > 0"
                class="mt-8 bg-white dark:bg-gray-800 shadow-md rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
                <div class="px-6 py-4 border-b border-gray-200 dark:border-gray-700">
                    <h2 class="text-lg font-bold text-gray-800 dark:text-gray-100">User Delete History</h2>
                    <p class="text-sm text-gray-500 dark:text-gray-400">Audit trail for deleted user accounts.</p>
                </div>
                <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 table-fixed">
                    <thead class="bg-gray-50 dark:bg-gray-700">
                        <tr>
                            <th
                                class="w-1/4 px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Deleted User</th>
                            <th
                                class="w-1/4 px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Timestamp</th>
                            <th
                                class="w-2/4 px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Audit Details</th>
                        </tr>
                    </thead>
                    <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                        <tr v-for="log in paginatedDeleteHistory" :key="log.id"
                            class="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition">
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-800 dark:text-gray-200 truncate">
                                {{ log.description?.replace('Deleted user: ', '').split(' (Email:')[0] }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300 truncate">
                                {{ formatTimestamp(log.timestamp) }}
                            </td>
                            <td class="px-6 py-4 text-sm text-gray-600 dark:text-gray-300 truncate">
                                {{ log.description }}
                            </td>
                        </tr>
                        <tr v-if="deleteHistory.length === 0">
                            <td colspan="3" class="px-6 py-6 text-center text-sm text-gray-500 dark:text-gray-400">
                                No deleted user records found.
                            </td>
                        </tr>
                    </tbody>
                </table>
                <div v-if="deleteHistory.length > 0"
                    class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                    <span class="text-sm text-gray-600 dark:text-gray-400">
                        Showing {{ deleteHistoryPaginationStart }} to {{ deleteHistoryPaginationEnd }} of {{
                            deleteHistory.length }} entries
                    </span>
                    <div class="flex items-center space-x-2">
                        <button @click="deleteHistoryPage--" :disabled="deleteHistoryPage === 1"
                            class="px-3 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 transition">
                            Previous
                        </button>
                        <span class="text-sm font-medium text-gray-700 dark:text-gray-300">
                            Page {{ deleteHistoryPage }} of {{ deleteHistoryTotalPages || 1 }}
                        </span>
                        <button @click="deleteHistoryPage++" :disabled="deleteHistoryPage >= deleteHistoryTotalPages"
                            class="px-3 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md disabled:opacity-50 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-300 transition">
                            Next
                        </button>
                    </div>
                </div>
            </div>

        </div>


        <!-- Edit User Modal -->
        <div v-if="showEditModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
            <div
                class="bg-white dark:bg-gray-800 rounded-lg p-6 max-w-md w-full shadow-xl border border-gray-200 dark:border-gray-700">
                <h3 class="text-lg font-bold text-gray-800 dark:text-gray-100 mb-4">Edit User</h3>
                <form @submit.prevent="updateUser" class="space-y-4">
                    <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Full
                            Name</label>
                        <input v-model="editingUser.name" type="text" required
                            class="w-full bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 outline-none" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Email
                            Address</label>
                        <input v-model="editingUser.email" type="email" required
                            class="w-full bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 outline-none" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Role</label>
                        <select v-model="editingUser.role"
                            class="w-full bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 outline-none">
                            <option value="Admin">Admin</option>
                            <option value="Staff">Staff</option>
                        </select>
                    </div>
                    <div class="flex justify-end space-x-3 mt-6">
                        <button type="button" @click="showEditModal = false"
                            class="bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-200 px-4 py-2 rounded-lg transition font-medium">Cancel</button>
                        <button type="submit" :disabled="updating"
                            class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg transition font-medium">
                            {{ updating ? 'Updating...' : 'Update Changes' }}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</template>