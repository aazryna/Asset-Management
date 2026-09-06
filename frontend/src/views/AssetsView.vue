<script setup>
import { ref, watch, computed, onMounted, onUnmounted } from 'vue'
import * as XLSX from 'xlsx'
import { assetService } from '../services/assetService'
import { ticketService } from '../services/ticketService'
import { userService } from '../services/userService'
import MaintenanceModal from '../components/MaintenanceModal.vue'

// State management
const assets = ref([])
const loading = ref(true)
const error = ref(null)
const searchQuery = ref('')
const statusFilter = ref('')
const assignedUserFilter = ref('')
const openMenuId = ref(null)
const menuPosition = ref({ top: 0, left: 0 })
const usersList = ref([])

const successMessage = ref('')
const errorMessage = ref('')
const showDeleteModal = ref(false)
const deletingAssetId = ref(null)
const deleteFinalNotes = ref('')

// Pagination state
const currentPage = ref(1)
const itemsPerPage = ref(50)

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

// Modal states
const showModal = ref(false)
const showEditModal = ref(false)
const showMaintenanceModal = ref(false)
const showFinalNotesModal = ref(false)
const submitting = ref(false)
const updating = ref(false)
const submittingMaintenance = ref(false)

//form for add new asset
const newAsset = ref({
    name: '',
    category: '',
    serialNumber: '',
    userId: null
})

//state for edit modal
const editingAsset = ref({
    id: null,
    name: '',
    category: '',
    serialNumber: '',
    status: 'Available',
    userId: null,
    userName: ''
})
const selectedFinalNotes = ref('')

const currentUser = ref(JSON.parse(localStorage.getItem('user')) || {})
const isAdmin = computed(() => (currentUser.value.role ?? currentUser.value.Role) === 'Admin')

const selectedAssetForTicket = ref(null)
const maintenanceForm = ref({
    subject: '',
    description: '',
    priority: 'Medium'
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
        left: Math.max(8, buttonRect.right - 144)
    }
    openMenuId.value = openMenuId.value === id ? null : id
}


// Function to fetch data using the service layer
const fetchAssets = async () => {
    try {
        loading.value = true
        assets.value = await assetService.getAssets()
    } catch (err) {
        error.value = err.message
    } finally {
        loading.value = false
    }
}

const fetchUsersList = async () => {
    try {
        const users = await userService.getUsers()
        usersList.value = users.filter(user => (user.status ?? user.Status ?? 'Active') === 'Active')
    } catch (err) {
        console.error('Failed to retrieve staff list:', err)
    }
}

// Computed properties for Dashboard Metrics 
const totalAssets = computed(() => assets.value.length)
const assignedCount = computed(() =>
    assets.value.filter(a => a.status === 'In Use').length
)
const unassignedCount = computed(() =>
    assets.value.filter(a => a.status === 'Available').length
)
const maintenanceCount = computed(() =>
    assets.value.filter(a => a.status === 'Maintenance').length
)

const myAssetsCount = computed(() => {
    const userId = currentUser.value.id
    const userName = (currentUser.value.name || currentUser.value.username || '').toLowerCase()

    return assets.value.filter(a => {
        const ownerId = a.userId ?? a.UserId ?? a.user?.id ?? a.User?.id
        const ownerName = (a.user?.name ?? a.user?.Name ?? a.user?.username ?? a.user?.Username ?? a.User?.name ?? '').toLowerCase()
        return (userId && ownerId === userId) || (userName && ownerName === userName)
    }).length
})

const myTicketsCount = computed(() => {
    return assets.value.filter(a => {
        const userId = currentUser.value.id
        const ownerId = a.userId ?? a.UserId ?? a.user?.id ?? a.User?.id
        return ownerId === userId && a.status === 'Maintenance'
    }).length
})

