<script setup>
import { ref, computed, onMounted } from 'vue'
import { ticketService } from '../services/ticketService'
import { assetService } from '../services/assetService'

// State management
const tickets = ref([])
const assets = ref([])
const loading = ref(true)
const error = ref(null)
const searchQuery = ref('')
const statusFilter = ref('')
const openMenuId = ref(null)

// Modal state
const showModal = ref(false)
const submitting = ref(false)

// New Ticket Form
const newTicket = ref({
    subject: '',
    assetId: '',
    priority: 'Medium',
    description: '',
    status: 'Open'
})

// Toggle Dropdown Menu
const toggleMenu = (id) => {
    openMenuId.value = openMenuId.value === id ? null : id
}

// Fetch Tickets and Assets
const fetchData = async () => {
    try {
        loading.value = true
        const [ticketRes, assetRes] = await Promise.all([
            ticketService.getTickets(),
            assetService.getAssets()
        ])
        tickets.value = ticketRes
        assets.value = assetRes
    } catch (err) {
        error.value = err.message
    } finally {
        loading.value = false
    }
}

// Computed property for searching and filtering
const filteredTickets = computed(() => {
    return tickets.value.filter(ticket => {
        const query = searchQuery.value.toLowerCase()
        const subject = ticket.subject?.toLowerCase() || ''
        const description = ticket.description?.toLowerCase() || ''

        const matchesQuery = subject.includes(query) || description.includes(query)
        const matchesStatus = statusFilter.value ? ticket.status === statusFilter.value : true

        return matchesQuery && matchesStatus
    })
})

// Create Ticket Action
const createTicket = async () => {
    submitting.value = true
    try {
        await ticketService.createTicket(newTicket.value)
        showModal.value = false
        newTicket.value = { subject: '', assetId: '', priority: 'Medium', description: '', status: 'Open' }
        await fetchData()
    } catch (err) {
        alert('Error: ' + err.message)
    } finally {
        submitting.value = false
    }
}

// Delete Ticket Action
const removeTicket = async (id) => {
    if (!confirm('Are you sure you want to close/delete this ticket?')) return
    try {
        await ticketService.deleteTicket(id)
        await fetchData()
    } catch (err) {
        alert('Error: ' + err.message)
    }
}

onMounted(() => {
    fetchData()
})
</script>

