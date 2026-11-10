export const themeKey = "marixa_theme";
export function initialTheme(storage, prefersDark = false) {
  try {
    const v = storage.getItem(themeKey);
    if (v === "dark" || v === "light") return v;
  } catch {
    /* Storage can be unavailable. */
  }
  return prefersDark ? "dark" : "light";
}
export function applyTheme(theme) {
  document.documentElement.dataset.theme = theme;
  document.documentElement.style.colorScheme = theme;
  try {
    localStorage.setItem(themeKey, theme);
  } catch {
    /* Preference is optional. */
  }
}
