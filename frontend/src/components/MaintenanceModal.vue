<script setup>
import { ref, watch } from 'vue'
import { ticketService } from '../services/ticketService'

// Receive props from the parent component
const props = defineProps({
    modelValue: {
        type: Boolean,
        required: true
    },
    asset: {
        type: Object,
        default: null
    },
    submitting: {
        type: Boolean,
        default: false
    }
})

// Send event back to the parent (to close the modal or refresh data)
const emit = defineEmits(['update:modelValue', 'submit', 'success'])

const maintenanceForm = ref({
    subject: '',
    description: '',
    priority: 'Medium'
})
const selectedFiles = ref([])
const attachmentError = ref('')

// Auto-update subject when asset changes
watch(() => props.asset, (newAsset) => {
    if (newAsset) {
        maintenanceForm.value = {
            subject: `Maintenance Request: ${newAsset.name} (${newAsset.serialNumber})`,
            description: '',
            priority: 'Medium'
        }
        selectedFiles.value = []
        attachmentError.value = ''
    }
}, { immediate: true })

const closeModal = () => {
    emit('update:modelValue', false)
}

const handleFileSelection = (event) => {
    const files = Array.from(event.target.files || [])
    const invalidFile = files.find(file =>
        !['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024
    )

    if (files.length > 3) {
        attachmentError.value = 'You can attach up to 3 images.'
        event.target.value = ''
        selectedFiles.value = []
        return
    }

    if (invalidFile) {
        attachmentError.value = 'Use JPG, PNG, or WEBP images up to 5 MB each.'
        event.target.value = ''
        selectedFiles.value = []
        return
    }

    attachmentError.value = ''
    selectedFiles.value = files
}

const handleFormSubmit = () => {
    emit('submit', { ...maintenanceForm.value, attachments: selectedFiles.value })
}

</script>

<template>
    <div v-if="modelValue" class="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50">
        <div
            class="bg-white dark:bg-gray-800 rounded-lg max-w-md w-full p-6 shadow-xl border border-gray-200 dark:border-gray-700 transition-colors">
            <h2 class="text-xl font-bold text-gray-800 dark:text-gray-100 mb-1">Request Maintenance</h2>
            <p class="text-xs text-gray-500 dark:text-gray-400 mb-4" v-if="asset">
                Reporting issue for: <span class="font-semibold text-gray-700 dark:text-gray-200">{{ asset.name
                    }}</span> ({{ asset.serialNumber }})
            </p>

            <form @submit.prevent="handleFormSubmit" class="space-y-4">
                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Issue Subject</label>
                    <input v-model="maintenanceForm.subject" type="text" required
                        class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-500" />
                </div>

                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Priority</label>
                    <select v-model="maintenanceForm.priority"
                        class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-500">
                        <option value="Low">Low</option>
                        <option value="Medium">Medium</option>
                        <option value="High">High</option>
                        <option value="Critical">Critical</option>
                    </select>
                </div>

                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Description of
                        Issue</label>
                    <textarea v-model="maintenanceForm.description" rows="3" required
                        placeholder="Explain what is wrong with the asset..."
                        class="w-full bg-gray-50 dark:bg-gray-700 text-gray-900 dark:text-gray-100 border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-orange-500"></textarea>
                </div>

                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Screenshots
                        (Optional)</label>
                    <input type="file" accept="image/jpeg,image/png,image/webp" multiple @change="handleFileSelection"
                        class="w-full text-sm text-gray-600 dark:text-gray-300 file:mr-3 file:rounded-md file:border-0 file:bg-orange-100 file:px-3 file:py-2 file:text-orange-700 hover:file:bg-orange-200" />
                    <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Up to 3 images, 5 MB each.</p>
                    <p v-if="attachmentError" class="mt-1 text-xs text-red-600 dark:text-red-400">{{ attachmentError }}
                    </p>
                </div>

                <div class="flex justify-end space-x-3 mt-6">
                    <button type="button" @click="closeModal"
                        class="px-4 py-2 text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition">Cancel</button>
                    <button type="submit" :disabled="submitting"
                        class="px-4 py-2 bg-orange-600 hover:bg-orange-700 text-white font-semibold rounded-lg shadow transition">
                        {{ submitting ? 'Submitting...' : 'Submit Request' }}
                    </button>
                </div>
            </form>
        </div>
    </div>
</template>