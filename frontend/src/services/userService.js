const API_BASE_URL = "http://localhost:5090/api/users";

export const userService = {
  async getUsers() {
    const response = await fetch(API_BASE_URL);
    if (!response.ok) throw new Error("Failed to fetch users from server");
    return await response.json();
  },

  async createUser(userData) {
    const response = await fetch(API_BASE_URL, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(userData),
    });
    if (!response.ok) throw new Error("Failed to add new user.");
    return await response.json();
  },

  async updateUser(id, userData) {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(userData),
    });
    if (!response.ok) throw new Error("Failed to update user.");

    const text = await response.text();
    return text ? JSON.parse(text) : true;
  },

  async deleteUser(id) {
    const response = await fetch(`${API_BASE_URL}/${id}`, {
      method: "DELETE",
    });
    if (!response.ok) throw new Error("Failed to delete user.");
    return true;
  },
};
