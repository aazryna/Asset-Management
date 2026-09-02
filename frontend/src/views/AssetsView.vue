<script setup>
import { ref, computed, onMounted } from 'vue'
import * as XLSX from 'xlsx'
import { assetService } from '../services/assetService'
import { ticketService } from '../services/ticketService'
import MaintenanceModal from '../components/MaintenanceModal.vue'

// State management
const assets = ref([])
const loading = ref(true)
const error = ref(null)
const searchQuery = ref('')
const openMenuId = ref(null)

// Modal states
const showModal = ref(false)
const showEditModal = ref(false)
const showMaintenanceModal = ref(false)
const submitting = ref(false)
const updating = ref(false)
const submittingMaintenance = ref(false)

//form for add new asset
const newAsset = ref({
    name: '',
    category: '',
    serialNumber: '',
    status: 'Available'
})

//state for edit modal
const editingAsset = ref({
    id: null,
    name: '',
    category: '',
    serialNumber: '',
    status: 'Available'
})

const selectedAssetForTicket = ref(null)
const maintenanceForm = ref({
    subject: '',
    description: '',
    priority: 'Medium'
})

// Toggle Dropdown Menu
const toggleMenu = (id) => {
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

// Computed property for real-time searching
const filteredAssets = computed(() => {
    return assets.value.filter(asset => {
        const query = searchQuery.value.toLowerCase()
        return (
            asset.name.toLowerCase().includes(query) ||
            asset.category.toLowerCase().includes(query) ||
            asset.serialNumber.toLowerCase().includes(query)
        )
    })
})

// CRUD Actions
const createAsset = async () => {
    submitting.value = true
    try {
        await assetService.createAsset(newAsset.value)
        newAsset.value = { name: '', category: '', serialNumber: '', status: 'Available' }
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

//send PUT request to update asset
const updateAsset = async () => {
    updating.value = true
    try {
        await assetService.updateAsset(editingAsset.value.id, editingAsset.value)
        showEditModal.value = false
        await fetchAssets()
    } catch (err) {
        alert('Error: ' + err.message)
    } finally {
        updating.value = false
    }
}

// DELETE to delete asset
const removeAsset = async (id) => {
    if (!confirm('Are you sure you want to delete this asset?')) return
    try {
        await assetService.deleteAsset(id)
        await fetchAssets()
    } catch (err) {
        alert('Error: ' + err.message)
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
        await ticketService.createTicket({
            subject: formData.subject,
            description: formData.description,
            priority: formData.priority,
            assetId: selectedAssetForTicket.value.id,
            status: 'Open'
        })
        showMaintenanceModal.value = false
        alert('Maintenance request submitted successfully! You can track it in the Tickets page.')
    } catch (err) {
        alert('Error: ' + err.message)
    } finally {
        submittingMaintenance.value = false
    }
}

// Excel Export Utility
const exportToExcel = () => {
    if (assets.value.length === 0) {
        alert('No asset data available to export.')
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
                alert('The Excel file is empty.')
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
                    status: row['Status'] || row['status'] || 'Available'
                }

                if (assetPayload.name) {
                    await assetService.createAsset(assetPayload)
                }
            }

            alert('Successfully imported all assets from Excel!')
            await fetchAssets() // Refresh the asset list after import
        } catch (err) {
            alert('Ralat semasa import Excel: ' + err.message)
        } finally {
            loading.value = false
            // Reset the file input so the same file can be selected again if needed
            event.target.value = ''
        }
    }
    reader.readAsArrayBuffer(file)
}

onMounted(() => {
    fetchAssets()
})
</script>

