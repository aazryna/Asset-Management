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
const priorityFilter = ref('')
const openMenuId = ref(null)
const menuPosition = ref({ top: 0, left: 0 })
const authStore = useAuthStore()
const showResolveModal = ref(false)
const resolvingTicketId = ref(null)
const resolutionFeedback = ref('')
const selectedResolution = ref('')
const selectedResolutionHistory = ref([])
const showResolutionModal = ref(false)
const selectedTicketDetails = ref(null)
const showTicketDetailsModal = ref(false)

const successMessage = ref('')
const errorMessage = ref('')
const showDeleteModal = ref(false)
const deletingTicketId = ref(null)

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
const isAdmin = computed(() => authStore.isAdmin)

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
const toggleMenu = (id, event) => {
    if (openMenuId.value === id) {
        openMenuId.value = null
        return
    }

    const buttonRect = event.currentTarget.getBoundingClientRect()
    menuPosition.value = {
        top: buttonRect.bottom + 4,
        left: buttonRect.right - 208
    }
    openMenuId.value = id
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
        const ticketPriority = ticket.priority?.toLowerCase()
        const matchesPriority = !priorityFilter.value ||
            (priorityFilter.value === 'critical'
                ? ticketPriority === 'critical' || ticketPriority === 'urgent'
                : ticketPriority === priorityFilter.value)

        return matchesQuery && matchesStatus && matchesPriority
    })
})

const priorityCounts = computed(() => {
    const counts = { low: 0, medium: 0, high: 0, critical: 0 }

    tickets.value.forEach(ticket => {
        const priority = ticket.priority?.toLowerCase()
        if (priority === 'low') counts.low++
        if (priority === 'medium') counts.medium++
        if (priority === 'high') counts.high++
        if (priority === 'critical' || priority === 'urgent') counts.critical++
    })

    return counts
})

