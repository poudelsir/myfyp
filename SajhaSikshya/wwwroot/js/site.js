// SajhaSikshya — global bootstrap script.
// Runs on every page (both the dashboard shell and the auth/marketing layout).
// Keep this file limited to behavior that truly applies everywhere; page- or
// feature-specific scripts belong in their own file (see dashboard.js, dark-mode.js).

(function () {
    "use strict";

    document.addEventListener("DOMContentLoaded", function () {
        if (window.lucide) {
            window.lucide.createIcons();
        }
    });
})();