<template>
    <div
        class="min-h-screen bg-gray-100 dark:bg-gray-900 text-gray-900 dark:text-gray-100 p-8 transition-colors duration-200">
        <div class="max-w-6xl mx-auto">
            <header class="mb-8 flex justify-between items-center">
                <div>
                    <h1 class="text-3xl font-bold text-gray-800 dark:text-gray-100">Asset Management System</h1>
                    <p class="text-gray-600 dark:text-gray-400">Company asset inventory list from database.</p>
                </div>
                <button @click="showModal = true"
                    class="bg-blue-600 hover:bg-blue-700 text-white font-semibold px-4 py-2 rounded-lg shadow transition">
                    + Add New Asset
                </button>
            </header>

            <!-- Dashboard Metrics Section -->
            <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
                <!-- Total Assets Card -->
                <div
                    class="bg-white dark:bg-gray-800 p-6 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 flex items-center justify-between transition-colors duration-200">
                    <div>
                        <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Total
                            Assets</p>
                        <h3 class="text-3xl font-bold text-gray-800 dark:text-gray-100 mt-1">{{ totalAssets }}</h3>
                    </div>
                    <div
                        class="p-3 bg-blue-50 dark:bg-blue-900/40 text-blue-600 dark:text-blue-400 rounded-full text-xl">
                        📦
                    </div>
                </div>

                <!-- Assigned vs Unassigned Card -->
                <div
                    class="bg-white dark:bg-gray-800 p-6 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 transition-colors duration-200">
                    <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">
                        Assigned vs Unassigned
                    </p>
                    <div class="flex justify-between items-center text-sm">
                        <span class="text-gray-600 dark:text-gray-300">In Use: <strong
                                class="text-gray-800 dark:text-gray-100">{{ assignedCount
                                }}</strong></span>
                        <span class="text-gray-600 dark:text-gray-300">Available: <strong
                                class="text-gray-800 dark:text-gray-100">{{ unassignedCount
                                }}</strong></span>
                    </div>
                    <!-- Visual progress bar -->
                    <div class="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2.5 mt-3 overflow-hidden flex">
                        <div class="bg-green-500 h-2.5 transition-all duration-300"
                            :style="{ width: totalAssets ? (assignedCount / totalAssets) * 100 + '%' : '0%' }"></div>
                        <div class="bg-blue-400 h-2.5 transition-all duration-300"
                            :style="{ width: totalAssets ? (unassignedCount / totalAssets) * 100 + '%' : '0%' }"></div>
                    </div>
                </div>

                <!-- Maintenance Card -->
                <div
                    class="bg-white dark:bg-gray-800 p-6 rounded-lg shadow-md border border-gray-200 dark:border-gray-700 flex items-center justify-between transition-colors duration-200">
                    <div>
                        <p class="text-sm font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Needs
                            Maintenance</p>
                        <h3 class="text-3xl font-bold text-orange-600 dark:text-orange-400 mt-1">{{ maintenanceCount }}
                        </h3>
                    </div>
                    <div
                        class="p-3 bg-orange-50 dark:bg-orange-900/40 text-orange-600 dark:text-orange-400 rounded-full text-xl">
                        🔧
                    </div>
                </div>
            </div>

            <!-- Search Bar & Action Buttons (Aligned) -->
            <div class="mb-6 flex justify-between items-center">
                <!-- Search Bar on the Left -->
                <div class="w-full max-w-md">
                    <input v-model="searchQuery" type="text" placeholder="Search by name, category, or serial number..."
                        class="w-full bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-700 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500 shadow-sm transition-colors" />
                </div>

                <!-- Import Excel Button on the Right -->
                <div>
                    <input type="file" ref="fileInput" @change="handleFileUpload" accept=".xlsx, .xls" class="hidden" />
                    <button @click="$refs.fileInput.click()"
                        class="bg-green-600 hover:bg-green-700 text-white font-semibold px-4 py-2 rounded-lg shadow transition inline-flex items-center gap-2">
                        📂 Import Excel
                    </button>
                </div>
            </div>

            <!-- Loading / Error State -->
            <div v-if="loading" class="text-blue-600 dark:text-blue-400 font-medium">Loading data...</div>
            <div v-if="error" class="text-red-500 font-medium">Error: {{ error }}</div>

            <!-- Asset Table -->
            <div v-if="!loading && !error"
                class="bg-white dark:bg-gray-800 shadow-md rounded-lg border border-gray-200 dark:border-gray-700 overflow-visible transition-colors duration-200">
                <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead class="bg-gray-50 dark:bg-gray-700 transition-colors duration-200">
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
                                class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-300 uppercase tracking-wider">
                                Actions</th>
                        </tr>
                    </thead>
                    <tbody
                        class="bg-white dark:bg-gray-800 divide-y divide-gray-200 dark:divide-gray-700 transition-colors duration-200">
                        <tr v-for="asset in filteredAssets" :key="asset.id"
                            class="hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors duration-150">
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">{{ asset.id
                                }}</td>
                            <td
                                class="px-6 py-4 whitespace-nowrap text-sm font-semibold text-gray-800 dark:text-gray-100">
                                {{ asset.name }}
                            </td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">{{
                                asset.category }}</td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-600 dark:text-gray-300">{{
                                asset.serialNumber }}</td>
                            <td class="px-6 py-4 whitespace-nowrap text-sm">
                                <span
                                    class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 dark:bg-green-900/40 text-green-800 dark:text-green-300">
                                    {{ asset.status }}
                                </span>
                            </td>
                            <td
                                class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium relative flex items-center justify-end gap-2">
                                <button @click="openMaintenanceModal(asset)" title="Request Maintenance"
                                    class="text-orange-600 dark:text-orange-400 hover:text-orange-800 bg-orange-50 dark:bg-orange-900/30 hover:bg-orange-100 px-3 py-1.5 rounded-md text-xs font-medium transition inline-flex items-center gap-1">
                                    🔧 Report Issue
                                </button>
                                <div class="relative">
                                    <button @click="toggleMenu(asset.id)"
                                        class="text-gray-500 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200 bg-gray-50 dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 p-2 rounded-md transition inline-flex items-center justify-center">
                                        <span>⋮</span>
                                    </button>

                                    <div v-if="openMenuId === asset.id"
                                        class="absolute right-0 top-full mt-1 w-32 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-xl z-50 py-1 text-left">
                                        <button @click.stop="openEditModal(asset); openMenuId = null"
                                            class="w-full px-4 py-2 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 block">Edit</button>
                                        <button @click.stop="removeAsset(asset.id); openMenuId = null"
                                            class="w-full px-4 py-2 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-gray-700 block">Delete</button>
                                    </div>
                                </div>
                            </td>
                        </tr>
                        <tr v-if="filteredAssets.length === 0">
                            <td colspan="6" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">No
                                asset records found.
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <!-- Modal Borang Tambah Aset -->
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
                            <label
                                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Status</label>
                            <select v-model="newAsset.status"
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
                                <option value="Available">Available</option>
                                <option value="In Use">In Use</option>
                                <option value="Maintenance">Maintenance</option>
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
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                        </div>

                        <div>
                            <label
                                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Category</label>
                            <input v-model="editingAsset.category" type="text" required
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                        </div>

                        <div>
                            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Serial
                                Number</label>
                            <input v-model="editingAsset.serialNumber" type="text" required
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500" />
                        </div>

                        <div>
                            <label
                                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Status</label>
                            <select v-model="editingAsset.status"
                                class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500">
                                <option value="Available">Available</option>
                                <option value="In Use">In Use</option>
                                <option value="Maintenance">Maintenance</option>
                            </select>
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


        </div>
    </div>
</template>