const togglePriorityFilter = (priority) => {
    priorityFilter.value = priorityFilter.value === priority ? '' : priority
}

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
watch([searchQuery, statusFilter, priorityFilter], () => {
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



const viewFullResolution = (ticket) => {
    selectedResolution.value = ticket.resolution
    selectedResolutionHistory.value = ticket.resolutionHistory || []
    showResolutionModal.value = true
}

const formatResolutionDate = (date) => new Date(date).toLocaleString()

const viewTicketDetails = (ticket) => {
    selectedTicketDetails.value = ticket
    showTicketDetailsModal.value = true
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
                const assetOwner = targetAsset.user ?? targetAsset.User
                const ownerIsInactive = assetOwner && (assetOwner.status ?? assetOwner.Status) !== 'Active'

                await assetService.updateAsset(targetAsset.id, {
                    ...targetAsset,
                    userId: ownerIsInactive ? null : (targetAsset.userId ?? targetAsset.UserId ?? assetOwner?.id),
                    status: ownerIsInactive ? 'Available' : 'In Use'
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

const openDeletePrompt = (id) => {
    deletingTicketId.value = id
    showDeleteModal.value = true
}

const confirmDeleteTicket = async () => {
    if (!deletingTicketId.value) return
    try {
        await ticketService.deleteTicket(deletingTicketId.value)
        showSuccess('Ticket successfully deleted!')
        showDeleteModal.value = false
        deletingTicketId.value = null
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

            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
                <button type="button" @click="togglePriorityFilter('low')"
                    :class="priorityFilter === 'low' ? 'ring-2 ring-green-600 dark:ring-green-300 ring-offset-2 dark:ring-offset-gray-900' : ''"
                    class="bg-green-100 dark:bg-green-900/40 hover:bg-green-200 dark:hover:bg-green-900/60 p-6 rounded-lg shadow-md border border-green-200 dark:border-green-800 flex items-center justify-between text-left transition-all duration-200">
                    <div>
                        <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Low
                            Priority</p>
                        <h3 class="text-3xl font-bold text-green-600 dark:text-green-400 mt-1">{{ priorityCounts.low }}
                        </h3>
                    </div>
                    <div
                        class="p-3 bg-green-50 dark:bg-green-900/40 text-green-600 dark:text-green-400 rounded-full text-xl">
                        L</div>
                </button>

                <button type="button" @click="togglePriorityFilter('medium')"
                    :class="priorityFilter === 'medium' ? 'ring-2 ring-yellow-600 dark:ring-yellow-300 ring-offset-2 dark:ring-offset-gray-900' : ''"
                    class="bg-yellow-100 dark:bg-yellow-900/40 hover:bg-yellow-200 dark:hover:bg-yellow-900/60 p-6 rounded-lg shadow-md border border-yellow-200 dark:border-yellow-800 flex items-center justify-between text-left transition-all duration-200">
                    <div>
                        <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Medium
                            Priority</p>
                        <h3 class="text-3xl font-bold text-yellow-600 dark:text-yellow-400 mt-1">{{
                            priorityCounts.medium }}</h3>
                    </div>
                    <div
                        class="p-3 bg-yellow-50 dark:bg-yellow-900/40 text-yellow-600 dark:text-yellow-400 rounded-full text-xl">
                        M</div>
                </button>

                <button type="button" @click="togglePriorityFilter('high')"
                    :class="priorityFilter === 'high' ? 'ring-2 ring-orange-600 dark:ring-orange-300 ring-offset-2 dark:ring-offset-gray-900' : ''"
                    class="bg-orange-100 dark:bg-orange-900/40 hover:bg-orange-200 dark:hover:bg-orange-900/60 p-6 rounded-lg shadow-md border border-orange-200 dark:border-orange-800 flex items-center justify-between text-left transition-all duration-200">
                    <div>
                        <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">High
                            Priority</p>
                        <h3 class="text-3xl font-bold text-orange-600 dark:text-orange-400 mt-1">{{ priorityCounts.high
                        }}</h3>
                    </div>
                    <div
                        class="p-3 bg-orange-50 dark:bg-orange-900/40 text-orange-600 dark:text-orange-400 rounded-full text-xl">
                        H</div>
                </button>

                <button type="button" @click="togglePriorityFilter('critical')"
                    :class="priorityFilter === 'critical' ? 'ring-2 ring-red-600 dark:ring-red-300 ring-offset-2 dark:ring-offset-gray-900' : ''"
                    class="bg-red-100 dark:bg-red-900/40 hover:bg-red-200 dark:hover:bg-red-900/60 p-6 rounded-lg shadow-md border border-red-200 dark:border-red-800 flex items-center justify-between text-left transition-all duration-200">
                    <div>
                        <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                            Critical Priority</p>
                        <h3 class="text-3xl font-bold text-red-600 dark:text-red-400 mt-1">{{ priorityCounts.critical }}
                        </h3>
                    </div>
                    <div class="p-3 bg-red-50 dark:bg-red-900/40 text-red-600 dark:text-red-400 rounded-full text-xl">!
                    </div>
                </button>
            </div>

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

            <div v-if="!loading && !error"
                class="bg-white dark:bg-gray-800 shadow-md rounded-lg border border-gray-200 dark:border-gray-700 overflow-visible">

                <!-- Table Container -->
                <div class="overflow-x-auto">
                    <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 table-fixed">
                        <thead class="bg-gray-50 dark:bg-gray-800">
                            <tr>
                                <th
                                    class="w-16 px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    ID</th>
                                <th v-if="isAdmin"
                                    class="w-32 px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Asset Owned By</th>
                                <th
                                    class="w-48 px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Subject & Details</th>
                                <th
                                    class="w-32 px-2 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Asset ID</th>
                                <th
                                    class="w-20 px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Priority</th>
                                <th
                                    class="w-24 px-3 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Status</th>
                                <th
                                    class="w-32 px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Submitted By</th>
                                <th v-if="isAdmin"
                                    class="w-20 px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Actions</th>
                            </tr>
                        </thead>


                        <tbody class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700">
                            <tr v-for="ticket in paginatedTickets" :key="ticket.id"
                                class="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition">
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">#{{
                                    ticket.id }}</td>
                                <td v-if="isAdmin"
                                    class="px-4 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">
                                    {{ ticket.asset?.user?.name || ticket.user?.name || '-' }}
                                </td>

                                <td class="px-6 py-4 align-middle text-sm">
                                    <p class="font-semibold text-gray-800 dark:text-gray-100 truncate max-w-xs">
                                        {{ truncateText(ticket.subject, 40) }}
                                    </p>
                                    <p class="text-gray-500 dark:text-gray-400 text-xs truncate max-w-xs">
                                        {{ truncateText(ticket.description, 60) }}
                                    </p>
                                    <button
                                        v-if="(ticket.subject?.length || 0) > 40 || (ticket.description?.length || 0) > 60"
                                        @click="viewTicketDetails(ticket)"
                                        class="mt-1 text-xs text-blue-600 dark:text-blue-400 hover:underline font-medium">
                                        View More
                                    </button>
                                </td>
                                <td class="w-32 max-w-32 px-2 py-4 text-sm text-gray-600 dark:text-gray-300">
                                    <span class="block truncate"
                                        :title="ticket.assetId ? `Asset #${ticket.assetId}` : 'General Inquiry'">
                                        {{ ticket.assetId ? `Asset #${ticket.assetId}` : 'General Inquiry' }}
                                    </span>
                                    <button v-if="ticket.resolution" @click="viewFullResolution(ticket)"
                                        class="mt-1 block text-left text-xs text-blue-600 dark:text-blue-400 hover:underline font-medium whitespace-nowrap">
                                        [View Resolution]
                                    </button>
                                </td>
                                <td class="px-3 py-4 whitespace-nowrap text-sm">
                                    <span class="px-2.5 py-1 inline-flex text-xs leading-5 font-semibold rounded-full"
                                        :class="{
                                            'bg-red-100 dark:bg-red-900/50 text-red-800 dark:text-red-300': ticket.priority === 'Urgent' || ticket.priority === 'High',
                                            'bg-yellow-100 dark:bg-yellow-900/50 text-yellow-800 dark:text-yellow-300': ticket.priority === 'Medium',
                                            'bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300': ticket.priority === 'Low'
                                        }">
                                        {{ ticket.priority }}
                                    </span>
                                </td>
                                <td class="w-24 px-3 py-4 whitespace-nowrap text-sm">
                                    <span class="px-2.5 py-1 inline-flex text-xs leading-5 font-semibold rounded-full"
                                        :class="{
                                            'bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-300': ticket.status === 'Open',
                                            'bg-purple-100 dark:bg-purple-900/50 text-purple-800 dark:text-purple-300': ticket.status === 'In Progress',
                                            'bg-green-100 dark:bg-green-900/50 text-green-800 dark:text-green-300': ticket.status === 'Resolved',
                                            'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200': ticket.status === 'Closed',
                                            'bg-red-100 dark:bg-red-900/50 text-red-800 dark:text-red-300': ticket.status === 'Cancelled'
                                        }">
                                        {{ ticket.status }}
                                    </span>
                                </td>
                                <td class="px-4 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-400">
                                    {{ ticket.createdBy?.name || ticket.user?.name || ticket.creator?.name ||
                                        ticket.author?.name || '-' }}
                                </td>
                                <td v-if="isAdmin"
                                    class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium relative">

                                    <button v-if="isAdmin" @click="toggleMenu(ticket.id, $event)"
                                        class="text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 bg-gray-50 dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 p-2 rounded-md transition inline-flex items-center justify-center">
                                        <span>⋮</span>
                                    </button>

                                    <div v-if="isAdmin && openMenuId === ticket.id"
                                        class="fixed w-52 bg-white dark:bg-gray-800 border border-gray-100 dark:border-gray-700 rounded-xl shadow-xl z-[100] py-1.5 text-left divide-y divide-gray-100 dark:divide-gray-700"
                                        :style="{ top: `${menuPosition.top}px`, left: `${menuPosition.left}px` }">

                                        <div v-if="isAdmin" class="py-1">
                                            <button v-if="ticket.status === 'Resolved'"
                                                @click="updateTicketStatus(ticket, 'Open'); openMenuId = null"
                                                class="w-full px-4 py-2 text-xs font-medium text-gray-700 dark:text-gray-200 hover:bg-yellow-50 dark:hover:bg-gray-700 hover:text-yellow-600 dark:hover:text-yellow-400 flex items-center space-x-2.5 transition">
                                                <svg class="w-4 h-4 text-yellow-500" fill="none" stroke="currentColor"
                                                    viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round"
                                                        stroke-width="2"
                                                        d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15">
                                                    </path>
                                                </svg>
                                                <span>Reopen Ticket</span>
                                            </button>

                                            <button v-if="ticket.status === 'Closed'"
                                                @click="updateTicketStatus(ticket, 'Open'); openMenuId = null"
                                                class="w-full px-4 py-2 text-xs font-medium text-gray-700 dark:text-gray-200 hover:bg-yellow-50 dark:hover:bg-gray-700 hover:text-yellow-600 dark:hover:text-yellow-400 flex items-center space-x-2.5 transition">
                                                <svg class="w-4 h-4 text-yellow-500" fill="none" stroke="currentColor"
                                                    viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round"
                                                        stroke-width="2"
                                                        d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15">
                                                    </path>
                                                </svg>
                                                <span>Reopen Ticket</span>
                                            </button>

                                            <button v-if="ticket.status === 'Resolved'"
                                                @click="updateTicketStatus(ticket, 'Closed'); openMenuId = null"
                                                class="w-full px-4 py-2 text-xs font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-white flex items-center space-x-2.5 transition">
                                                <svg class="w-4 h-4 text-gray-500" fill="none" stroke="currentColor"
                                                    viewBox="0 0 24 24">
                                                    <path stroke-linecap="round" stroke-linejoin="round"
                                                        stroke-width="2" d="M5 5h14v14H5z"></path>
                                                </svg>
                                                <span>Close Ticket</span>
                                            </button>

                                            <template v-else-if="ticket.status !== 'Closed'">
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
                                                    <svg class="w-4 h-4 text-emerald-500" fill="none"
                                                        stroke="currentColor" viewBox="0 0 24 24">
                                                        <path stroke-linecap="round" stroke-linejoin="round"
                                                            stroke-width="2" d="M5 13l4 4L19 7"></path>
                                                    </svg>
                                                    <span>Mark as Resolved</span>
                                                </button>
                                            </template>
                                        </div>

                                    </div>
                                </td>
                            </tr>
                            <tr v-if="filteredTickets.length === 0">
                                <td :colspan="isAdmin ? 8 : 7"
                                    class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">
                                    No support tickets found.
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

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

    <!-- Modal Confirm Delete Ticket -->
    <div v-if="showDeleteModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
        <div
            class="bg-white dark:bg-gray-800 rounded-xl shadow-2xl max-w-md w-full p-6 border border-gray-100 dark:border-gray-700">
            <h3 class="text-lg font-bold text-gray-900 dark:text-gray-100 mb-2">
                Delete Ticket
            </h3>
            <p class="text-sm text-gray-600 dark:text-gray-300 mb-6">
                Are you sure you want to delete this ticket? This action cannot be undone.
            </p>
            <div class="flex justify-end gap-3">
                <button @click="showDeleteModal = false; deletingTicketId = null"
                    class="px-4 py-2 text-xs font-medium text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition">
                    Cancel
                </button>
                <button @click="confirmDeleteTicket"
                    class="px-4 py-2 text-xs font-medium text-white bg-red-600 hover:bg-red-700 rounded-lg shadow-sm transition">
                    Delete
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
            <div v-if="selectedResolutionHistory.length" class="border-t border-gray-200 dark:border-gray-700 pt-4">
                <h4 class="text-xs font-semibold uppercase text-gray-500 dark:text-gray-400 mb-3">Resolution History
                </h4>
                <div class="space-y-3 max-h-48 overflow-y-auto">
                    <div v-for="history in selectedResolutionHistory" :key="history.id"
                        class="bg-gray-50 dark:bg-gray-900 p-3 rounded-lg">
                        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">
                            {{ formatResolutionDate(history.createdAt) }}
                        </p>
                        <p class="text-sm text-gray-700 dark:text-gray-300 whitespace-pre-wrap break-words">
                            {{ history.feedback }}
                        </p>
                    </div>
                </div>
            </div>
            <div class="flex justify-end">
                <button @click="showResolutionModal = false"
                    class="px-4 py-2 text-xs font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm transition">
                    Close
                </button>
            </div>
        </div>
    </div>

    <!-- Modal View Full Ticket Details -->
    <div v-if="showTicketDetailsModal"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
        <div
            class="bg-white dark:bg-gray-800 rounded-xl shadow-2xl max-w-lg w-full p-6 border border-gray-100 dark:border-gray-700">
            <h3 class="text-lg font-bold text-gray-900 dark:text-gray-100 mb-4">
                Ticket Details
            </h3>
            <div class="space-y-3">
                <div>
                    <p class="text-xs font-semibold uppercase text-gray-500 dark:text-gray-400">Subject</p>
                    <p class="text-sm text-gray-800 dark:text-gray-200 whitespace-pre-wrap break-words">
                        {{ selectedTicketDetails?.subject }}
                    </p>
                </div>
                <div>
                    <p class="text-xs font-semibold uppercase text-gray-500 dark:text-gray-400">Details</p>
                    <p
                        class="text-sm text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-900 p-4 rounded-lg whitespace-pre-wrap break-words">
                        {{ selectedTicketDetails?.description || 'No details provided.' }}
                    </p>
                </div>
            </div>
            <div class="flex justify-end mt-4">
                <button @click="showTicketDetailsModal = false"
                    class="px-4 py-2 text-xs font-medium text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow-sm transition">
                    Close
                </button>
            </div>
        </div>
    </div>

</template>