const assignedUserOptions = computed(() => {
    const options = new Map()
    assets.value.forEach(asset => {
        const ownerId = asset.userId ?? asset.UserId ?? asset.user?.id ?? asset.User?.id
        const ownerName = asset.user?.name ?? asset.user?.Name ?? asset.user?.username ?? asset.user?.Username ??
            asset.User?.name ?? asset.User?.Name ?? asset.User?.username ?? asset.User?.Username

        if (ownerId != null && ownerName) {
            options.set(String(ownerId), ownerName)
        }
    })
    return [...options].map(([id, name]) => ({ id, name }))
})

const filteredAssets = computed(() => {
    return assets.value.filter(asset => {
        const query = searchQuery.value.toLowerCase()

        const ownerName = (
            asset.user?.name ?? asset.user?.Name ?? asset.user?.username ?? asset.user?.Username ??
            asset.User?.name ?? asset.User?.Name ?? asset.User?.username ?? asset.User?.Username ??
            ''
        ).toLowerCase()

        const ownerId = asset.userId ?? asset.UserId ?? asset.user?.id ?? asset.User?.id
        const matchesStatus = !statusFilter.value || asset.status === statusFilter.value
        const matchesAssignedUser = assignedUserFilter.value === 'unassigned'
            ? ownerId == null
            : !assignedUserFilter.value || String(ownerId) === assignedUserFilter.value

        return (
            asset.name.toLowerCase().includes(query) ||
            asset.category.toLowerCase().includes(query) ||
            asset.serialNumber.toLowerCase().includes(query) ||
            ownerName.includes(query)
        ) && matchesStatus && matchesAssignedUser

    })
})

// CRUD Actions
const createAsset = async () => {
    submitting.value = true
    try {
        const payload = {
            name: newAsset.value.name,
            category: newAsset.value.category,
            serialNumber: newAsset.value.serialNumber,
            userId: newAsset.value.userId ? Number(newAsset.value.userId) : null
        }
        console.log("Payload send to backend:", payload)
        await assetService.createAsset(payload)

        newAsset.value = {
            name: '',
            category: '',
            serialNumber: '',
            userId: null
        }

        showModal.value = false
        await fetchAssets()
    } catch (err) {
        alert('Error: ' + err.message)
    } finally {
        submitting.value = false
    }
}

//Open Edit modal and Load Data
const openEditModal = (asset) => {
    editingAsset.value = { ...asset }
    showEditModal.value = true
}

const viewFinalNotes = (notes) => {
    selectedFinalNotes.value = notes
    showFinalNotesModal.value = true
}

//send PUT request to update asset
const updateAsset = async () => {
    updating.value = true
    try {
        await assetService.updateAsset(editingAsset.value.id, editingAsset.value)
        showEditModal.value = false
        showSuccess('Asset successfully updated!')
        await fetchAssets()
    } catch (err) {
        showError('Error: ' + err.message)
    } finally {
        updating.value = false
    }
}

// DELETE to delete asset
const openDeletePrompt = (id) => {
    deletingAssetId.value = id
    deleteFinalNotes.value = ''
    showDeleteModal.value = true
}

const deletingAsset = computed(() => assets.value.find(asset => asset.id === deletingAssetId.value))
const deletingMaintenanceAsset = computed(() => deletingAsset.value?.status === 'Maintenance')

const confirmDeleteAsset = async () => {
    if (!deletingAssetId.value) return
    try {
        await assetService.deleteAsset(deletingAssetId.value, deleteFinalNotes.value)
        showSuccess('Asset successfully deleted!')
        showDeleteModal.value = false
        deletingAssetId.value = null
        deleteFinalNotes.value = ''
        await fetchAssets()
    } catch (err) {
        showError('Error: ' + err.message)
    }
}

const openMaintenanceModal = (asset) => {
    selectedAssetForTicket.value = asset
    maintenanceForm.value = {
        subject: `Maintenance Request: ${asset.name} (${asset.serialNumber})`,
        description: '',
        priority: 'Medium'
    }
    showMaintenanceModal.value = true
}