<template>
    <div class="min-h-screen bg-gray-100 dark:bg-gray-900 text-gray-900 dark:text-gray-100 p-8">
        <div class="max-w-6xl mx-auto">
            <header class="mb-8 flex justify-between items-center">
                <div>
                    <h1 class="text-3xl font-bold text-gray-800 dark:text-gray-100">IT Support Tickets</h1>
                    <p class="text-gray-600 dark:text-gray-400">Track hardware issues, repair requests, and support
                        status.</p>
                </div>
                <button @click="showModal = true"
                    class="bg-blue-600 hover:bg-blue-700 text-white font-semibold px-4 py-2 rounded-lg shadow transition">
                    + Submit New Ticket
                </button>
            </header>

            <!-- Search Bar & Status Filter -->
            <div class="mb-6 flex flex-col md:flex-row justify-between items-center gap-4">
                <div class="w-full max-w-md">
                    <input v-model="searchQuery" type="text" placeholder="Search by subject or description..."
                        class="w-full bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 shadow-sm placeholder-gray-400 dark:placeholder-gray-500" />
                </div>

                <div class="w-full md:w-auto flex items-center gap-3">
                    <select v-model="statusFilter"
                        class="bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 shadow-sm">
                        <option value="">All Statuses</option>
                        <option value="Open">Open</option>
                        <option value="In Progress">In Progress</option>
                        <option value="Resolved">Resolved</option>
                    </select>
                </div>
            </div>

            <!-- Loading / Error State -->
            <div v-if="loading" class="text-blue-600 dark:text-blue-400 font-medium">Loading tickets...</div>
            <div v-if="error" class="text-red-500 font-medium">Error: {{ error }}</div>

            <!-- Tickets Table -->
            <div v-if="!loading && !error"
                class="bg-white dark:bg-gray-800 shadow-md rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden">
                <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead class="bg-gray-50 dark:bg-gray-700">
                        <tr>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                ID</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Subject & Details</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Asset ID</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Priority</th>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Status</th>
                            <th
                                class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Actions</th>
                        </tr>
                    </thead>
                    <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                        <tr v-for="ticket in filteredTickets" :key="ticket.id"
                            class="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition">
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">#{{
                                ticket.id }}</td>
                            <td class="px-6 py-4 text-sm">
                                <p class="font-semibold text-gray-800 dark:text-gray-100">{{ ticket.subject }}</p>
                                <p class="text-gray-500 dark:text-gray-400 text-xs truncate max-w-xs">{{
                                    ticket.description }}</p>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                                {{ ticket.assetId ? `Asset #${ticket.assetId}` : 'General Inquiry' }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm">
                                <span class="px-2.5 py-1 inline-flex text-xs leading-5 font-semibold rounded-full"
                                    :class="{
                                        'bg-red-100 dark:bg-red-900/50 text-red-800 dark:text-red-300': ticket.priority === 'Urgent' || ticket.priority === 'High',
                                        'bg-yellow-100 dark:bg-yellow-900/50 text-yellow-800 dark:text-yellow-300': ticket.priority === 'Medium',
                                        'bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300': ticket.priority === 'Low'
                                    }">
                                    {{ ticket.priority }}
                                </span>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm">
                                <span class="px-2.5 py-1 inline-flex text-xs leading-5 font-semibold rounded-full"
                                    :class="{
                                        'bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-300': ticket.status === 'Open',
                                        'bg-purple-100 dark:bg-purple-900/50 text-purple-800 dark:text-purple-300': ticket.status === 'In Progress',
                                        'bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300': ticket.status === 'Resolved'
                                    }">
                                    {{ ticket.status }}
                                </span>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium relative">
                                <button @click="toggleMenu(ticket.id)"
                                    class="text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 bg-gray-50 dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 p-2 rounded-md transition inline-flex items-center justify-center">
                                    <span>⋮</span>
                                </button>

                                <div v-if="openMenuId === ticket.id"
                                    class="absolute right-0 top-full mt-1 w-32 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-xl z-50 py-1 text-left">
                                    <button @click="removeTicket(ticket.id); openMenuId = null"
                                        class="w-full px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 block">Delete</button>
                                </div>
                            </td>
                        </tr>
                        <tr v-if="filteredTickets.length === 0">
                            <td colspan="6" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">No
                                support tickets found.</td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <!-- Add Ticket Modal -->
            <div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50">
                <div
                    class="bg-white dark:bg-gray-800 rounded-lg p-6 max-w-md w-full shadow-xl border border-gray-200 dark:border-gray-700">
                    <h3 class="text-lg font-bold text-gray-800 dark:text-gray-100 mb-4">Submit IT Support Ticket</h3>
                    <form @submit.prevent="createTicket" class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Subject /
                                Issue</label>
                            <input v-model="newTicket.subject" type="text" required
                                placeholder="e.g. Laptop screen flickering"
                                class="w-full bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 outline-none placeholder-gray-400 dark:placeholder-gray-500" />
                        </div>
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Related Asset
                                (Optional)</label>
                            <select v-model="newTicket.assetId"
                                class="w-full bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 outline-none">
                                <option value="">-- General Issue (No Asset) --</option>
                                <option v-for="asset in assets" :key="asset.id" :value="asset.id">
                                    {{ asset.name || asset.AssetName }} (ID: {{ asset.id }})
                                </option>
                            </select>
                        </div>
                        <div>
                            <label
                                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Priority</label>
                            <select v-model="newTicket.priority"
                                class="w-full bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 outline-none">
                                <option value="Low">Low</option>
                                <option value="Medium">Medium</option>
                                <option value="High">High</option>
                                <option value="Urgent">Urgent</option>
                            </select>
                        </div>
                        <div>
                            <label
                                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Description</label>
                            <textarea v-model="newTicket.description" rows="3" required
                                placeholder="Describe the problem in detail..."
                                class="w-full bg-white dark:bg-gray-900 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 outline-none placeholder-gray-400 dark:placeholder-gray-500"></textarea>
                        </div>
                        <div class="flex justify-end space-x-3 mt-6">
                            <button type="button" @click="showModal = false"
                                class="bg-gray-200 dark:bg-gray-700 hover:bg-gray-300 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-200 px-4 py-2 rounded-lg transition font-medium">Cancel</button>
                            <button type="submit" :disabled="submitting"
                                class="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg transition font-medium">
                                {{ submitting ? 'Submitting...' : 'Submit Ticket' }}
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    </div>
</template>