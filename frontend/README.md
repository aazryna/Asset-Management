# Asset Management System (Vue 3 + Tailwind CSS)

A company asset inventory management system connected directly to a backend API.

## 🛠️ What We Have Built / Updated So Far:

1. **Asset List Display (Data Fetching):**
   - Fetched asset data dynamically from the backend server using the `fetch` API (`onMounted`).
   - Implemented a clean _Loading state_ and _Error handling_ interface.

2. **Real-time Search Functionality:**
   - Added a search bar that filters the asset list dynamically by name, category, or serial number using `computed` properties.

3. **CRUD Operations (Create, Read, Update, Delete):**
   - **Add Asset:** An interactive modal form to add new assets into the database.
   - **Update (Edit) Asset:** Added an _Edit_ modal form feature along with a three-dot action button (`⋮`) to load existing data and send a `PUT` request to the backend.
   - **Delete Asset:** A function to delete asset records with a security confirmation (`confirm`) and auto-refresh the list.

4. **UI/UX Improvements (Tailwind CSS):**
   - Fixed an issue where the actions dropdown menu was clipped due to the `overflow-hidden` property on the table container.
   - Adjusted the dropdown menu positioning so that it appears smoothly right below the three-dot icon without any visual clipping.