// Send maintenance ticket data to the backend
const submitMaintenanceRequest = async (formData) => {
    submittingMaintenance.value = true
    try {
        await ticketService.createTicketWithAttachments({
            subject: formData.subject,
            description: formData.description,
            priority: formData.priority,
            assetId: selectedAssetForTicket.value.id,
            status: 'Open'
        }, formData.attachments)

        showMaintenanceModal.value = false
        showSuccess('Maintenance request submitted successfully! You can track it in the Tickets page.')

        await fetchAssets()
    } catch (err) {
        showError('Error: ' + err.message)
    } finally {
        submittingMaintenance.value = false
    }
}

// Excel Export Utility
const exportToExcel = () => {
    if (assets.value.length === 0) {
        showError('No asset data available to export.')
        return
    }
    const dataToExport = assets.value.map(asset => ({
        ID: asset.id,
        'Asset Name': asset.name,
        Category: asset.category,
        'Serial Number': asset.serialNumber,
        Status: asset.status
    }))
    const worksheet = XLSX.utils.json_to_sheet(dataToExport)
    const workbook = XLSX.utils.book_new()
    XLSX.utils.book_append_sheet(workbook, worksheet, 'Asset List')
    XLSX.writeFile(workbook, 'asset_inventory.xlsx')
}

// Excel Import Utility
const handleFileUpload = async (event) => {
    const file = event.target.files[0]
    if (!file) return

    const reader = new FileReader()
    reader.onload = async (e) => {
        try {
            const data = new Uint8Array(e.target.result)
            const workbook = XLSX.read(data, { type: 'array' })

            // Convert worksheet to JSON data
            const sheetName = workbook.SheetNames[0]
            const worksheet = workbook.Sheets[sheetName]

            // Convert worksheet to JSON data
            const jsonData = XLSX.utils.sheet_to_json(worksheet)

            if (jsonData.length === 0) {
                showError('The Excel file is empty.')
                return
            }

            loading.value = true

            // Loop through each row and send data to the backend
            for (const row of jsonData) {
                // Map keys according to the Excel file columns
                const assetPayload = {
                    name: row['Asset Name'] || row['name'] || '',
                    category: row['Category'] || row['category'] || '',
                    serialNumber: row['Serial Number'] || row['serialNumber'] || '',
                    // status: row['Status'] || row['status'] || 'Available'
                }

                if (assetPayload.name) {
                    await assetService.createAsset(assetPayload)
                }
            }

            alert('Successfully imported all assets from Excel!')
            await fetchAssets() // Refresh the asset list after import
        } catch (err) {
            showError('Error during Excel import: ' + err.message)
        } finally {
            loading.value = false
            // Reset the file input so the same file can be selected again if needed
            event.target.value = ''
        }
    }
    reader.readAsArrayBuffer(file)
}

const paginatedAssets = computed(() => {
    const start = (currentPage.value - 1) * itemsPerPage.value
    const end = start + itemsPerPage.value
    return filteredAssets.value.slice(start, end)
})

const totalPages = computed(() => {
    return Math.ceil(filteredAssets.value.length / itemsPerPage.value)
})

const paginationStart = computed(() => {
    if (filteredAssets.value.length === 0) return 0
    return (currentPage.value - 1) * itemsPerPage.value + 1
})

const paginationEnd = computed(() => {
    const end = currentPage.value * itemsPerPage.value
    return end > filteredAssets.value.length ? filteredAssets.value.length : end
})

watch(() => editingAsset.value.status, (newStatus, oldStatus) => {
    if (showEditModal.value && newStatus === 'Maintenance' && oldStatus !== 'Maintenance') {
        showEditModal.value = false

        selectedAssetForTicket.value = { ...editingAsset.value }

        showMaintenanceModal.value = true
    }
})

watch([searchQuery, statusFilter, assignedUserFilter], () => {
    currentPage.value = 1
})

onMounted(() => {
    fetchAssets()
    fetchUsersList()
    window.addEventListener('click', closeMenuOutside)
})

const closeMenuOutside = (e) => {
    if (!e.target.closest('.relative')) {
        openMenuId.value = null
    }
}
</script>

