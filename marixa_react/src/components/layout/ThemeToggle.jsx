import { useState, useEffect } from "react";
import { applyTheme, initialTheme, themeKey } from "../../services/theme/theme";
export default function ThemeToggle() {
  const [theme, setTheme] = useState(
    () => document.documentElement.dataset.theme || "light",
  );
  useEffect(() => {
    const sync = (e) => {
      if (e.key === themeKey) {
        const next = initialTheme(
          localStorage,
          matchMedia("(prefers-color-scheme: dark)").matches,
        );
        document.documentElement.dataset.theme = next;
        document.documentElement.style.colorScheme = next;
        setTheme(next);
      }
    };
    window.addEventListener("storage", sync);
    return () => window.removeEventListener("storage", sync);
  }, []);
  return (
    <button
      type="button"
      className="theme-toggle"
      aria-label={
        theme === "dark" ? "Chuyển sang chế độ sáng" : "Chuyển sang chế độ tối"
      }
      title={theme === "dark" ? "Chế độ sáng" : "Chế độ tối"}
      onClick={() => {
        const next = theme === "dark" ? "light" : "dark";
        applyTheme(next);
        setTheme(next);
      }}
    >
      <span aria-hidden="true">{theme === "dark" ? "☀" : "☾"}</span>
    </button>
  );
}
