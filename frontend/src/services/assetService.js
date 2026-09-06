const API_BASE_URL = "http://localhost:5090/api/assets";

// Helper function to retrieve the token from localStorage (adjust the name 'token' if a different name is used)
const getAuthHeaders = () => {
  const token = localStorage.getItem("token");
  return {
    "Content-Type": "application/json",
    ...(token ? { Authorization: `Bearer ${token}` } : {}),
  };
};

export const assetService = {
  async getAssets() {
    const response = await fetch(API_BASE_URL, {
      method: "GET",
      headers: getAuthHeaders(),
    });
    if (!response.ok) throw new Error("Failed to fetch data from server");
    return await response.json();
  },

  async createAsset(assetData) {
    const response = await fetch(API_BASE_URL, {
      method: "POST",
      headers: getAuthHeaders(),
      body: JSON.stringify(assetData),
    });
    if (!response.ok) throw new Error("Failed to add new asset.");
    return await response.json();
  },

  async updateAsset(id, assetData) {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: "PUT",
      headers: getAuthHeaders(),
      body: JSON.stringify(assetData),
    });
    if (!response.ok) throw new Error("Failed to update asset.");

    // Check if the response contains text/JSON content before parsing. If it's empty, just return true/null.
    const text = await response.text();
    return text ? JSON.parse(text) : true;
  },

  async deleteAsset(id, finalNotes = "") {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: "DELETE",
      headers: getAuthHeaders(),
      body: JSON.stringify({ finalNotes }),
    });
    if (!response.ok) throw new Error("Failed to delete asset.");
    return true;
  },
};
