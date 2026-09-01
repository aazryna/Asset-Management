const API_BASE_URL = "http://localhost:5090/api/assets";

export const assetService = {
  async getAssets() {
    const response = await fetch(API_BASE_URL);
    if (!response.ok) throw new Error("Failed to fetch data from server");
    return await response.json();
  },

  async createAsset(assetData) {
    const response = await fetch(API_BASE_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(assetData),
    });
    if (!response.ok) throw new Error("Failed to add new asset.");
    return await response.json();
  },

  async updateAsset(id, assetData) {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(assetData),
    });
    if (!response.ok) throw new Error("Failed to update asset.");

    // Check if the response contains text/JSON content before parsing. If it's empty, just return true/null.
    const text = await response.text();
    return text ? JSON.parse(text) : true;
  },

  async deleteAsset(id) {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: "DELETE",
    });
    if (!response.ok) throw new Error("Failed to delete asset.");
    return true;
  },
};
