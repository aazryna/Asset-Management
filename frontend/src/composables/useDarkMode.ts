export const useDarkMode = () => {
  const isDark = useState("isDark", () => {
    if (import.meta.client) {
      return localStorage.getItem("theme") === "dark";
    }
    return false;
  });

  const toggleDarkMode = () => {
    isDark.value = !isDark.value;
    if (import.meta.client) {
      localStorage.setItem("theme", isDark.value ? "dark" : "light");
      if (isDark.value) {
        document.documentElement.classList.add("dark");
      } else {
        document.documentElement.classList.remove("dark");
      }
    }
  };

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