<template>
    <div
        class="min-h-screen bg-gray-100 dark:bg-gray-900 text-gray-900 dark:text-gray-100 p-8 transition-colors duration-200">
        <div class="max-w-6xl mx-auto">
            <header class="mb-8 flex justify-between items-center">
                <div>
                    <h1 class="text-3xl font-bold text-gray-800 dark:text-gray-100">Asset Management System</h1>
                    <p class="text-gray-600 dark:text-gray-400">
                        {{ isAdmin ? 'Company asset inventory list.' : 'My personal assigned assets.' }}</p>
                </div>
                <button v-if="isAdmin" @click="showModal = true; newAsset.userId = null"
                    class="bg-blue-600 hover:bg-blue-700 text-white font-semibold px-4 py-2 rounded-lg shadow transition">
                    + Add New Asset
                </button>
            </header>

            <!-- Dashboard Metrics Section -->
            <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">

                <div
                    class="bg-white dark:bg-gray-800 p-6 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 flex items-center justify-between transition-colors duration-200">
                    <div>
                        <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                            {{ isAdmin ? 'Total Assets' : 'My Assigned Assets' }}
                        </p>
                        <h3 class="text-3xl font-bold text-gray-800 dark:text-gray-100 mt-1">
                            {{ isAdmin ? totalAssets : myAssetsCount }}
                        </h3>
                    </div>
                    <div
                        class="p-3 bg-blue-50 dark:bg-blue-900/40 text-blue-600 dark:text-blue-400 rounded-full text-xl">
                        📦
                    </div>
                </div>

                <div v-if="isAdmin"
                    class="bg-white dark:bg-gray-800 p-6 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 transition-colors duration-200">
                    <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
                        Assigned vs Unassigned
                    </p>
                    <div class="flex justify-between items-center text-sm">
                        <span class="text-gray-600 dark:text-gray-300">In Use: <strong
                                class="text-gray-800 dark:text-gray-100">{{ assignedCount }}</strong></span>
                        <span class="text-gray-600 dark:text-gray-300">Available: <strong
                                class="text-gray-800 dark:text-gray-100">{{ unassignedCount }}</strong></span>
                    </div>
                    <!-- Visual progress bar -->
                    <div class="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2.5 mt-3 overflow-hidden flex">
                        <div class="bg-green-500 h-2.5 transition-all duration-300"
                            :style="{ width: totalAssets ? (assignedCount / totalAssets) * 100 + '%' : '0%' }"></div>
                        <div class="bg-blue-400 h-2.5 transition-all duration-300"
                            :style="{ width: totalAssets ? (unassignedCount / totalAssets) * 100 + '%' : '0%' }"></div>
                    </div>
                </div>

                <div
                    class="bg-white dark:bg-gray-800 p-6 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 flex items-center justify-between transition-colors duration-200">
                    <div>
                        <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                            {{ isAdmin ? 'Needs Maintenance' : 'My Active Tickets' }}
                        </p>
                        <h3 class="text-3xl font-bold text-orange-600 dark:text-orange-400 mt-1">
                            {{ isAdmin ? maintenanceCount : myTicketsCount }}
                        </h3>
                    </div>
                    <div
                        class="p-3 bg-orange-50 dark:bg-orange-900/40 text-orange-600 dark:text-orange-400 rounded-full text-xl">
                        🔧
                    </div>
                </div>
            </div>

            <!-- Search Bar & Action Buttons (Aligned) -->
            <div class="mb-6 flex flex-col lg:flex-row lg:justify-between lg:items-center gap-3">
                <!-- Search Bar on the Left -->
                <div class="w-full lg:max-w-md">
                    <input v-model="searchQuery" type="text" placeholder="Search by name, category, or serial number..."
                        class="w-full bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-700 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 shadow-sm transition-colors" />
                </div>

                <div class="flex flex-col sm:flex-row gap-3 w-full lg:w-auto">
                    <select v-model="statusFilter"
                        class="bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
                        <option value="">All Statuses</option>
                        <option value="Available">Available</option>
                        <option value="In Use">In Use</option>
                        <option value="Maintenance">Maintenance</option>
                        <option value="Decommissioned">Decommissioned</option>
                    </select>
                    <select v-model="assignedUserFilter"
                        class="bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
                        <option value="">All Assigned Users</option>
                        <option value="unassigned">Unassigned</option>
                        <option v-for="user in assignedUserOptions" :key="user.id" :value="user.id">
                            {{ user.name }}
                        </option>
                    </select>

                    <!-- Import Excel Button on the Right -->
                    <div v-if="isAdmin">
                        <button @click="exportToExcel"
                            class="bg-emerald-600 hover:bg-emerald-700 text-white font-semibold px-4 py-2 rounded-lg shadow transition inline-flex items-center gap-2">
                            📊 Export Excel
                        </button>
                    </div>
                </div>
            </div>

            <!-- Loading / Error State -->
            <div v-if="loading" class="text-blue-600 dark:text-blue-400 font-medium">Loading data...</div>
            <div v-if="error" class="text-red-500 font-medium">Error: {{ error }}</div>

            <!-- Asset Table Wrapper -->
            <div v-if="!loading && !error"
                class="bg-white dark:bg-gray-800 shadow-md rounded-lg border border-gray-200 dark:border-gray-700 overflow-hidden transition-colors duration-200">
                <div class="overflow-x-auto max-h-[650px] overflow-y-auto">
                    <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                        <thead class="bg-gray-50 dark:bg-gray-700 sticky top-0 z-10 transition-colors duration-200">
                            <tr>
                                <th
                                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    ID</th>
                                <th
                                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Asset Name</th>
                                <th
                                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Category</th>
                                <th
                                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Serial Number
                                </th>
                                <th
                                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Status</th>
                                <th
                                    class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Assigned User</th>
                                <th
                                    class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                    Actions</th>
                            </tr>
                        </thead>
                        <tbody
                            class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700 transition-colors duration-200">
                            <tr v-for="asset in paginatedAssets" :key="asset.id"
                                class="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors duration-150">
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">{{
                                    asset.id }}</td>
                                <td
                                    class="px-6 py-4 whitespace-nowrap text-sm font-semibold text-gray-800 dark:text-gray-100">
                                    {{ asset.name }}
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">{{
                                    asset.category }}</td>
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">{{
                                    asset.serialNumber }}</td>
                                <td class="px-6 py-4 whitespace-nowrap text-sm">
                                    <div class="flex flex-col items-center">
                                        <span :class="{
                                            'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300': asset.status === 'Available',
                                            'bg-blue-100 text-blue-800 dark:bg-blue-900/40 dark:text-blue-300': asset.status === 'In Use',
                                            'bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300': asset.status === 'Maintenance'
                                        }"
                                            class="px-2.5 py-0.5 inline-flex text-xs leading-5 font-semibold rounded-full">
                                            {{ asset.status }}
                                        </span>
                                        <button v-if="asset.finalNotes" @click="viewFinalNotes(asset.finalNotes)"
                                            class="mt-1 text-center text-xs text-blue-600 dark:text-blue-400 hover:underline font-medium">
                                            [View More]
                                        </button>
                                    </div>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">
                                    <span v-if="asset.user || asset.User || asset.userId || asset.UserId">
                                        {{
                                            asset.user?.name ?? asset.user?.Name ?? asset.user?.username ??
                                            asset.user?.Username
                                            ??
                                            asset.User?.name ?? asset.User?.Name ?? asset.User?.username ??
                                            asset.User?.Username
                                            ??
                                            'User #' + (asset.userId ?? asset.UserId)
                                        }}
                                    </span>
                                    <span v-else class="text-gray-400 italic">
                                        Unassigned
                                    </span>
                                </td>
                                <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium relative">
                                    <div class="flex items-center justify-end gap-2">
                                        <button @click="openMaintenanceModal(asset)" title="Report Issue to IT"
                                            class="text-orange-600 dark:text-orange-400 hover:bg-orange-50 dark:hover:bg-orange-900/30 px-3 py-1.5 rounded-md text-xs font-medium transition inline-flex items-center gap-1 bg-orange-50/50 dark:bg-orange-950/30">
                                            🔧 Report Issue
                                        </button>

                                        <div v-if="isAdmin" class="relative">
                                            <button v-if="isAdmin" @click="toggleMenu(asset.id, $event)"
                                                aria-label="Open asset actions"
                                                class="text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 bg-gray-50 dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 min-h-10 min-w-10 p-2 rounded-md transition inline-flex items-center justify-center">
                                                <span>⋮</span>
                                            </button>

                                            <div v-if="isAdmin && openMenuId === asset.id"
                                                class="fixed w-40 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-xl z-[100] py-1 text-left"
                                                :style="{ top: `${menuPosition.top}px`, left: `${menuPosition.left}px` }">
                                                <button @click.stop="openEditModal(asset); openMenuId = null"
                                                    class="w-full px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 block text-left">
                                                    ✏️ Edit Asset
                                                </button>
                                                <button v-if="asset.status !== 'Decommissioned'"
                                                    @click.stop="openDeletePrompt(asset.id); openMenuId = null"
                                                    class="w-full px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-gray-700 block text-left">
                                                    🗑️ Delete
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </td>
                            </tr>
                            <tr v-if="filteredAssets.length === 0">
                                <td colspan="7" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">
                                    No asset records found.
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>

                <!-- Pagination Bar -->
                <div v-if="filteredAssets.length > 0"
                    class="px-6 py-4 bg-gray-50 dark:bg-gray-700/50 border-t border-gray-200 dark:border-gray-700 flex items-center justify-between">
                    <span class="text-sm text-gray-600 dark:text-gray-400">
                        Showing {{ paginationStart }} to {{ paginationEnd }} of {{ filteredAssets.length }} entries
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
            <!-- Asset Addition Form Modal -->
            <div v-if="showModal"
                class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
                <div
                    class="bg-white dark:bg-gray-800 rounded-lg max-w-md w-full p-6 shadow-xl border border-gray-200 dark:border-gray-700 transition-colors">
                    <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-4">Add New Asset</h2>

                    <form @submit.prevent="createAsset" class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Asset
                                Name</label>
                            <input v-model="newAsset.name" type="text" required
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                                placeholder="e.g. MacBook Pro M3" />
                        </div>

                        <div>
                            <label
                                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Category</label>
                            <input v-model="newAsset.category" type="text" required
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                                placeholder="e.g. Laptop / Monitor" />
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Serial
                                Number</label>
                            <input v-model="newAsset.serialNumber" type="text" required
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                                placeholder="e.g. SN-12345" />
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Assign to
                                Staff (Optional)</label>
                            <select v-model="newAsset.userId"
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
                                <option :value="null">-- Unassigned (Available) --</option>
                                <option v-for="user in usersList" :key="user.id" :value="user.id">
                                    {{ user.name || user.username }} ({{ user.role }})
                                </option>
                            </select>
                        </div>


                        <div class="flex justify-end space-x-3 mt-6">
                            <button type="button" @click="showModal = false"
                                class="px-4 py-2 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition">Cancel</button>
                            <button type="submit" :disabled="submitting"
                                class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg shadow transition">
                                {{ submitting ? 'Saving...' : 'Save Asset' }}
                            </button>
                        </div>
                    </form>
                </div>
            </div>

            <!-- Edit Form -->
            <div v-if="showEditModal"
                class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
                <div
                    class="bg-white dark:bg-gray-800 rounded-lg max-w-md w-full p-6 shadow-xl border border-gray-200 dark:border-gray-700 transition-colors">
                    <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-4">Edit Asset</h2>

                    <form @submit.prevent="updateAsset" class="space-y-4">
                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Asset
                                Name</label>
                            <input v-model="editingAsset.name" type="text" required
                                :disabled="editingAsset.status === 'Decommissioned'"
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                        </div>

                        <div>
                            <label
                                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Category</label>
                            <input v-model="editingAsset.category" type="text" required
                                :disabled="editingAsset.status === 'Decommissioned'"
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Serial
                                Number</label>
                            <input v-model="editingAsset.serialNumber" type="text" required
                                :disabled="editingAsset.status === 'Decommissioned'"
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Assign to
                                Staff</label>
                            <select v-model="editingAsset.userId" :disabled="editingAsset.status === 'Decommissioned'"
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
                                <option :value="null">-- Unassigned (Available) --</option>
                                <option v-for="user in usersList" :key="user.id" :value="user.id">
                                    {{ user.name || user.username }} ({{ user.role }})
                                </option>
                            </select>
                        </div>

                        <div v-if="editingAsset.status === 'Decommissioned'">
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Final
                                Notes</label>
                            <textarea v-model="editingAsset.finalNotes" rows="4"
                                placeholder="Enter the reason for decommissioning..."
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"></textarea>
                        </div>


                        <div class="flex justify-end space-x-3 mt-6">
                            <button type="button" @click="showEditModal = false"
                                class="px-4 py-2 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition">Cancel</button>
                            <button type="submit" :disabled="updating"
                                class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg shadow transition">
                                {{ updating ? 'Updating...' : 'Update Changes' }}
                            </button>
                        </div>
                    </form>
                </div>
            </div>

            <MaintenanceModal v-model="showMaintenanceModal" :asset="selectedAssetForTicket"
                :submitting="submittingMaintenance" @submit="submitMaintenanceRequest" />

            <div v-if="showFinalNotesModal"
                class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
                <div
                    class="bg-white dark:bg-gray-800 rounded-lg max-w-md w-full p-6 shadow-xl border border-gray-200 dark:border-gray-700">
                    <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-4">Final Notes</h2>
                    <p
                        class="text-sm text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-900 p-4 rounded-lg mb-4 whitespace-pre-wrap break-words">
                        {{ selectedFinalNotes }}
                    </p>
                    <div class="flex justify-end">
                        <button @click="showFinalNotesModal = false"
                            class="px-4 py-2 text-sm text-white bg-blue-600 hover:bg-blue-700 rounded-lg shadow transition">
                            Close
                        </button>
                    </div>
                </div>
            </div>

            <!-- Modal Confirm Delete Asset -->
            <div v-if="showDeleteModal"
                class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
                <div
                    class="bg-white dark:bg-gray-800 rounded-lg max-w-md w-full p-6 shadow-xl border border-gray-200 dark:border-gray-700">
                    <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-2">Decommission Asset</h2>
                    <p class="text-sm text-gray-600 dark:text-gray-300 mb-4">
                        Are you sure you want to decommission this asset? This action cannot be undone.
                    </p>
                    <div v-if="deletingMaintenanceAsset" class="mb-6">
                        <p class="text-sm text-gray-600 dark:text-gray-300 mb-2">
                            This asset has an active maintenance ticket. Add final notes before decommissioning.
                        </p>
                        <textarea v-model="deleteFinalNotes" rows="4" required placeholder="Enter the final notes..."
                            class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-red-500 resize-none"></textarea>
                    </div>
                    <p v-else class="text-sm text-gray-600 dark:text-gray-300 mb-6">
                        The asset will be marked as Decommissioned.
                    </p>
                    <div class="flex justify-end space-x-3">
                        <button type="button"
                            @click="showDeleteModal = false; deletingAssetId = null; deleteFinalNotes = ''"
                            class="px-4 py-2 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition">
                            Cancel
                        </button>
                        <button type="button" @click="confirmDeleteAsset"
                            :disabled="deletingMaintenanceAsset && !deleteFinalNotes.trim()"
                            class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-lg shadow transition">
                            Decommission
                        </button>
                    </div>
                </div>
            </div>


        </div>
    </div>
</template>