import axios from "axios";

const API_URL = "http://localhost:5090/api/auth";

export default {
  async login(credentials) {
    try {
      const response = await axios.post(`${API_URL}/login`, credentials);
      const data = response.data;

      const token = data.token || data.Token;
      const user = data.user || data.User || data;

      if (token) {
        localStorage.setItem("token", token);
        localStorage.setItem("user", JSON.stringify(user));
      }
      return data;
    } catch (error) {
      throw (
        error.response?.data || {
          message: "Login failed. Please check your credentials.",
        }
      );
    }
  },

  logout() {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
  },

  getCurrentUser() {
    const user = localStorage.getItem("user");
    return user && user !== "undefined" ? JSON.parse(user) : null;
  },

  getToken() {
    return localStorage.getItem("token");
  },
};
