<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { ticketService } from '../services/ticketService'
import { assetService } from '../services/assetService'
import { useAuthStore } from '../stores/auth'

// State management
const tickets = ref([])
const assets = ref([])
const loading = ref(true)
const error = ref(null)
const searchQuery = ref('')
const statusFilter = ref('')
const openMenuId = ref(null)
const authStore = useAuthStore()
const showResolveModal = ref(false)
const resolvingTicketId = ref(null)
const resolutionFeedback = ref('')
const selectedResolution = ref('')
const showResolutionModal = ref(false)

const successMessage = ref('')
const errorMessage = ref('')

// Pagination state
const currentPage = ref(1)
const pageSize = ref(10)

const showSuccess = (msg) => {
    successMessage.value = msg
    errorMessage.value = ''
    setTimeout(() => {
        if (successMessage.value === msg) successMessage.value = ''
    }, 5000)
}

const showError = (msg) => {
    errorMessage.value = msg
    successMessage.value = ''
}

const currentUser = ref(JSON.parse(localStorage.getItem('user')) || {})
const isAdmin = computed(() => currentUser.value.role === 'Admin')

// Modal state
const showModal = ref(false)
const submitting = ref(false)

// New Ticket Form
const newTicket = ref({
    subject: '',
    assetId: '',
    priority: 'Medium',
    description: '',
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

// Paginated tickets
const totalPages = computed(() => Math.ceil(filteredTickets.value.length / pageSize.value))

const paginatedTickets = computed(() => {
    const start = (currentPage.value - 1) * pageSize.value
    const end = start + pageSize.value
    return filteredTickets.value.slice(start, end)
})

const paginationStart = computed(() => {
    if (filteredTickets.value.length === 0) return 0
    return (currentPage.value - 1) * pageSize.value + 1
})

const paginationEnd = computed(() => {
    const end = currentPage.value * pageSize.value
    return end > filteredTickets.value.length ? filteredTickets.value.length : end
})

// Reset to page 1 on search or filter change
import { watch } from 'vue'
watch([searchQuery, statusFilter], () => {
    currentPage.value = 1
})

const updateTicketStatus = async (ticket, newStatus) => {
    try {
        await ticketService.updateTicket(ticket.id, {
            id: ticket.id,
            subject: ticket.subject,
            description: ticket.description,
            priority: ticket.priority,
            assetId: ticket.assetId,
            status: newStatus,
            resolution: ticket.resolution || '',
            userId: ticket.userId || ticket.createdBy?.id || ticket.creator?.id
        })
        showSuccess(`Ticket #${ticket.id} status updated to ${newStatus}!`)
        await fetchData()
    } catch (err) {
        showError('Error updating status: ' + err.message)
    }
}

// Create Ticket Action
const createTicket = async () => {
    submitting.value = true
    try {
        await ticketService.createTicket(newTicket.value)
        showModal.value = false
        newTicket.value = { subject: '', assetId: '', priority: 'Medium', description: '' }
        showSuccess('Ticket successfully created!')
        await fetchData()
    } catch (err) {
        showError('Error: ' + err.message)
    } finally {
        submitting.value = false
    }
}

const truncateText = (text, maxLength = 50) => {
    if (!text) return ''
    return text.length > maxLength ? text.substring(0, maxLength) + '...' : text
}



const viewFullResolution = (resolutionText) => {
    selectedResolution.value = resolutionText
    showResolutionModal.value = true
}

const openResolvePrompt = (ticket) => {
    resolvingTicketId.value = ticket.id
    resolutionFeedback.value = ''
    showResolveModal.value = true
}

const submitResolvedWithFeedback = async () => {
    console.log("Button clicked, resolvingTicketId:", resolvingTicketId.value)

    const ticketToResolve = tickets.value.find(t => t.id === resolvingTicketId.value)
    if (!ticketToResolve) return

    try {
        await ticketService.updateTicket(resolvingTicketId.value, {
            ...ticketToResolve,
            status: 'Resolved',
            resolution: resolutionFeedback.value,
            userId: ticketToResolve.userId || ticketToResolve.createdBy?.id
        })

        if (ticketToResolve.assetId) {
            const targetAsset = assets.value.find(a => a.id === ticketToResolve.assetId)
            if (targetAsset) {
                await assetService.updateAsset(targetAsset.id, {
                    ...targetAsset,
                    status: 'In Use'
                })
            }
        }

        showSuccess(`Ticket #${resolvingTicketId.value} successfully resolved!`)
        showResolveModal.value = false
        await fetchData()
    } catch (err) {
        showError('Error updating status: ' + err.message)
    }
}

// Delete Ticket Action
const removeTicket = async (id) => {
    if (!confirm('Are you sure you want to close/delete this ticket?')) return
    try {
        await ticketService.deleteTicket(id)
        showSuccess('Ticket successfully deleted!')
        await fetchData()
    } catch (err) {
        showError('Error: ' + err.message)
    }
}

const closeMenuOutside = (e) => {
    if (!e.target.closest('td')) {
        openMenuId.value = null;
    }
}

onMounted(() => {
    fetchData()
    window.addEventListener('click', closeMenuOutside);
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
                    <h1 class="text-3xl font-bold text-gray-800 dark:text-gray-100">IT Support Tickets</h1>
                    <p class="text-gray-600 dark:text-gray-400">Track hardware issues, repair requests, and support
                        status.</p>
                </div>

            </header>

            <div v-if="successMessage"
                class="mb-6 bg-green-50 dark:bg-green-900/30 border border-green-200 dark:border-green-800 text-green-800 dark:text-green-300 px-4 py-3 rounded-lg shadow-sm flex items-center justify-between transition-all">
                <div class="flex items-center gap-2">
                    <span class="text-green-600 dark:text-green-400 font-bold">✓</span>
                    <span class="text-sm font-medium">{{ successMessage }}</span>
                </div>
                <button @click="successMessage = ''"
                    class="text-green-600 dark:text-green-400 hover:text-green-800 font-bold text-sm">✕</button>
            </div>

            <div v-if="errorMessage"
                class="mb-6 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-800 dark:text-red-300 px-4 py-3 rounded-lg shadow-sm flex items-center justify-between transition-all">
                <div class="flex items-center gap-2">
                    <span class="text-red-600 dark:text-red-400 font-bold">⚠️</span>
                    <span class="text-sm font-medium">{{ errorMessage }}</span>
                </div>
                <button @click="errorMessage = ''"
                    class="text-red-600 dark:text-red-400 hover:text-red-800 font-bold text-sm">✕</button>
            </div>

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
                class="bg-white dark:bg-gray-800 shadow-md rounded-lg border border-gray-200 dark:border-gray-700 overflow-visible">
                <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead class="bg-gray-50 dark:bg-gray-700">
                        <tr>
                            <th
                                class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                ID</th>
                            <th v-if="isAdmin" class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                Asset Owned By</th>

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
                            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                                Submitted By</th>
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
                            <td v-if="isAdmin"
                                class="px-4 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
                                {{ ticket.asset?.user?.name || ticket.user?.name || '-' }}
                            </td>

                            <td class="px-6 py-4 text-sm">
                                <p class="font-semibold text-gray-800 dark:text-gray-100">{{ ticket.subject }}</p>
                                <p class="text-gray-500 dark:text-gray-400 text-xs truncate max-w-xs">{{
                                    ticket.description }}</p>
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                                {{ ticket.assetId ? `Asset #${ticket.assetId}` : 'General Inquiry' }}
                                <div v-if="ticket.resolution" class="mt-1">
                                    <p
                                        class="text-emerald-600 dark:text-emerald-400 text-xs font-medium truncate max-w-xs inline-block align-bottom">
                                        Resolution: {{ ticket.resolution }}
                                    </p>
                                    <button @click="viewFullResolution(ticket.resolution)"
                                        class="text-xs text-blue-500 hover:underline ml-1 font-medium">
                                        [View]
                                    </button>
                                </div>
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
                            <td class="px-4 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-400">
                                {{ ticket.createdBy?.name || ticket.user?.name || ticket.creator?.name ||
                                    ticket.author?.name || '-' }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium relative">
                                <button @click="toggleMenu(ticket.id)"
                                    class="text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 bg-gray-50 dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 p-2 rounded-md transition inline-flex items-center justify-center">
                                    <span>⋮</span>
                                </button>

                                <div v-if="openMenuId === ticket.id"
                                    class="absolute right-0 bottom-full mb-1 w-52 bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl shadow-xl z-50 py-1.5 text-left divide-y divide-gray-100 dark:divide-gray-700">

                                    <div v-if="isAdmin" class="py-1">
                                        <!-- UBAH DI SINI: Papar butang 'Reopen Ticket' jika status dah Resolved -->
                                        <button v-if="ticket.status === 'Resolved'"
                                            @click="updateTicketStatus(ticket, 'Open'); openMenuId = null"
                                            class="w-full px-4 py-2 text-xs font-medium text-gray-700 dark:text-gray-200 hover:bg-yellow-50 dark:hover:bg-gray-700 hover:text-yellow-600 dark:hover:text-yellow-400 flex items-center space-x-2.5 transition">
                                            <svg class="w-4 h-4 text-yellow-500" fill="none" stroke="currentColor"
                                                viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                                    d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15">
                                                </path>
                                            </svg>
                                            <span>Reopen Ticket</span>
                                        </button>

                                        <template v-else>
                                            <button v-if="ticket.status !== 'In Progress'"
                                                @click="updateTicketStatus(ticket, 'In Progress'); openMenuId = null"
                                                class="w-full px-4 py-2 text-xs font-medium text-gray-700 dark:text-gray-200 hover:bg-blue-50 dark:hover:bg-gray-700 hover:text-blue-600 dark:hover:text-blue-400 flex items-center space-x-2.5 transition">
                                                <svg class="w-4 h-4 text-blue-500" fill="none" stroke="currentColor"
                                                    viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round"
                                                        stroke-width="2"
                                                        d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                                                </svg>
                                                <span>Mark as In Progress</span>
                                            </button>

                                            <button @click="openResolvePrompt(ticket); openMenuId = null"
                                                class="w-full px-4 py-2 text-xs font-medium text-gray-700 dark:text-gray-200 hover:bg-emerald-50 dark:hover:bg-gray-700 hover:text-emerald-600 dark:hover:text-emerald-400 flex items-center space-x-2.5 transition">
                                                <svg class="w-4 h-4 text-emerald-500" fill="none" stroke="currentColor"
                                                    viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round"
                                                        stroke-width="2" d="M5 13l4 4L19 7"></path>
                                                </svg>
                                                <span>Mark as Resolved</span>
                                            </button>
                                        </template>
                                    </div>

                                    <div class="py-1">
                                        <button @click="removeTicket(ticket.id); openMenuId = null"
                                            class="w-full px-4 py-2 text-xs font-medium text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-gray-700 flex items-center space-x-2.5 transition">
                                            <svg class="w-4 h-4 text-red-500" fill="none" stroke="currentColor"
                                                viewBox="0 0 24 24">
                                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                                                    d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16">
                                                </path>
                                            </svg>
                                            <span>Delete Ticket</span>
                                        </button>
                                    </div>
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

            <!-- Pagination Bar -->
            <div v-if="filteredTickets.length > 0"
                class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                <span class="text-sm text-gray-600 dark:text-gray-400">
                    Showing {{ paginationStart }} to {{ paginationEnd }} of {{ filteredTickets.length }} entries
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

    <!-- Modal Resolution Feedback -->
    <div v-if="showResolveModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
        <div
            class="bg-white dark:bg-gray-800 rounded-xl shadow-2xl max-w-md w-full p-6 border border-gray-100 dark:border-gray-700">
            <h3 class="text-lg font-bold text-gray-900 dark:text-gray-100 mb-2">
                Resolution Feedback</h3>
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-4">Please provide a
                summary of the issue or repair actions taken for future reference.</p>

            <textarea v-model="resolutionFeedback" rows="4"
                placeholder="E.g., Replaced faulty SSD / Reinstalled Windows drivers..."
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-700 text-gray-900 dark:text-gray-100 rounded-lg p-3 text-sm focus:outline-none focus:ring-2 focus:ring-emerald-500 mb-4 resize-none"></textarea>

            <div class="flex justify-end space-x-3">
                <button @click="showResolveModal = false"
                    class="px-4 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition">
                    Cancel
                </button>
                <button @click="submitResolvedWithFeedback"
                    class="px-4 py-2 text-xs font-medium text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg shadow-sm transition">
                    Submit & Resolve
                </button>
            </div>
        </div>
    </div>

    <!-- Modal View Full Resolution -->
    <div v-if="showResolutionModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
        <div
            class="bg-white dark:bg-gray-800 rounded-xl shadow-2xl max-w-md w-full p-6 border border-gray-100 dark:border-gray-700">
            <h3 class="text-lg font-bold text-gray-900 dark:text-gray-100 mb-2">
                Resolution Details
            </h3>
            <p
                class="text-sm text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-900 p-4 rounded-lg mb-4 whitespace-pre-wrap">
                {{ selectedResolution }}
            </p>
            <div class="flex justify-end">
                <button @click="showResolutionModal = false"
                    class="px-4 py-2 text-xs font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm transition">
                    Close
                </button>
            </div>
        </div>
    </div>

</template>