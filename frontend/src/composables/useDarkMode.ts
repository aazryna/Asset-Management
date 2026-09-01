export const useDarkMode = () => {
  const isDark = useState("isDark", () => {
    // Cuba baca dari localStorage kalau ada, kalau tak default ke false (light mode)
    if (import.meta.client) {
      return localStorage.getItem("theme") === "dark";
    }
    return false;
  });

  const toggleDarkMode = () => {
    isDark.value = !isDark.value;
    if (import.meta.client) {
      localStorage.setItem("theme", isDark.value ? "dark" : "light");
      // Masukkan atau buang class 'dark' kat tag <html> secara global
      if (isDark.value) {
        document.documentElement.classList.add("dark");
      } else {
        document.documentElement.classList.remove("dark");
      }
    }
  };

  // Sync class masa page mula-mula load
  const initTheme = () => {
    if (import.meta.client && isDark.value) {
      document.documentElement.classList.add("dark");
    }
  };

  return {
    isDark,
    toggleDarkMode,
    initTheme,
  };
};
