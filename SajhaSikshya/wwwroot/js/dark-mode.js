// SajhaSikshya — dark mode toggle.
// Persists the chosen theme in localStorage and applies it via Bootstrap 5.3's
// [data-bs-theme] attribute, which recolors every Bootstrap component automatically.

(function () {
    "use strict";

    const STORAGE_KEY = "sajhasikshya-theme";

    function getPreferredTheme() {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored === "light" || stored === "dark") {
            return stored;
        }
        return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }

    function applyTheme(theme) {
        document.documentElement.setAttribute("data-bs-theme", theme);
        const icon = document.getElementById("darkModeIcon");
        if (icon) {
            icon.setAttribute("data-lucide", theme === "dark" ? "sun" : "moon");
            if (window.lucide) {
                window.lucide.createIcons();
            }
        }
    }

    applyTheme(getPreferredTheme());

    document.addEventListener("DOMContentLoaded", function () {
        const toggleBtn = document.getElementById("darkModeToggleBtn");
        if (!toggleBtn) {
            return;
        }

        toggleBtn.addEventListener("click", function () {
            const current = document.documentElement.getAttribute("data-bs-theme") === "dark" ? "dark" : "light";
            const next = current === "dark" ? "light" : "dark";
            localStorage.setItem(STORAGE_KEY, next);
            applyTheme(next);
        });
    });
})();
