import axios from "axios";

const API_URL = "http://localhost:5090/api/tickets";

export const ticketService = {
  async getTickets() {
    const response = await axios.get(API_URL);
    return response.data;
  },
  async createTicket(ticketData) {
    const response = await axios.post(API_URL, ticketData);
    return response.data;
  },
  async deleteTicket(id) {
    const response = await axios.delete(`${API_URL}/${id}`);
    return response.data;
  },
};
