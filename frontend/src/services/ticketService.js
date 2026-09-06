import axios from "axios";

const API_URL = "http://localhost:5090/api/tickets";

const getAuthHeaders = () => {
  const token = localStorage.getItem("token");
  return token ? { headers: { Authorization: `Bearer ${token}` } } : {};
};

export const ticketService = {
  async getTickets() {
    const response = await axios.get(API_URL, getAuthHeaders());
    return response.data;
  },

  async createTicket(ticketData) {
    const response = await axios.post(API_URL, ticketData, getAuthHeaders());
    return response.data;
  },
  async createTicketWithAttachments(ticketData, attachments = []) {
    const formData = new FormData();
    formData.append("subject", ticketData.subject);
    formData.append("description", ticketData.description);
    formData.append("priority", ticketData.priority);
    formData.append("assetId", ticketData.assetId ?? "");

    attachments.forEach((file) => formData.append("attachments", file));

    const token = localStorage.getItem("token");
    const response = await axios.post(`${API_URL}/with-attachments`, formData, {
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });
    return response.data;
  },
  async updateTicket(id, ticketData) {
    const response = await axios.put(
      `${API_URL}/${id}`,
      ticketData,
      getAuthHeaders(),
    );
    return response.data;
  },
  async deleteTicket(id) {
    const response = await axios.delete(`${API_URL}/${id}`, getAuthHeaders());
    return response.data;
  },
};
