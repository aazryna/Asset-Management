const API_BASE_URL = "http://localhost:5090/api/activity-logs";

export const activityLogService = {
  async getActivityLogs() {
    const token = localStorage.getItem("token");
    const response = await fetch(API_BASE_URL, {
      headers: {
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
    });

    if (!response.ok) throw new Error("Failed to fetch activity logs.");
    return await response.json();
  },
};
